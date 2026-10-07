using Elastic.Clients.Elasticsearch;
using ElasticsearchMovieApi.Models;
using ElasticsearchMovieApi.Services;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using ElasticsearchMovieApi.Options;

public class ElasticsearchService : IMovieRepository, IHostedService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;
    private readonly bool _resetIndexOnStartup;
    private readonly string _moviesCsvFilePath =
        Environment.GetEnvironmentVariable("MOVIE_CSV_PATH") ?? "local-data/movies.csv";

    private const int BulkBatchSize = 500;

    public ElasticsearchService(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
        _indexName = options.Value.IndexName;
        _resetIndexOnStartup = options.Value.ResetIndexOnStartup;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var indexExists = (await _client.Indices.ExistsAsync(_indexName)).Exists;
        if (indexExists && _resetIndexOnStartup)
        {
            _logger.LogWarning("Reset requested. Deleting Elasticsearch index {IndexName} before CSV import.", _indexName);
            var deleteResponse = await _client.Indices.DeleteAsync(_indexName);
            if (!deleteResponse.IsValidResponse)
            {
                throw new InvalidOperationException($"Could not delete Elasticsearch index '{_indexName}'.");
            }

            indexExists = false;
        }

        if (indexExists)
        {
            _logger.LogInformation("Elasticsearch index {IndexName} already exists; skipping CSV import.", _indexName);
            return;
        }

        await BulkIndexAsync(_indexName, _moviesCsvFilePath, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<IEnumerable<Movie>> SearchByTitle(string title)
    {
        var response = await _client.SearchAsync<Movie>(search =>
            search.Indices(_indexName)
            .Query(q => q
                .Match(m => m.Field(f => f.Title).Query(title))
            )
        );
        return response.Documents;
    }

    public async Task<IEnumerable<Movie>> SearchByOverview(string overview)
    {
        var response = await _client.SearchAsync<Movie>(search =>
            search.Indices(_indexName)
            .Query(q => q
                .Match(m => m.Field(f => f.Overview).Query(overview))
            )
        );
        return response.Documents;
    }

    public async Task CreateIndexIfNotExistAsync(string indexName)
    {
        if (!(await _client.Indices.ExistsAsync(indexName)).Exists)
        {
            await _client.Indices.CreateAsync(indexName);
        }
    }

    private async Task BulkIndexAsync(string indexName, string csvFilePath, CancellationToken cancellationToken)
    {
        await CreateIndexIfNotExistAsync(indexName);

        var fileInfo = new FileInfo(csvFilePath);
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting CSV import from {CsvFilePath} ({CsvFileSizeBytes} bytes) into index {IndexName}; batch size is {BatchSize}.",
            fileInfo.FullName,
            fileInfo.Length,
            indexName,
            BulkBatchSize);

        using var reader = new StreamReader(csvFilePath);
        await reader.ReadLineAsync(cancellationToken);

        var batch = new List<Movie>(BulkBatchSize);
        long rowsRead = 0;
        long rowsSkipped = 0;
        long moviesIndexed = 0;
        var batchNumber = 0;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            rowsRead++;
            if (string.IsNullOrWhiteSpace(line))
            {
                rowsSkipped++;
                continue;
            }

            var movie = ParseMovie(line);
            if (movie is null)
            {
                rowsSkipped++;
                continue;
            }

            batch.Add(movie);
            if (batch.Count == BulkBatchSize)
            {
                batchNumber++;
                _logger.LogInformation(
                    "Submitting CSV batch {BatchNumber}: {BatchSize} movies; {RowsRead} rows read, {RowsSkipped} skipped.",
                    batchNumber,
                    batch.Count,
                    rowsRead,
                    rowsSkipped);
                await IndexBatchAsync(batch, indexName);
                moviesIndexed += batch.Count;
                batch.Clear();
                _logger.LogInformation(
                    "CSV batch {BatchNumber} completed; {MoviesIndexed} movies indexed so far.",
                    batchNumber,
                    moviesIndexed);
            }
        }

        if (batch.Count > 0)
        {
            batchNumber++;
            _logger.LogInformation(
                "Submitting final CSV batch {BatchNumber}: {BatchSize} movies; {RowsRead} rows read, {RowsSkipped} skipped.",
                batchNumber,
                batch.Count,
                rowsRead,
                rowsSkipped);
            await IndexBatchAsync(batch, indexName);
            moviesIndexed += batch.Count;
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "CSV import complete: {RowsRead} rows read, {MoviesIndexed} movies indexed, {RowsSkipped} rows skipped, {BatchCount} batches in {ElapsedSeconds:F1} seconds.",
            rowsRead,
            moviesIndexed,
            rowsSkipped,
            batchNumber,
            stopwatch.Elapsed.TotalSeconds);
    }

    private async Task IndexBatchAsync(IEnumerable<Movie> movies, string indexName)
    {
        var response = await _client.IndexManyAsync(movies, indexName);
        if (response.Errors)
        {
            throw new InvalidOperationException("One or more movies could not be indexed in Elasticsearch.");
        }
    }

    private static Movie? ParseMovie(string line)
    {
        var fields = ParseCsvLine(line);

        if (fields.Count < 16 ||
            !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return null;
        }

        DateTime? releaseDate = DateTime.TryParse(
            fields[5], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedReleaseDate)
            ? parsedReleaseDate
            : null;

        return new Movie
        {
            Id = id,
            Title = fields[1],
            ReleaseDate = releaseDate,
            Genres = fields[15],
            Overview = fields[12]
        };
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var insideQuotes = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (character == ',' && !insideQuotes)
            {
                fields.Add(field.ToString().Trim());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        fields.Add(field.ToString().Trim());
        return fields;
    }

}
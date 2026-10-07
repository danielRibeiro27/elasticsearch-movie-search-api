using System.Diagnostics;
using ElasticsearchMovieApi.Services;
using ElasticsearchMovieApi.Options;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using Elastic.Transport;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<ElasticsearchOptions>()
    .Bind(builder.Configuration.GetSection(ElasticsearchOptions.SectionName));

builder.Services.AddSingleton<IExecutionLogger, FileExecutionLogger>();
builder.Services.AddSingleton(serviceProvider =>
    CreateElasticsearchClient(
        serviceProvider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value));

// Share one instance between startup indexing and API repository requests.
builder.Services.AddSingleton<ElasticsearchService>();
builder.Services.AddSingleton<IMovieRepository>(serviceProvider =>
    serviceProvider.GetRequiredService<ElasticsearchService>());

// The host awaits indexing during startup before it begins serving requests.
builder.Services.AddHostedService<ElasticsearchService>(serviceProvider =>
    serviceProvider.GetRequiredService<ElasticsearchService>());

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/movies/search/title", async (string title, IMovieRepository repository, IExecutionLogger executionLogger) =>
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        var results = (await repository.SearchByTitle(title)).ToList();
        return Results.Ok(new { results, count = results.Count });
    }
    finally
    {
        stopwatch.Stop();
        executionLogger.Log("movie-search", stopwatch.Elapsed);
    }
});

app.MapGet("/movies/search/overview", async (string overview, IMovieRepository repository, IExecutionLogger executionLogger) =>
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        var results = (await repository.SearchByOverview(overview)).ToList();
        return Results.Ok(new { results, count = results.Count });
    }
    finally
    {
        stopwatch.Stop();
        executionLogger.Log("movie-search-overview", stopwatch.Elapsed);
    }
});

app.Run();

static ElasticsearchClient CreateElasticsearchClient(ElasticsearchOptions options)
{
    if (string.IsNullOrWhiteSpace(options.Url))
    {
        throw new InvalidOperationException("Elasticsearch URL não configurada.");
    }

    var endpoint = new Uri(options.Url);
    var settings = new ElasticsearchClientSettings(endpoint)
        .Authentication(new BasicAuthentication(options.Username, options.Password));

    // HTTP development endpoints do not need TLS certificate pinning.
    if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(options.CertificateAuthorityPem))
        {
            throw new InvalidOperationException("Elasticsearch CA certificate not configured.");
        }

        // Elastic.Transport pins the CA by its SHA-256 fingerprint
        using var caCertificate = X509Certificate2.CreateFromPem(options.CertificateAuthorityPem);
        settings.CertificateFingerprint(caCertificate.GetCertHashString(HashAlgorithmName.SHA256));
    }
    return new ElasticsearchClient(settings);
}

using System.Globalization;
using ElasticsearchMovieApi.Models;

namespace ElasticsearchMovieApi.Services;

public class MovieCsvRepository : IMovieRepository
{
    private readonly string _movieDatasetPath = Environment.GetEnvironmentVariable("MOVIE_CSV_PATH")
        ?? "C:\\Data\\movie dataset\\movies.csv";

    public MovieCsvRepository()
    {
        if (!File.Exists(_movieDatasetPath))
            throw new FileNotFoundException("Movie dataset not found.", _movieDatasetPath);
    }

    private static Movie? ParseMovie(string line)
    {
        var fields = ParseCsvLine(line);

        if (fields.Count < 16 ||
            !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ||
            !DateTime.TryParse(fields[5], CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate))
        {
            return null;
        }

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

    public async Task<IEnumerable<Movie>> SearchByTitle(string title)
    {
        var lines = await File.ReadAllLinesAsync(_movieDatasetPath);
        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseMovie)
            .Where(movie => movie is not null)
            .Select(movie => movie!)
            .Where(movie => string.IsNullOrWhiteSpace(title) ||
                movie.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<Movie>> SearchByOverview(string overview)
    {
        var lines = await File.ReadAllLinesAsync(_movieDatasetPath);
        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseMovie)
            .Where(movie => movie is not null)
            .Select(movie => movie!)
            .Where(movie => string.IsNullOrWhiteSpace(overview) ||
                movie.Overview.Contains(overview, StringComparison.OrdinalIgnoreCase));
    }
}

using System.Text.Json;
using ElasticsearchMovieApi.Models;

namespace ElasticsearchMovieApi.Services;

public class MovieRepository : IMovieRepository
{
    private readonly List<Movie> _movies;
    private const string MovieDatasetPath = "C:\\Data\\movies.json";

    public MovieRepository()
    {
        if (File.Exists(MovieDatasetPath))
        {
            var json = File.ReadAllText(MovieDatasetPath);
            _movies = JsonSerializer.Deserialize<List<Movie>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Movie>();
        }
        else
        {
            _movies = new List<Movie>
            {
                new() { Id = 1, Title = "The Matrix", ReleaseDate = new DateTime(1999, 1, 1), Genres = "Sci-Fi", Overview = string.Empty },
                new() { Id = 2, Title = "Inception", ReleaseDate = new DateTime(2010, 1, 1), Genres = "Sci-Fi", Overview = string.Empty },
                new() { Id = 3, Title = "The Godfather", ReleaseDate = new DateTime(1972, 1, 1), Genres = "Drama", Overview = string.Empty },
                new() { Id = 4, Title = "Pulp Fiction", ReleaseDate = new DateTime(1994, 1, 1), Genres = "Crime", Overview = string.Empty }
            };
        }
    }

    public Task<IEnumerable<Movie>> SearchByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Task.FromResult<IEnumerable<Movie>>(_movies);

        IEnumerable<Movie> results = _movies
            .Where(movie => movie.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(results);
    }

    public Task<IEnumerable<Movie>> SearchByOverview(string overview)
    {
        if (string.IsNullOrWhiteSpace(overview))
            return Task.FromResult<IEnumerable<Movie>>(_movies);

        IEnumerable<Movie> results = _movies
            .Where(movie => movie.Overview.Contains(overview, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(results);
    }
}

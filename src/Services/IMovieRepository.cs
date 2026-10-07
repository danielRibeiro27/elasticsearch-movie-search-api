using ElasticsearchMovieApi.Models;

namespace ElasticsearchMovieApi.Services;

public interface IMovieRepository
{
    Task<IEnumerable<Movie>> SearchByTitle(string title);
    Task<IEnumerable<Movie>> SearchByOverview(string overview);
}

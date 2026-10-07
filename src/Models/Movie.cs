namespace ElasticsearchMovieApi.Models;

public class Movie
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public required string Genres{ get; set; }
    public required string Overview { get; set; }
}

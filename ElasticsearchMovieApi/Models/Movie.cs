namespace ElasticsearchMovieApi.Models;

public class Movie
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? Year { get; set; }
    public string? Language { get; set; }
    public float? Rating { get; set; }
}

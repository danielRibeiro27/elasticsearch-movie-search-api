namespace ElasticsearchMovieApi.Options;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; init; } = string.Empty;

    public string? ApiKey { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string CertificateAuthorityPem { get; init; } = string.Empty;

    public string IndexName { get; init; } = "movies";

    public bool ResetIndexOnStartup { get; init; }
}
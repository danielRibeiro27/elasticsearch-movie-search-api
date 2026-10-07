namespace ElasticsearchMovieApi.Services;

public interface IExecutionLogger
{
    void Log(string operation, TimeSpan elapsed);
}
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ ./
RUN dotnet publish ElasticsearchMovieApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY local-data/movies.csv /app/data/movies.csv

ENV ASPNETCORE_HTTP_PORTS=8080
ENV MOVIE_CSV_PATH=/app/data/movies.csv

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElasticsearchMovieApi.dll"]
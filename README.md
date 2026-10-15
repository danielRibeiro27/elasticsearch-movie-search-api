# Elasticsearch Movie Search API

A movie search API backed by Elasticsearch. On startup the API imports `movies.csv` into the `movies` index (skipped if the index already exists), then serves searches from Elasticsearch.

## Requirements

- .NET 10 SDK for local execution
- Docker
- Minikube, kubectl, and Helm for Kubernetes
- The movie CSV dataset, expected at `local-data/movies.csv`

The repository is not bundled with the dataset. In PowerShell, copy the existing file into the build context before building the image:

```powershell
Copy-Item "C:\Data\movie dataset\movies.csv" ".\local-data\movies.csv" -Force
```

## API

```text
GET /
GET /movies/search/title?title=matrix
GET /movies/search/overview?overview=hacker
```

`/` returns a simple health message. The search routes return `{ results, count }`.

## Run Locally

To run against a local Elasticsearch in Docker (and debug it), see [docs/local-development.md](docs/local-development.md).

From the repository root in PowerShell:

```powershell
$env:MOVIE_CSV_PATH = (Resolve-Path ".\local-data\movies.csv").Path
dotnet run --project .\src --launch-profile http
```

The API is available at `http://localhost:7176`. Try it with:

```powershell
Invoke-RestMethod "http://localhost:7176/movies/search/title?title=matrix"
```

## Run On Minikube

Check *docs/kubernetes.md*
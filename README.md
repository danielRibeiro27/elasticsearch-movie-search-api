# Elasticsearch Movie Search API

The current API scans a CSV file for each search request. Elasticsearch is not currently connected to the application.

## Requirements

- .NET 10 SDK for local execution
- Docker Desktop
- Minikube, kubectl, and Helm for Kubernetes
- The movie CSV dataset expected by `MovieCsvRepository`

The repository is not bundled with the dataset. In PowerShell, copy the existing file into the build context before building the image:

```powershell
Copy-Item "C:\Data\movie dataset\movies.csv" ".\local-data\movies.csv" -Force
```

## API

```text
GET /movies/search
GET /movies/search?title=matrix
```

The first route returns the full dataset and can produce a large response.

## Run Locally

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
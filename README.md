# Project Elasticsearch Movies API
Goal: Demonstrates a feature that a normal SQL LIKE '%matrix%' query would not handle well.

## Tech
- ASP.NET Core 9
- Elasticsearch
- Docker Compose
- Elastic.Clients.Elasticsearch

## Endpoints
-POST /seed
-GET /search?q=matrix
-GET /autocomplete?q=mat

## Run
1. docker compose up
2. dotnet run
3. curl localhost:5000/search?q=matrx

#Response:
```json
[
  {
    "title": "The Matrix"
  }
]
```

# Project Annotations

## Brief description
The project objective is to learn and demonstrate actual production use of elastic search within 3 hours of development.

For that we are aiming at the simplest API that can search for movies in a catalog.

That search is the chore of the project, because it uses elasticsearch for solving a problem that otherwise would be a pain.

**The problem**: the search — misspeled words — in a regular SQL with billions registers database would take X time and require additional logic for handling diffusion.
**The solution**: using the elasticsearch API it takes X time and automatically handles diffusion.

## Tech Stack
- .NET 9
- REST API
- Elasticsearch

## The API
A simple API that allows a title and overview search

## Endpoints
- POST /seed
- GET  /search?q=
    /search aceita filtros:
    /search?q=matrix
       &year=1999
       &language=en
       &minRating=8
- GET  /autocomplete?q=
- POST /reindex
- GET  /movie/{id}

## The Strucutre
There is no need for using complex patterns, the project should only have: a controller, a search service and a database

elasticsearch-movie-search-api
├── docs
├── local-data
├── ├── movies.csv
├── src
├── ├── /Controllers
├── ├── ├──MoviesController.cs
├── ├── /Services
├── ├── ├──ElasticSearchService.cs
├── ├── /Models
├── ├── ├── Movie.cs
├── ├── /Util
├── ├── ├──CsvParser.cs



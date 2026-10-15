# Run Locally With Elasticsearch

Run the API on your machine against a single-node Elasticsearch in Docker. Commands are for Linux/macOS; run them from the repository root.

## Requirements

- .NET 10 SDK
- Docker
- The movie dataset at `local-data/movies.csv` (git-ignored, about 788 MB)

## 1. Start Elasticsearch

The defaults in `src/appsettings.Development.json` are `http://localhost:9200`, user `elastic`, password `changeme`. This container matches them:

```bash
docker run -d --name es-local -p 127.0.0.1:9200:9200 \
  -e discovery.type=single-node \
  -e ELASTIC_PASSWORD=changeme \
  -e xpack.security.http.ssl.enabled=false \
  -e ES_JAVA_OPTS="-Xms1g -Xmx1g" \
  docker.elastic.co/elasticsearch/elasticsearch:9.5.4
```

Wait until it responds:

```bash
curl -u elastic:changeme http://localhost:9200
```

Stop and start it later with `docker stop es-local` and `docker start es-local`. Data is kept in the container; `docker rm -f es-local` discards it.

## 2. Run The API

```bash
dotnet run --project src --launch-profile http
```

The API listens on `http://localhost:7176`.

On startup it imports the CSV into the `movies` index **before** serving requests (about 1.2 million movies; it can take 10+ minutes). If the index already exists, the import is skipped.

The CSV path defaults to `local-data/movies.csv`. If it is not found relative to the working directory, parent folders are searched. To use another file, set an absolute path:

```bash
export MOVIE_CSV_PATH="$(pwd)/local-data/movies.csv"
```

Replace this with the real location of your file if it is elsewhere. If you exported a wrong value, clear it with `unset MOVIE_CSV_PATH` and restart the API.

A smaller CSV with the same columns speeds up debugging.

## 3. Try It

```bash
curl "http://localhost:7176/"
curl "http://localhost:7176/movies/search/title?title=matrix" | jq > response.json && kate response.json
curl "http://localhost:7176/movies/search/overview?overview=hacker"
```

Request timings are written to `execution.log` next to the built binary (`src/bin/Debug/net10.0/execution.log`), not to the console.

## Debug In VS Code

1. Open the repository root.
2. Start the API with F5 using the C# debugger on the `src` project, or run `dotnet run` and use **Attach to Process**.
3. Set breakpoints in `src/Program.cs` or `src/Services/ElasticsearchService.cs`. Breakpoints in the import code are hit during startup.

## Configuration Overrides

Settings bind from the `Elasticsearch` section and can be overridden with environment variables:

| Variable | Purpose |
| --- | --- |
| `Elasticsearch__Url` | Elasticsearch address |
| `Elasticsearch__Username` / `Elasticsearch__Password` | Basic authentication |
| `Elasticsearch__IndexName` | Index name (default `movies`) |
| `Elasticsearch__ResetIndexOnStartup` | `true` deletes and re-imports the index on start |
| `MOVIE_CSV_PATH` | CSV file to import |

For an HTTPS Elasticsearch, also set `Elasticsearch__CertificateAuthorityPem` to the CA certificate; the app pins it by fingerprint.

## Troubleshooting

- **Import skipped / empty results:** an empty `movies` index from an earlier failed run makes the app skip the import. Delete it and restart:

  ```bash
  curl -u elastic:changeme -X DELETE http://localhost:9200/movies
  ```

- **Connection refused on 9200:** Elasticsearch is still starting or stopped. Check `docker ps` and `docker logs es-local`.
- **401 Unauthorized:** the password differs from `Elasticsearch__Password` / `appsettings.Development.json`.
- **Container exits immediately:** give Docker at least 2 GB of memory, or on Linux run `sudo sysctl -w vm.max_map_count=262144`.
- **Running in Kubernetes instead:** see [kubernetes.md](./kubernetes.md).

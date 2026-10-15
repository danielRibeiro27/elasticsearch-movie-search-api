The movie database is large and token-heavy.

### MovieCsvRepository

As the report `\reports\Report20260921-1644.diagsession` shows, the `MovieCsvRepository` reads the entire CSV file using a StringBuilder to parse the data. 
This approach is inefficien and results in a high CPU usage, as indicated by the following metrics:

|Nome da Função|Total de CPU \[unidade, %\]|CPU Própria \[unidade, %\]|Módulo|
|-|-|-|-|
|\| - ElasticsearchMovieApi.Services.MovieCsvRepository.ParseCsvLine\(string\)|10420 \(63,24%\)|5670 \(34,41%\)|elasticsearchmovieapi|

### MovieElasticsearchRepository

This approach improves performance significantly.

### Measured Search Latency

Both lines come from `execution.log` (`operation=movie-search`, same title search):

| Repository | Strategy | Log entry | Elapsed |
|-|-|-|-|
| `MovieCsvRepository` | Full scan, reading the CSV from disk on every request | `2026-10-14T19:11:28.6472361+00:00 operation=movie-search elapsed_ms=6402.230` | 6402.230 ms (~6.4 s) |
| Elasticsearch (indexed), local / inside Kubernetes | Index lookup | `2026-10-14T18:59:34.4202704+00:00 operation=movie-search elapsed_ms=8.287` | 8.287 ms |

The indexed Elasticsearch search was about 770x faster (6402.230 / 8.287 ≈ 772.5) than the full CSV scan. These are single-request samples, not averaged benchmarks.

## Configuracao do Elasticsearch

A aplicacao usa `IConfiguration` e o bind de `ElasticsearchOptions`. Os valores de
`appsettings.json` sao defaults nao sensiveis e podem ser sobrescritos por variaveis
de ambiente no Kubernetes.

| Variavel de ambiente | Destino da configuracao | Origem no Kubernetes |
| --- | --- | --- |
| `Elasticsearch__Url` | `Elasticsearch:Url` | `env` ou ConfigMap |
| `Elasticsearch__IndexName` | `Elasticsearch:IndexName` | `env` ou ConfigMap |
| `Elasticsearch__ApiKey` | `Elasticsearch:ApiKey` | `env` com `secretKeyRef` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente ASP.NET Core | `env` ou ConfigMap |

O Deployment fornece `Elasticsearch__ApiKey` por `secretKeyRef`; nao coloque a chave
em `appsettings*.json`, nos arquivos `values-*.yaml`, no Dockerfile ou em outros
arquivos versionados. Antes de aplicar o chart, crie ou atualize o Secret com a chave
real fora do Git; `secrets.yaml` e somente um exemplo com placeholder seguro. Os
arquivos `values-act.yaml`, `values-dev.yaml` e `values-prd.yaml` contem apenas
valores nao sensiveis e a referencia para o segredo.


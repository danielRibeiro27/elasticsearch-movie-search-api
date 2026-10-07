The movie database is large and token-heavy.

### MovieCsvRepository

As the report `\reports\Report20260921-1644.diagsession` shows, the `MovieCsvRepository` reads the entire CSV file using a StringBuilder to parse the data. 
This approach is inefficien and results in a high CPU usage, as indicated by the following metrics:

|Nome da Função|Total de CPU \[unidade, %\]|CPU Própria \[unidade, %\]|Módulo|
|-|-|-|-|
|\| - ElasticsearchMovieApi.Services.MovieCsvRepository.ParseCsvLine\(string\)|10420 \(63,24%\)|5670 \(34,41%\)|elasticsearchmovieapi|

### MovieElasticsearchRepository

This approach improves performance significantly

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


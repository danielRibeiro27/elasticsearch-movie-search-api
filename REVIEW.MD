# Revisão do projeto

**Data:** 2026-09-28  
**Escopo:** estrutura geral, API e principalmente Helm/Kubernetes. A revisão considera o estado atual do workspace, incluindo arquivos ainda não commitados. A escolha de ler o CSV em disco não foi tratada como problema em si.

## Resumo

O projeto tem uma estrutura pequena e compreensível, com código da API separado de `deploy/helm`, documentação de execução e Dockerfile multi-stage. Porém, a configuração atual ainda não está pronta para instalação consistente: o chart falha no lint, o Deployment referencia um ConfigMap que não é criado, e os recursos default tornam o perfil local exigente. Além disso, a camada Elasticsearch está declarada na infraestrutura, mas não participa da API.

## Achados

### P1 - O chart não passa na validação do Helm

Em [`Chart.yaml`](deploy/helm/elasticsearch-movie-api/Chart.yaml), `apiVersion` está como `v1`. O `helm lint` executado com `values-dev.yaml` falhou com: `Chart.yaml: chart type is not valid in apiVersion 'v1'. It is valid in apiVersion 'v2'`. Assim, os comandos documentados de lint/instalação não são confiáveis até a metadata do chart ser corrigida.

**Recomendação:** atualizar o chart para a versão de API Helm suportada (`v2`) e manter `helm lint` e `helm template` como validações do fluxo de entrega.

### P1 - O Deployment referencia um ConfigMap Elasticsearch inexistente

[`deployment.yaml`](deploy/helm/elasticsearch-movie-api/templates/deployment.yaml) injeta `elasticsearch-config` no container Elasticsearch. O chart só cria o ConfigMap da API em [`configmap.yaml`](deploy/helm/elasticsearch-movie-api/templates/configmap.yaml); não há template que crie `elasticsearch-config`. Depois de corrigido o bloqueio de metadata, o pod pode ficar em `CreateContainerConfigError` por referência ausente.

**Recomendação:** criar o recurso com a configuração necessária, ou remover o container e a referência enquanto Elasticsearch não fizer parte do runtime. Configurações sensíveis devem vir de `Secret`, não de `ConfigMap`.

### P1 - Recursos default podem impedir o perfil Minikube documentado

[`values.yaml`](deploy/helm/elasticsearch-movie-api/values.yaml) define limite de `4 CPU` e `4Gi` para cada container, API e Elasticsearch. Como não há `requests`, o Kubernetes usa os limites como requests. O pod passa a solicitar aproximadamente `8 CPU` e `8Gi`; os values de desenvolvimento herdam esses defaults. O procedimento em [`kubernetes.md`](docs/kubernetes.md) não provisiona um Minikube com essa capacidade, então o pod pode permanecer `Pending` em uma instalação padrão.

**Recomendação:** definir `requests` e `limits` medidos para cada perfil, com valores de desenvolvimento compatíveis com Minikube; reservar os maiores valores para ambientes dimensionados e testados.

### P1 - Elasticsearch está isolado em cada réplica e não é usado pela API

[`Program.cs`](src/Program.cs) registra `MovieCsvRepository`; não registra nem chama um cliente Elasticsearch. [`ElasticsearchService.cs`](src/Services/ElasticsearchService.cs) está vazio, e a configuração `ElasticsearchOptions` não é consumida pelo fluxo. Ao mesmo tempo, o Deployment executa um container Elasticsearch junto de cada API. Em produção, `replicaCount: 3` significa três instâncias Elasticsearch independentes, sem volume persistente, serviço ou configuração de cluster. Isso não forma um backend compartilhado e os dados dessas instâncias seriam efêmeros.

**Recomendação:** escolher explicitamente o estado atual ou o objetivo: enquanto a API for CSV-backed, retirar o container Elasticsearch do Deployment; se Elasticsearch for requisito, integrar a API e operá-lo como serviço próprio gerenciado/StatefulSet, com persistência, configuração, credenciais e estratégia de disponibilidade adequadas.

### P2 - Tag `latest` dificulta releases reproduzíveis

Todos os arquivos de values usam a tag `latest` com `IfNotPresent`, inclusive o perfil de produção em [`values-prd.yaml`](deploy/helm/elasticsearch-movie-api/values-prd.yaml). O mesmo manifesto pode então executar imagens diferentes conforme cache e histórico do nó, e uma atualização de tag não garante novo pull.

**Recomendação:** publicar e fixar tags imutáveis por release (ou digest), promovendo a mesma imagem entre ambientes.

### P2 - `Recreate` causa interrupção durante atualizações

[`deployment.yaml`](deploy/helm/elasticsearch-movie-api/templates/deployment.yaml) usa estratégia `Recreate`. Com três réplicas, uma atualização remove os pods existentes antes de iniciar os novos, causando indisponibilidade.

**Recomendação:** usar `RollingUpdate` com limites explícitos de `maxUnavailable`/`maxSurge`, depois de separar Elasticsearch da API e validar compatibilidade entre versões.

### P2 - Faltam probes para tráfego e recuperação

O Deployment não define `startupProbe`, `readinessProbe` nem `livenessProbe`. O Service pode direcionar tráfego sem sinal de prontidão da aplicação, e o Kubernetes não tem um critério de saúde do processo além de ele estar rodando.

**Recomendação:** expor um endpoint de health apropriado e configurar probes com tempos compatíveis com a inicialização. Readiness deve refletir se a API pode atender; liveness não deve reiniciar o processo por uma dependência externa temporariamente indisponível.

### P2 - Ingress não configura host nem TLS

[`ingress.yaml`](deploy/helm/elasticsearch-movie-api/templates/ingress.yaml) fixa `ingressClassName: nginx`, aceita qualquer host e não declara TLS. Isso pode servir para o Minikube descrito na documentação, mas não oferece configuração de domínio e HTTPS para produção.

**Recomendação:** parametrizar classe e host; habilitar TLS via Secret/cert-manager nos ambientes expostos e não deixar o perfil de produção depender de HTTP sem uma decisão explícita.

### P2 - Rotas documentadas não correspondem às rotas implementadas

O README documenta `GET /movies/search?title=matrix`, mas [`Program.cs`](src/Program.cs) implementa `/movies/search/title` e `/movies/search/overview`. O planejamento e as anotações também descrevem rotas como `/search?q=`, que não existem no código atual. Os exemplos podem resultar em 404 ou orientar consumidores para um contrato antigo.

**Recomendação:** definir o contrato atual, alinhar README/docs e adicionar testes de integração para as rotas e parâmetros publicados.

### P2 - Logs em arquivo não se integram bem ao modelo de containers

[`FileExecutionLogger.cs`](src/Services/FileExecutionLogger.cs) grava em `execution.log` dentro do diretório da aplicação. O arquivo não é coletado automaticamente por `kubectl logs`, pode ser perdido quando o pod é substituído e fica separado por réplica.

**Recomendação:** preferir logging estruturado via `ILogger` para stdout/stderr e agregar logs no ambiente de execução; métricas de latência devem ser expostas/colhidas separadamente dos logs.

## Visão geral da API

A API é enxuta e os endpoints de título e overview deixam clara a intenção de busca. Hoje, no entanto, sua implementação é uma API CSV-backed; não há integração funcional com Elasticsearch apesar do nome, pacote e configuração existentes. As respostas não têm paginação ou limite explícito, e uma consulta sem termo pode retornar o catálogo inteiro. Para o próximo passo, vale estabilizar contrato e validação dos parâmetros, limitar/paginar resultados, documentar OpenAPI e cobrir as rotas com testes. A decisão de armazenamento em CSV fica fora deste parecer.

## Cobertura de testes

[`docs/tests.md`](docs/tests.md) descreve cenários pretendidos, mas não há projeto de testes automatizados visível na estrutura atual. Isso deixa sem verificação executável tanto o contrato HTTP como os templates e valores Helm. Recomenda-se começar com testes de integração pequenos para a API e validação Helm em CI para cada conjunto de values.

## Ordem sugerida

1. Corrigir metadata do chart e a referência/configuração do ConfigMap para tornar a instalação válida.
2. Decidir se Elasticsearch faz parte do runtime atual; alinhar Deployment e código com essa decisão.
3. Dimensionar requests/limits para dev e produção e adicionar probes.
4. Fixar imagens por release, adotar rollout apropriado e configurar TLS/host no perfil de produção.
5. Alinhar documentação ao contrato HTTP e automatizar testes da API e do chart.

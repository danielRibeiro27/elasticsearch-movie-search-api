
# Kubernetes (Minikube) Deployment

Run these commands from the repository root in PowerShell. They use a separate Minikube profile named `movie-api`, leaving the existing `minikube` profile untouched. The Docker driver publishes the Ingress controller's HTTP port on `localhost:7176`.

## Build The Image

```powershell
docker build -t elasticsearch-movie-api:latest -f .\src\Dockerfile .
```

## Start Minikube

Create the profile and port mapping once:

```powershell
minikube start -p minikube --driver=docker --ports=127.0.0.1:7176:80 --addons=ingress
```

On later sessions, start the existing profile with:

```powershell
minikube start -p movie-api
```

Wait for the Ingress controller, then load the local image into this profile:

```powershell
kubectl --context minikube wait --namespace ingress-nginx --for=condition=Ready pod --selector=app.kubernetes.io/component=controller --timeout=180s
minikube image load -p movie-api elasticsearch-movie-api:latest
```

## Install The ECK Operator

Install the ECK operator and its custom resource definitions:

```powershell
helm repo add elastic https://helm.elastic.co
helm repo update
helm install elastic-operator elastic/eck-operator -n elastic-system --create-namespace
kubectl get pods -n elastic-system
kubectl get crds
```

## Install The Helm Chart

The chart creates the Elasticsearch resource and API deployment. Install it once; do not apply the Helm template directly with `kubectl`, because Helm values must be rendered first. Disable Helm 4 server-side apply here so it does not conflict with ECK's ownership of `spec.nodeSets`.

Validate and install the Helm chart:

```powershell
helm lint .\deploy\helm\elasticsearch-movie-api -f .\deploy\helm\elasticsearch-movie-api\values-dev.yaml
helm upgrade --install elasticsearch-movie-api .\deploy\helm\elasticsearch-movie-api -f .\deploy\helm\elasticsearch-movie-api\values-dev.yaml --kube-context movie-api --server-side=false
kubectl --context movie-api rollout status deployment/elasticsearch-movie-api --timeout=180s
kubectl --context movie-api get pods
```

## Watch CSV Indexing

The CSV is copied into the API image at `/app/data/movies.csv`; it is not a Kubernetes volume. Elasticsearch stores indexed data on its own persistent volume claim (PVC).

Follow the API logs while it reads and indexes the CSV. Progress messages report submitted/completed batches, rows read, skipped rows, and indexed movies:

```powershell
kubectl --context movie-api -n default logs --follow deployment/elasticsearch-movie-api --timestamps
```

If the API container restarted, inspect its previous container logs as well:

```powershell
$apiPod = kubectl --context movie-api -n default get pods -l app=elasticsearch-movie-api -o jsonpath='{.items[0].metadata.name}'
kubectl --context movie-api -n default logs $apiPod --previous --timestamps
```

Check pod state and Elasticsearch data-volume usage in another terminal:

```powershell
kubectl --context movie-api -n default get pods --watch
kubectl --context movie-api -n default get pvc
kubectl --context movie-api -n default exec elasticsearch-movie-api-es-default-0 -- df -h /usr/share/elasticsearch/data
```

## Reset The Movie Index

To discard and rebuild only the `movies` index, enable the one-shot reset setting for a Helm upgrade. Watch the logs until they report `CSV import complete`:

```powershell
helm upgrade --install elasticsearch-movie-api .\deploy\helm\elasticsearch-movie-api -f .\deploy\helm\elasticsearch-movie-api\values-dev.yaml --set elasticsearch.resetIndexOnStartup=true --kube-context movie-api --server-side=false
kubectl --context movie-api -n default logs --follow deployment/elasticsearch-movie-api --timestamps
```

After the import completes, turn the setting off. The existing index will then be kept on later restarts rather than imported again:

```powershell
helm upgrade --install elasticsearch-movie-api .\deploy\helm\elasticsearch-movie-api -f .\deploy\helm\elasticsearch-movie-api\values-dev.yaml --set elasticsearch.resetIndexOnStartup=false --kube-context movie-api --server-side=false
```

Do not leave the reset setting enabled: each API pod restart would delete and rebuild the index. If indexing fails partway through, check the last completed batch and PVC usage, resolve the storage issue, then repeat the index reset.

## Wipe Elasticsearch Storage (Destructive)

Use this only if the Elasticsearch PVC itself must be recreated. It permanently deletes **all** Elasticsearch indices on that PVC, not just `movies`. Prefer resetting the movie index above for ordinary import failures.

First inspect the PVC names and identify the one belonging to `elasticsearch-movie-api-es-default-0`. Then stop the API and Elasticsearch resource before deleting that specific claim:

```powershell
kubectl --context movie-api -n default get pvc
kubectl --context movie-api -n default scale deployment/elasticsearch-movie-api --replicas=0
kubectl --context movie-api -n default delete elasticsearch elasticsearch-movie-api
kubectl --context movie-api -n default get pvc
$pvcName = Read-Host "Enter the exact Elasticsearch data PVC name shown above"
kubectl --context movie-api -n default delete pvc $pvcName
helm upgrade --install elasticsearch-movie-api .\deploy\helm\elasticsearch-movie-api -f .\deploy\helm\elasticsearch-movie-api\values-dev.yaml --set elasticsearch.resetIndexOnStartup=false --kube-context movie-api --server-side=false
```

Recreate the Elasticsearch resource and API from Helm afterward. Do not delete a PVC unless its name has been confirmed as the Elasticsearch data claim; deleting the wrong claim destroys unrelated data.

### Alternative / Manual Steps

Other ways to install or inspect the chart, kept here for reference:

```powershell
helm install es-quickstart elastic/eck-elasticsearch -n elastic-stack --create-namespace

kubectl apply -f deploy\helm\elasticsearch-movie-api\templates\deployment-elasticsearch.yaml

helm install elasticsearch . -f values-dev.yaml
# or if already exists
helm upgrade --install elasticsearch-movie-api . -f values-dev.yaml --server-side=false

# check
helm template elasticsearch . -f values-dev.yaml
helm lint . -f values-dev.yaml
```

## Credentials

Get the generated password:

```powershell
kubectl get secret elasticsearch-movie-api-es-elastic-user -o jsonpath='{.data.elastic}' | ForEach-Object { [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_)) }
```

For temporarily encoding a user:

```powershell
$encoded = kubectl --context movie-api -n default get secret elasticsearch-movie-api-es-elastic-user -o jsonpath='{.data.elastic}'
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($encoded))
```

## Check The Ingress

Check the Ingress and call the API through it:

```powershell
kubectl --context movie-api get ingress elasticsearch-movie-api-ingress
Invoke-RestMethod "http://localhost:7176/movies/search?title=matrix"
```

## Rebuild And Redeploy

For later rebuilds, repeat the Docker build and image load commands, then restart the deployment so Kubernetes replaces pods using the same `latest` tag:

```powershell
kubectl --context movie-api rollout restart deployment/elasticsearch-movie-api
kubectl --context movie-api rollout status deployment/elasticsearch-movie-api --timeout=180s
```

## Stop The Cluster

```powershell
minikube stop -p movie-api
```

## Troubleshooting: Disk I/O Hitting 100% After Starting Minikube

**Symptom:** Disk I/O spikes to 100% shortly after `minikube start`, and the Elasticsearch pod (`elasticsearch-movie-api-es-default-0`) restarts repeatedly.

**Root cause:** the `Elasticsearch` custom resource set `node.store.allow_mmap: false`. This forces Lucene to use `niofs` (plain read/write syscalls) for every segment access instead of memory-mapped files backed by the OS page cache, which drives disk I/O far higher during startup, indexing, and merges.

**Fix:** remove the `allow_mmap: false` override so mmap stays enabled (the default), and instead satisfy mmap's kernel requirement by raising `vm.max_map_count` through a privileged `initContainer` in [deployment-elasticsearch.yaml](../deploy/helm/elasticsearch-movie-api/templates/deployment-elasticsearch.yaml):

```yaml
podTemplate:
  spec:
    initContainers:
      - name: sysctl
        securityContext:
          privileged: true
          runAsUser: 0
        command:
          - sh
          - -c
          - sysctl -w vm.max_map_count=262144
```

After changing the chart, re-run the Helm upgrade so the change actually reaches the cluster, then confirm a new revision was created and the pod picked up the init container:

```powershell
helm upgrade --install elasticsearch-movie-api .\deploy\helm\elasticsearch-movie-api -f .\deploy\helm\elasticsearch-movie-api\values-dev.yaml --kube-context movie-api --server-side=false
helm history elasticsearch-movie-api
kubectl --context movie-api get pod elasticsearch-movie-api-es-default-0 -o jsonpath='{.spec.initContainers[*].name}'
kubectl --context movie-api get pods
```

**Checking the data volume:**

```powershell
kubectl get pvc --context movie-api
kubectl get pv --context movie-api
kubectl get storageclass --context movie-api
kubectl exec elasticsearch-movie-api-es-default-0 --context movie-api -- df -h /usr/share/elasticsearch/data
```

Note: the PVC reports a 1Gi capacity (ECK's default, since the chart defines no `volumeClaimTemplates`), but minikube's `standard` StorageClass uses the `k8s.io/minikube-hostpath` provisioner, which does not enforce that quota — actual space is bounded by the underlying node disk instead.

On Windows with the Docker driver, that node disk is a Docker Desktop/WSL2 virtual disk (`ext4.vhdx`), which reports its own configured/max size via `df`, not the real free space on the Windows host drive. Compare against the actual host disk to know the true ceiling:

```powershell
Get-PSDrive C | Select-Object Used,Free
docker system df -v
```

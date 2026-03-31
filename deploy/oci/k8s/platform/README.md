# Platform Stack Bootstrap (Phase 2)

This folder prepares namespaces and baseline config. Helm-managed controllers are installed after namespace bootstrap.

## 1) Bootstrap namespaces

```powershell
kubectl apply -k deploy/oci/k8s/platform
```

## 2) Install controllers (recommended order)

```powershell
# Ingress NGINX
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx `
  --namespace myapp-platform `
  --set controller.replicaCount=1

# cert-manager (optional for TLS)
helm repo add jetstack https://charts.jetstack.io
helm repo update
helm upgrade --install cert-manager jetstack/cert-manager `
  --namespace myapp-platform `
  --set installCRDs=true

# Dapr control plane
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo update
helm upgrade --install dapr dapr/dapr `
  --namespace myapp-platform `
  --set global.ha.enabled=false

# External Secrets Operator
helm repo add external-secrets https://charts.external-secrets.io
helm repo update
helm upgrade --install external-secrets external-secrets/external-secrets `
  --namespace myapp-platform

# Optional: seed OCI Vault mapping templates
kubectl apply -f deploy/oci/k8s/platform/external-secrets-oci.example.yaml
```

## 3) Apply Dapr components

```powershell
kubectl apply -f deploy/oci/k8s/dapr/components/dapr-config.yaml
kubectl apply -f deploy/oci/k8s/dapr/components/statestore.yaml
kubectl apply -f deploy/oci/k8s/dapr/components/pubsub.yaml
```

## 4) Gateway TLS (dev default)

`deploy/oci/k8s/apps/overlays/dev/gateway-tls-issuer.yaml` creates a self-signed
issuer for dev TLS. For public environments, switch to a real ClusterIssuer (ACME/CA)
and update ingress annotations in `deploy/oci/k8s/apps/overlays/dev/gateway.yaml`.

## Phase 2 caveat

App ID scopes in Dapr components are currently short names for parity with existing manifests. If final app IDs use `myapp-dev-*`, update `scopes` before deploying workloads.

## Redis requirement

Your Dapr `statestore`/`pubsub` manifests reference `redis-master.myapp-platform.svc.cluster.local:6379`.

1. Create the redis secret first:

```powershell
kubectl apply -f deploy/oci/k8s/platform/redis-secrets.example.yaml
```

2. Then apply the platform kustomization (includes `redis.yaml`):

```powershell
kubectl apply -k deploy/oci/k8s/platform
```

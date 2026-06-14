# Cloud-agnostic Kubernetes manifests (Tier 0)

Portable workloads for the ERP microservices stack. No cloud provider assumptions.

## Layout

```text
base/
  platform/   namespaces, Redis
  dapr/       Dapr config and Redis-backed components
  sql/        SQL Server StatefulSet, bootstrap job (no backup)
  apps/       microservices, gateway, Aspire dashboard
overlays/
  dev/        self-signed gateway TLS
  prod/       Let's Encrypt ClusterIssuers + ingress host patch
contracts/    service catalog and secret key schemas
```

## Quick validate

```powershell
kubectl kustomize deploy/k8s/overlays/dev
kubectl kustomize deploy/k8s/overlays/prod
```

Secrets must exist in the cluster before apply (`redis-secrets`, `app-secrets`, `sql-secrets`, etc.). See `contracts/`.

## Local Minikube stage-test

Optional scripted deploy (dev passwords, reduced SQL resources, local image build):

```powershell
.\scripts\deploy-minikube-stage.ps1
.\scripts\deploy-minikube-stage.ps1 -Profile Core   # auth + gateway only
.\scripts\teardown-minikube-stage.ps1               # take down workloads + Helm
.\scripts\teardown-minikube-stage.ps1 -DeleteMinikube -Force  # remove cluster
```

See `deploy/k8s/overlays/minikube/README.md`.

## Cloud-specific overlays

| Cloud | Overlay path |
|-------|----------------|
| AWS EKS | `deploy/aws/k8s/overlays/` |
| OCI OKE | `deploy/oci/k8s/` (legacy; not yet on shared base) |

# Minikube stage-test overlay

Non-production local Kubernetes profile for smoke-testing manifests, Dapr, SQL bootstrap, and gateway routing.

**Not for production.** Uses fixed dev passwords in `stage-dev-secrets.yaml`.

## Quick start

From repository root:

```powershell
.\scripts\deploy-minikube-stage.ps1
```

Core subset (faster — auth + gateway + platform + SQL):

```powershell
.\scripts\deploy-minikube-stage.ps1 -Profile Core
```

Teardown (recommended):

```powershell
.\scripts\teardown-minikube-stage.ps1
```

Destroy the Minikube VM as well:

```powershell
.\scripts\teardown-minikube-stage.ps1 -DeleteMinikube -Wait -Force
```

Shortcut (delegates to teardown script):

```powershell
.\scripts\deploy-minikube-stage.ps1 -Teardown
```

## Prerequisites

- [Minikube](https://minikube.sigs.k8s.io/docs/start/) (Docker driver recommended on Windows)
- kubectl, Helm 3, Docker
- ~12 GB RAM allocated to Minikube (`-MinikubeMemoryMb` default: 12288)
- Optional: [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/)

## After deploy

1. Add hosts entry (run as Administrator if needed):

   ```text
   <minikube-ip> gateway.local
   ```

   The script prints the IP from `minikube ip`.

2. Open gateway (HTTPS, self-signed cert): `https://gateway.local/health`

3. Aspire dashboard UI (port-forward):

   ```powershell
   kubectl port-forward -n myapp-platform svc/aspire-dashboard 18888:18888
   ```

   Then open `http://localhost:18888`

## What this overlay changes

| Item | Change |
|------|--------|
| SQL Server | Lower CPU/RAM via `overlays/minikube/sql` patch (base manifest keeps prod-sized defaults) |
| Images | `myapp-<service>:minikube-stage` (built into Minikube Docker) |
| Secrets / ConfigMap | Stage dev values + `app-config` (missing from base `dev` overlay) |
| Redis secret | Duplicated in `myapp-platform` and `myapp-apps` (Dapr + Redis pod) |

## Omitted by design

- Audit / Agentic services (not in `deploy/k8s/base/apps` yet)
- External Secrets Operator, backups, production TLS
- Real GHCR image pulls (local build only unless `-SkipBuild`)

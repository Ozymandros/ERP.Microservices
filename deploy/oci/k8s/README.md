# OCI OKE Kubernetes Platform (Phase 2)

This directory contains additive Kubernetes assets for OCI OKE platform bootstrap.

## Scope

- Platform bootstrap namespaces, Redis, and baseline labels.
- Dapr component manifests for Redis-backed state store and pub/sub.
- SQL data-plane assets (StatefulSet, bootstrap job, backup template).
- App overlays for all services, gateway ingress, and Aspire Dashboard OTLP wiring.

## Out of scope

- Production-grade secret management automation (ESO bindings are still to be wired).
- Production TLS/cert issuance policies for ingress hosts.
- Final OCI CI auth setup (OIDC/API key) in workflows.

## Apply sequence

```powershell
# 0) Secrets first (replace examples with Vault/ESO-managed values in real envs)
kubectl apply -f deploy/oci/k8s/platform/redis-secrets.example.yaml
kubectl apply -f deploy/oci/k8s/sql/sql-secrets.example.yaml
kubectl apply -f deploy/oci/k8s/apps/overlays/dev/aspire-dashboard-secrets.example.yaml
kubectl apply -f deploy/oci/k8s/apps/overlays/dev/app-secrets.example.yaml

# Optional preferred path: External Secrets + OCI Vault mapping template
# kubectl apply -f deploy/oci/k8s/platform/external-secrets-oci.example.yaml

# 1) Platform (namespaces + redis)
kubectl apply -k deploy/oci/k8s/platform

# 2) Dapr config and components
kubectl apply -f deploy/oci/k8s/dapr/components/dapr-config.yaml
kubectl apply -f deploy/oci/k8s/dapr/components/statestore.yaml
kubectl apply -f deploy/oci/k8s/dapr/components/pubsub.yaml

# 3) SQL data plane
kubectl apply -k deploy/oci/k8s/sql

# 4) Apps + gateway + dashboard (dev)
kubectl apply -k deploy/oci/k8s/apps

# 5) Production ingress/TLS overlay (requires real DNS)
# kubectl apply -k deploy/oci/k8s/apps/overlays/prod
```

## Notes

- `secretKeyRef` is used for Redis and OTLP metadata to avoid committing credentials.
- Dapr `scopes` should match the final app IDs chosen in the app overlays.
- Dapr config now exports sidecar traces to Aspire Dashboard OTLP using `secretKeyRef` from `app-secrets`.
- Replace all `*.example.yaml` secrets with External Secrets / OCI Vault before non-dev rollout.
- Gateway ingress is TLS-enabled in dev via a self-signed cert-manager `Issuer` (`gateway-selfsigned`).
- Production ACME is isolated in `deploy/oci/k8s/apps/overlays/prod` with staging/prod ClusterIssuer templates.

## GitHub Actions workflow

Manual workflow: `.github/workflows/deploy-oci-k8s.yml`

Required repository secrets:

- `OCI_KUBECONFIG_B64`: base64-encoded kubeconfig with cluster access.

Inputs:

- `image_step`: `skip`, `build`, or `build-push` (ARM64 images)
- `app_image_tag`: optional explicit tag (default: short commit SHA)
- `infra_step`: `skip`, `validate`, or `apply` (OpenTofu stage first)
- `profile`: `dev` or `prod`
- `apply_mode`: `validate` (dry-run) or `apply`
- `ingress_domain`: required for `prod`
- `letsencrypt_issuer`: `letsencrypt-staging` or `letsencrypt-prod`

Additional secrets required when `infra_step != skip`:

- `OCI_TENANCY_OCID`
- `OCI_USER_OCID`
- `OCI_FINGERPRINT`
- `OCI_REGION`
- `OCI_API_PRIVATE_KEY_B64`

Image stage notes:

- `build` builds ARM64 images in CI without pushing.
- `build-push` builds and pushes ARM64 images to GHCR.
- `build-push` requires `apply_mode=apply`.
- Kubernetes manifests are patched in CI to use:
  - `ghcr.io/<owner>/<repo>/myapp-<service>:<image_tag>`

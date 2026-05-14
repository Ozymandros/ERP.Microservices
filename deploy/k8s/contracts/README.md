# Secret key contracts

These files document required Kubernetes secret shapes. They are **not** applied directly.

| File | Secret name | Namespace |
|------|-------------|-----------|
| `secret-keys.app-secrets.yaml` | `app-secrets` | `myapp-apps` |
| `secret-keys.sql-secrets.yaml` | `sql-secrets` | `myapp-apps` |

Cloud overlays document additional backup-related keys (S3 on AWS, Object Storage on OCI).

## Service catalog

`services.yaml` is the single source of truth for:

- GitHub Actions image build matrix (`.github/workflows/reusable-build-service-images.yml`)
- Image tag patching (`patch-k8s-image-tags.yml`)

## Helm prerequisites (any cluster)

Install before applying overlays:

1. ingress-nginx
2. cert-manager
3. Dapr
4. External Secrets Operator (optional; required for production secret sync)

## Apply order

```text
1. Secrets (manual or ESO)
2. kubectl apply -k deploy/k8s/base/platform
3. kubectl apply -f deploy/k8s/base/dapr/components/*.yaml
4. kubectl apply -k deploy/k8s/base/sql
5. kubectl apply -k deploy/k8s/overlays/dev   # or prod
```

For AWS, use `deploy/aws/k8s/overlays/*` instead of step 5.

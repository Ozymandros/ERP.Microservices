# AWS EKS Kubernetes overlays (Tier 1)

Thin AWS-specific patches on top of `deploy/k8s/`.

## Cost profiles

| Overlay | Use when | Includes |
|---------|----------|----------|
| `overlays/dev` | **Default — cheap lab / CI** | gp3 PVC (20Gi), reduced SQL CPU/RAM. No S3 backup, no ESO, no IRSA. Use manual Kubernetes secrets. |
| `overlays/prod` | Staging or when you need backups + Secrets Manager | Let's Encrypt overlay + backup CronJob + ESO + IRSA service accounts |

**Estimated infra baseline (dev tofu defaults):** 1× Spot `t3.xlarge` node, no NAT gateway, no S3 backup bucket — control plane (~$73/mo) + one Spot node (~$45–90/mo region-dependent) + small EBS volumes.

Scale up later via `deploy/aws/tofu` variables (`node_max_size`, `enable_nat_gateway`, `create_backup_bucket`, etc.) without changing the generic `deploy/k8s/` layer.

## GitHub Actions + OpenTofu (source of truth)

`deploy-aws-k8s.yml` resolves **cluster name** and **region** via OpenTofu (`tofu console` on `local.eks_cluster_name` and `var.aws_region` from tfvars). Optional override: workflow input `aws_region` or variable `AWS_REGION` (passed as `-var`).

| Setting | How CI gets it |
|---------|----------------|
| Cluster | `local.eks_cluster_name` → `{project}-{environment}-eks` |
| Region | `var.aws_region` (default `eu-west-1` in `variables.tf`) |
| Deploy role ARN | `tofu output` after apply, or repository variable `AWS_DEPLOY_ROLE_ARN` |

**Only bootstrap variable required:**

```powershell
cd deploy/aws/tofu
tofu apply
tofu output -raw github_actions_deploy_role_arn   # → AWS_DEPLOY_ROLE_ARN
```

If the GitHub OIDC provider already exists in your account, set `github_oidc_provider_arn` in `terraform.tfvars` instead of creating a duplicate.

## Cheap dev checklist

1. Apply tofu with `environments/dev/terraform.tfvars.example` defaults (Spot, single node, NAT off).
2. `kubectl apply -k deploy/aws/k8s/overlays/dev`
3. Create secrets manually (`redis-secrets`, `app-secrets`, `sql-secrets`) — see `deploy/k8s/contracts/`.
4. Install only required Helm charts: ingress-nginx, cert-manager, Dapr (skip ESO for dev).

## Validate

```powershell
kubectl kustomize deploy/aws/k8s/overlays/dev
kubectl kustomize deploy/aws/k8s/overlays/prod
```

## Workflow

Manual: `.github/workflows/deploy-aws-k8s.yml` — use `profile=dev` for the minimal overlay.

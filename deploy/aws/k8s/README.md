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

`deploy-aws-k8s.yml` assumes OIDC with `AWS_DEPLOY_ROLE_ARN`, then runs `tofu init` + `tofu output` for cluster/region. If state is not available yet, it falls back to `{project}-{environment}-eks` from the profile tfvars. Optional override: workflow input `aws_region` or variable `AWS_REGION`.

| Setting | How CI gets it |
|---------|----------------|
| Cluster | `tofu output eks_cluster_name` (or `planned_eks_cluster_name` / tfvars naming) |
| Region | `tofu output aws_region` (or tfvars / default `eu-west-1`) |
| Deploy role ARN | Repository variable `AWS_DEPLOY_ROLE_ARN` (from `tofu output` after first apply) |

### Bootstrap (first time — fixes OIDC error)

**CI error:** `No OpenIDConnect provider found in your account for https://token.actions.githubusercontent.com`

GitHub Actions cannot create the OIDC provider without admin AWS access. Pick **one** path:

#### Option A — GitHub workflow (no local AWS CLI)

1. In the repo, add **Actions secrets** (temporary):
   - `AWS_BOOTSTRAP_ACCESS_KEY_ID`
   - `AWS_BOOTSTRAP_SECRET_ACCESS_KEY`
   - (optional) `AWS_BOOTSTRAP_SESSION_TOKEN`
2. Run workflow **Bootstrap AWS Infrastructure (OIDC)** (`.github/workflows/bootstrap-aws-infrastructure.yml`), profile `dev`.
3. Copy `AWS_DEPLOY_ROLE_ARN` from the job **summary** into **Repository variables**.
4. Delete the bootstrap secrets.
5. Re-run **Deploy AWS Kubernetes Stack** (`infra_step=skip` is fine).

Or run **Deploy AWS Kubernetes Stack** with **`bootstrap_oidc=true`** once (same secrets required); it runs bootstrap then deploy in one go.

#### Option B — Local script

```powershell
# AWS admin credentials via aws configure or $Env:AWS_ACCESS_KEY_ID
.\scripts\bootstrap-aws-infrastructure.ps1 -Profile dev
```

#### Option C — Manual OpenTofu

```powershell
cd deploy/aws/tofu
copy environments\dev\terraform.tfvars.example terraform.tfvars
tofu init && tofu apply
tofu output -raw github_actions_deploy_role_arn   # → AWS_DEPLOY_ROLE_ARN
```

Verify: `aws iam list-open-id-connect-providers` includes `token.actions.githubusercontent.com`.

| Cause | Fix |
|-------|-----|
| `AWS_DEPLOY_ROLE_ARN` set before `tofu apply` | Bootstrap (A/B/C), then update the variable |
| Role ARN from another AWS account | Use output from the target account |
| Provider already exists | Set `github_oidc_provider_arn` in `terraform.tfvars` |

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

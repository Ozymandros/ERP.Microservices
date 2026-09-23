# AWS EKS Phase 1 (OpenTofu)

Scaffold for VPC, EKS managed node group, EBS CSI addon, and optional IRSA/S3.

**Default variable values target a cheap non-prod lab** (1× Spot node, no NAT, no backup bucket).

## Modules

| Module | Resources |
|--------|-----------|
| `vpc` | VPC, subnets, IGW, optional single NAT |
| `eks` | EKS cluster, Spot/On-Demand node group, OIDC, EBS CSI |
| `irsa` | Optional SQL backup + External Secrets roles |
| `github-oidc` | GitHub Actions deploy IAM role + EKS cluster admin access entry |

## Remote state (idempotent bootstrap)

Bootstrap and local applies use an **S3 backend** (+ DynamoDB locks) so re-runs converge instead of recreating from empty local state.

| Setting | Default | Override |
|---------|---------|----------|
| State bucket | `myapp-tfstate-{profile}` | Actions var `AWS_TF_STATE_BUCKET` or `-StateBucket` |
| Lock table | `myapp-tf-locks` | Actions var `AWS_TF_LOCK_TABLE` or `-LockTable` |
| State key | `aws/{profile}/eks/terraform.tfstate` | (fixed by convention) |

[`backend.tf`](backend.tf) declares an empty `backend "s3" {}`; CI/scripts pass `-backend-config=...`. Skeleton check (`.github/workflows/deploy-aws-phase1.yml`) still uses `tofu init -backend=false`.

**Re-runs are safe** while the same bucket/key are used. An existing account-wide GitHub OIDC provider (`token.actions.githubusercontent.com`) is **auto-adopted** via `-var=github_oidc_provider_arn=...`.

Stacks created **before** remote state was enabled are orphaned from OpenTofu’s view — import them into state or destroy/recreate; OIDC-only collisions are handled automatically.

## Cost levers

| Variable | Cheap dev | Scale up |
|----------|-----------|----------|
| `enable_nat_gateway` | `false` | `true` (private nodes) |
| `node_capacity_type` | `SPOT` | `ON_DEMAND` |
| `node_desired_size` | `1` | `2+` |
| `node_instance_types` | `["t3.xlarge"]` | `["m6i.large"]` etc. |
| `create_backup_bucket` | `false` | `true` + prod k8s overlay |
| `enable_external_secrets_irsa` | `false` | `true` + prod k8s overlay |

Pair tofu with `deploy/aws/k8s/overlays/dev` for minimal manifests. For prod IRSA/backup, set those two flags (and use a prod tfvars profile).

## Local run

Prefer the bootstrap script (creates bucket/table, adopts OIDC, applies with remote state):

```powershell
.\scripts\bootstrap-aws-infrastructure.ps1 -Profile dev
# optional: -StateBucket my-bucket -LockTable my-locks -PlanOnly
```

Or manually:

```powershell
cd deploy/aws/tofu
copy environments\dev\terraform.tfvars.example terraform.tfvars
# Ensure S3 bucket + DynamoDB lock table exist, then:
tofu init -backend-config="bucket=myapp-tfstate-dev" `
  -backend-config="key=aws/dev/eks/terraform.tfstate" `
  -backend-config="region=eu-west-1" `
  -backend-config="dynamodb_table=myapp-tf-locks" `
  -backend-config="encrypt=true"
tofu fmt -check -recursive
tofu validate -var-file=environments/dev/terraform.tfvars.example
tofu plan -var-file=environments/dev/terraform.tfvars.example
```

## GitHub Actions

Run `.github/workflows/bootstrap-aws-infrastructure.yml` (or the script above) with admin AWS credentials. Re-running the same profile is **idempotent**. That creates/updates the GitHub OIDC provider and deploy role.

Then set repository variable `AWS_DEPLOY_ROLE_ARN` from:

```powershell
tofu output -raw github_actions_deploy_role_arn
```

CI reads cluster/region from OpenTofu state (`tofu output` after OIDC auth), with tfvars fallback when state is empty.

See `deploy/aws/k8s/README.md`.

| Variable | Default |
|----------|---------|
| `enable_github_actions_deploy` | `true` |
| `github_repository` | `Ozymandros/ERP.Microservices` |
| `github_oidc_provider_arn` | `""` (create provider; set ARN if one already exists — bootstrap auto-detects) |

## CI

- `.github/workflows/deploy-aws-phase1.yml` (validate only, `-backend=false`)
- `.github/workflows/bootstrap-aws-infrastructure.yml` (idempotent apply + remote state)
- `.github/workflows/deploy-aws-k8s.yml` (`profile=dev` for cheap overlay)

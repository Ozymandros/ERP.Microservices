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

## Cost levers

| Variable | Cheap dev | Scale up |
|----------|-----------|----------|
| `enable_nat_gateway` | `false` | `true` (private nodes) |
| `node_capacity_type` | `SPOT` | `ON_DEMAND` |
| `node_desired_size` | `1` | `2+` |
| `node_instance_types` | `["t3.xlarge"]` | `["m6i.large"]` etc. |
| `create_backup_bucket` | `false` | `true` + prod k8s overlay |
| `enable_external_secrets_irsa` | `false` | `true` + prod k8s overlay |

Pair tofu with `deploy/aws/k8s/overlays/dev` for minimal manifests.

## Local run

```powershell
cd deploy/aws/tofu
copy environments\dev\terraform.tfvars.example terraform.tfvars
tofu init
tofu fmt -check -recursive
tofu validate
tofu plan
```

## GitHub Actions

**First deploy:** run `.github/workflows/bootstrap-aws-infrastructure.yml` (or `scripts/bootstrap-aws-infrastructure.ps1` locally) with admin AWS credentials. That creates the GitHub OIDC provider and deploy role.

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
| `github_oidc_provider_arn` | `""` (create provider; set ARN if one already exists) |

## CI

- `.github/workflows/deploy-aws-phase1.yml`
- `.github/workflows/deploy-aws-k8s.yml` (`profile=dev` for cheap overlay)

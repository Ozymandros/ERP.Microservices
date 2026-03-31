# OCI OKE Phase 1 (OpenTofu Scaffold)

This directory contains an additive OCI foundation for:

- compartment (create or reuse),
- network (VCN, public/private subnets, gateways, route tables),
- OKE cluster and node pool.

The Azure path remains unchanged (`infra/` + `.github/workflows/deploy.yml`).

## Phase 1 boundaries

Included:

- OpenTofu root and modules for `compartment`, `network`, `oke`.
- `dev` environment examples.
- Local-first execution flow.

Not included yet:

- Dapr, Ingress, ESO platform stack.
- SQL Server StatefulSet and backups.
- Application manifests and Aspire dashboard deployment.
- OCI GitHub authentication wiring.

## Azure parity mapping (concise)

| Azure Bicep concept | OCI/OpenTofu Phase 1 equivalent |
| --- | --- |
| Subscription-scoped deployment | Tenancy + dedicated/reused compartment |
| Resource group naming | Compartment naming (`cmp-<project>-<env>-core`) |
| Container Apps environment baseline | OKE cluster baseline |
| Network modules | VCN + subnet/gateway modules |

## Prerequisites

- OpenTofu installed (`tofu` on PATH).
- OCI API key credentials configured locally.
- Existing or target OCI region selected.

## Local-first runbook

From repository root:

```powershell
cd deploy/oci/tofu
copy environments/dev/terraform.tfvars.example terraform.tfvars
tofu init
tofu fmt -check
tofu validate
tofu plan
tofu apply
```

To use remote state later, copy `environments/dev/backend.tf.example` to `backend.tf` and set your Object Storage values before `tofu init -reconfigure`.

## Free Tier note

Before apply, verify current Always Free quotas in your tenancy/region:

- Ampere A1 compute capacity for node pool shape.
- Block volume allowance for future PVC workloads.
- Object Storage allowance for future backup retention.

Use the OCI docs and console quota pages as source of truth for your account.

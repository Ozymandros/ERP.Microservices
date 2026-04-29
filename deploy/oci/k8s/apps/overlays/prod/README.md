# Prod Overlay (Let's Encrypt Ready)

This overlay keeps dev resources but patches gateway ingress to use a Let's Encrypt
`ClusterIssuer` and a real DNS host.

## Important prerequisites

- A real public DNS record pointing to the OCI ingress public IP.
- cert-manager installed and healthy.
- NGINX ingress installed and reachable from the internet.
- Open ports for HTTP challenge (`80`) and TLS (`443`).

## Usage

```powershell
# Validate challenges first in staging:
kubectl apply -k deploy/oci/k8s/apps/overlays/prod

# Then switch patch annotation from letsencrypt-staging to letsencrypt-prod.
```

## Why this split works

- `dev` overlay continues using `selfSigned` certificates and works without public DNS.
- `prod` overlay adds ACME issuers and patches only ingress-specific fields.
- You avoid breaking dev while keeping production-ready manifests in-repo.

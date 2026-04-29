# SQL Server Data Plane (Phase 3)

This directory provides the Phase 3 baseline for SQL Server on OKE using a single replica and persistent storage.

## Included assets

- `statefulset-mssql.yaml`: SQL Server 2022 StatefulSet with `fsGroup: 10001`.
- `sql-pvc.yaml`: `ReadWriteOnce` PVC request set to `50Gi`.
- `sql-service.yaml`: ClusterIP service and headless service.
- `job-bootstrap-databases.yaml`: one-time job creating six service databases and `sqladmin` login/user mapping.
- `backup-cronjob.yaml`: template CronJob for nightly backups to OCI Object Storage (S3-compatible endpoint).
- `sql-secrets.example.yaml`: secret contract example.

## Apply order

```powershell
kubectl apply -f deploy/oci/k8s/sql/sql-secrets.example.yaml
kubectl apply -f deploy/oci/k8s/sql/sql-pvc.yaml
kubectl apply -f deploy/oci/k8s/sql/sql-service.yaml
kubectl apply -f deploy/oci/k8s/sql/statefulset-mssql.yaml
kubectl apply -f deploy/oci/k8s/sql/job-bootstrap-databases.yaml
kubectl apply -f deploy/oci/k8s/sql/backup-cronjob.yaml
```

## Connection string parity contract

The bootstrap job creates:

- Databases: `AuthDb`, `BillingDb`, `InventoryDb`, `OrdersDb`, `PurchasingDb`, `SalesDb`, `CrmDb`.
- Server login: `sqladmin` (password aligned with `sa-password` secret value).
- Database user `sqladmin` in each database with `db_owner`.

This keeps compatibility with existing secret shapes like:

`Server=sqlserver,1433;Database=AuthDb;User Id=sqladmin;Password=...;TrustServerCertificate=True;`

## Notes

- Replace `sql-secrets.example.yaml` with an ExternalSecret/Vault-managed secret before production.
- Add storage class and retention policies explicitly for your OCI environment as needed.
- The backup CronJob image is a template placeholder and must include both `sqlcmd` and `aws` CLI.

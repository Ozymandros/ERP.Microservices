terraform {
  # Partial config — pass bucket/key/region/dynamodb_table via -backend-config
  # (CI and scripts/bootstrap-aws-infrastructure.ps1).
  backend "s3" {}
}

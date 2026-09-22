output "vpc_id" {
  value = module.vpc.vpc_id
}

output "eks_cluster_name" {
  description = "Live cluster name (after apply). Same as planned_eks_cluster_name when infra matches tfvars."
  value       = module.eks.cluster_name
}

output "planned_eks_cluster_name" {
  description = "Cluster name from naming convention (same as eks_cluster_name once infra matches tfvars)."
  value       = local.eks_cluster_name
}

output "eks_cluster_endpoint" {
  value = module.eks.cluster_endpoint
}

output "eks_cluster_arn" {
  value = module.eks.cluster_arn
}

output "oidc_provider_arn" {
  value = module.eks.oidc_provider_arn
}

output "node_subnet_ids" {
  description = "Subnets used by the node group (public when NAT is disabled)."
  value       = local.node_subnet_ids
}

output "sql_backup_role_arn" {
  value = module.irsa.sql_backup_role_arn
}

output "external_secrets_role_arn" {
  value = module.irsa.external_secrets_role_arn
}

output "backup_bucket_name" {
  value = var.create_backup_bucket ? aws_s3_bucket.sql_backups[0].bucket : null
}

output "aws_region" {
  description = "Set as GitHub Actions variable AWS_REGION."
  value       = var.aws_region
}

output "github_actions_deploy_role_arn" {
  description = "Set as GitHub Actions variable AWS_DEPLOY_ROLE_ARN (Repository variables, not Secrets)."
  value       = var.enable_github_actions_deploy ? module.github_oidc[0].deploy_role_arn : null
}

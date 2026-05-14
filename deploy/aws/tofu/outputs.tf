output "vpc_id" {
  value = module.vpc.vpc_id
}

output "eks_cluster_name" {
  value = module.eks.cluster_name
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

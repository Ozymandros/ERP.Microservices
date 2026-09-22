locals {
  cluster_subnet_ids = concat(module.vpc.private_subnet_ids, module.vpc.public_subnet_ids)
  node_subnet_ids    = var.enable_nat_gateway ? module.vpc.private_subnet_ids : module.vpc.public_subnet_ids
}

module "vpc" {
  source = "./modules/vpc"

  name_prefix          = local.name_prefix
  vpc_cidr             = var.vpc_cidr
  public_subnet_cidrs  = var.public_subnet_cidrs
  private_subnet_cidrs = var.private_subnet_cidrs
  enable_nat_gateway   = var.enable_nat_gateway
  tags                 = local.base_tags
}

module "eks" {
  source = "./modules/eks"

  name_prefix          = local.name_prefix
  kubernetes_version   = var.kubernetes_version
  vpc_id               = module.vpc.vpc_id
  cluster_subnet_ids   = local.cluster_subnet_ids
  node_subnet_ids      = local.node_subnet_ids
  node_instance_types  = var.node_instance_types
  node_capacity_type   = var.node_capacity_type
  node_desired_size    = var.node_desired_size
  node_min_size        = var.node_min_size
  node_max_size        = var.node_max_size
  public_access_cidrs  = var.eks_public_access_cidrs
  tags                 = local.base_tags
}

resource "aws_s3_bucket" "sql_backups" {
  count  = var.create_backup_bucket ? 1 : 0
  bucket = var.backup_bucket_name != "" ? var.backup_bucket_name : "${local.name_prefix}-sql-backups"
}

resource "aws_s3_bucket_public_access_block" "sql_backups" {
  count  = var.create_backup_bucket ? 1 : 0
  bucket = aws_s3_bucket.sql_backups[0].id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "sql_backups" {
  count  = var.create_backup_bucket ? 1 : 0
  bucket = aws_s3_bucket.sql_backups[0].id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_versioning" "sql_backups" {
  count  = var.create_backup_bucket ? 1 : 0
  bucket = aws_s3_bucket.sql_backups[0].id

  versioning_configuration {
    status = "Disabled"
  }
}

module "irsa" {
  source = "./modules/irsa"

  name_prefix                  = local.name_prefix
  oidc_provider_arn            = module.eks.oidc_provider_arn
  oidc_provider_url            = module.eks.oidc_provider_url
  backup_bucket_arn            = var.create_backup_bucket ? aws_s3_bucket.sql_backups[0].arn : ""
  secrets_manager_prefix       = "myapp/${var.environment}/"
  create_sql_backup_role       = var.create_backup_bucket
  create_external_secrets_role = var.enable_external_secrets_irsa
  tags                         = local.base_tags
}

module "github_oidc" {
  count  = var.enable_github_actions_deploy ? 1 : 0
  source = "./modules/github-oidc"

  name_prefix              = local.name_prefix
  github_repository        = var.github_repository
  eks_cluster_name         = module.eks.cluster_name
  eks_cluster_arn          = module.eks.cluster_arn
  github_oidc_provider_arn = var.github_oidc_provider_arn
  tags                     = local.base_tags

  depends_on = [module.eks]
}

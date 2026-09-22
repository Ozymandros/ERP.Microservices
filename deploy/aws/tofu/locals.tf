locals {
  name_prefix                     = "${var.project}-${var.environment}"
  eks_cluster_name                = "${local.name_prefix}-eks"
  github_actions_deploy_role_name = "${local.name_prefix}-github-actions-deploy"
  base_tags = merge(
    {
      Project     = var.project
      Environment = var.environment
      ManagedBy   = "opentofu"
    },
    var.tags,
  )
}

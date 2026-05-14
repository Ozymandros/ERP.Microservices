locals {
  name_prefix = "${var.project}-${var.environment}"
  base_tags = merge(
    {
      Project     = var.project
      Environment = var.environment
      ManagedBy   = "opentofu"
    },
    var.tags,
  )
}

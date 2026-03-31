locals {
  name_prefix      = "${var.project}-${var.environment}"
  compartment_name = var.compartment_name != "" ? var.compartment_name : "cmp-${local.name_prefix}-core"

  base_tags = merge({
    "gitops-env" = var.environment
    "project"    = var.project
    "managed-by" = "opentofu"
  }, var.freeform_tags)
}

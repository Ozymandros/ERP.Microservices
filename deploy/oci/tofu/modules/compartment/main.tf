locals {
  create_compartment = var.existing_compartment_ocid == ""
}

resource "oci_identity_compartment" "this" {
  count = local.create_compartment ? 1 : 0

  compartment_id = var.tenancy_ocid
  description    = var.compartment_description
  name           = var.compartment_name
  enable_delete  = true
  freeform_tags  = var.freeform_tags
}

locals {
  created_compartment_ocid = try(oci_identity_compartment.this[0].id, null)
}

output "compartment_ocid" {
  description = "Compartment OCID used by downstream modules."
  value       = var.existing_compartment_ocid != "" ? var.existing_compartment_ocid : local.created_compartment_ocid
}

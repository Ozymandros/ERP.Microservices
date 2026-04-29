output "vcn_ocid" {
  description = "VCN OCID."
  value       = oci_core_vcn.this.id
}

output "public_subnet_ocid" {
  description = "Public subnet OCID."
  value       = oci_core_subnet.public.id
}

output "private_subnet_ocid" {
  description = "Private subnet OCID."
  value       = oci_core_subnet.private.id
}

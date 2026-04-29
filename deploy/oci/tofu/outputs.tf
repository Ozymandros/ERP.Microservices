output "compartment_ocid" {
  description = "Compartment used for OCI resources."
  value       = module.compartment.compartment_ocid
}

output "vcn_ocid" {
  description = "Created VCN OCID."
  value       = module.network.vcn_ocid
}

output "oke_cluster_id" {
  description = "OKE cluster OCID."
  value       = module.oke.cluster_id
}

output "oke_cluster_name" {
  description = "OKE cluster name."
  value       = module.oke.cluster_name
}

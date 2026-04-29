variable "compartment_ocid" {
  description = "Target compartment OCID."
  type        = string
}

variable "name_prefix" {
  description = "Global naming prefix."
  type        = string
}

variable "vcn_ocid" {
  description = "VCN OCID for OKE cluster."
  type        = string
}

variable "kubernetes_version" {
  description = "Kubernetes version to provision."
  type        = string
}

variable "node_pool_size" {
  description = "Worker node count."
  type        = number
}

variable "node_shape" {
  description = "Worker node shape."
  type        = string
}

variable "node_ocpus" {
  description = "Node OCPUs for flexible shape."
  type        = number
}

variable "node_memory_gbs" {
  description = "Node memory for flexible shape."
  type        = number
}

variable "image_id" {
  description = "Optional node image OCID."
  type        = string
  default     = ""
}

variable "public_subnet_ocid" {
  description = "Public subnet for OKE endpoint."
  type        = string
}

variable "private_subnet_ocid" {
  description = "Private subnet for worker nodes."
  type        = string
}

variable "freeform_tags" {
  description = "Resource tags."
  type        = map(string)
  default     = {}
}

variable "compartment_ocid" {
  description = "Target compartment OCID."
  type        = string
}

variable "name_prefix" {
  description = "Global naming prefix."
  type        = string
}

variable "vcn_cidr" {
  description = "VCN CIDR block."
  type        = string
}

variable "public_subnet_cidr" {
  description = "Public subnet CIDR block."
  type        = string
}

variable "private_subnet_cidr" {
  description = "Private subnet CIDR block."
  type        = string
}

variable "freeform_tags" {
  description = "Resource tags."
  type        = map(string)
  default     = {}
}

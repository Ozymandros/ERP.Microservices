variable "tenancy_ocid" {
  description = "Root tenancy OCID."
  type        = string
}

variable "existing_compartment_ocid" {
  description = "Existing compartment OCID. When provided, this module does not create a compartment."
  type        = string
  default     = ""
}

variable "compartment_name" {
  description = "Compartment name when creating one."
  type        = string
}

variable "compartment_description" {
  description = "Compartment description."
  type        = string
}

variable "freeform_tags" {
  description = "Resource tags."
  type        = map(string)
  default     = {}
}

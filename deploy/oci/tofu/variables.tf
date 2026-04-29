variable "tenancy_ocid" {
  description = "OCI tenancy OCID."
  type        = string
}

variable "user_ocid" {
  description = "OCI user OCID for local auth profile."
  type        = string
}

variable "fingerprint" {
  description = "API key fingerprint for OCI user."
  type        = string
}

variable "private_key_path" {
  description = "Path to local OCI API private key."
  type        = string
}

variable "region" {
  description = "OCI region, for example eu-madrid-1."
  type        = string
}

variable "project" {
  description = "Project logical name."
  type        = string
  default     = "myapp"
}

variable "environment" {
  description = "Environment slug."
  type        = string
  default     = "dev"
}

variable "freeform_tags" {
  description = "Base OCI freeform tags."
  type        = map(string)
  default     = {}
}

variable "existing_compartment_ocid" {
  description = "Existing compartment OCID. If set, no new compartment is created."
  type        = string
  default     = ""
}

variable "compartment_name" {
  description = "Compartment display name if created."
  type        = string
  default     = ""
}

variable "compartment_description" {
  description = "Compartment description."
  type        = string
  default     = "OCI resources for ERP microservices."
}

variable "vcn_cidr" {
  description = "VCN CIDR block."
  type        = string
  default     = "10.60.0.0/16"
}

variable "public_subnet_cidr" {
  description = "Public subnet CIDR."
  type        = string
  default     = "10.60.1.0/24"
}

variable "private_subnet_cidr" {
  description = "Private subnet CIDR for worker nodes."
  type        = string
  default     = "10.60.2.0/24"
}

variable "kubernetes_version" {
  description = "OKE Kubernetes version."
  type        = string
  default     = "v1.30.1"
}

variable "node_pool_size" {
  description = "Number of worker nodes."
  type        = number
  default     = 2
}

variable "node_shape" {
  description = "Node pool VM shape."
  type        = string
  default     = "VM.Standard.A1.Flex"
}

variable "node_ocpus" {
  description = "Node OCPUs for flexible shapes."
  type        = number
  default     = 2
}

variable "node_memory_gbs" {
  description = "Node memory in GB for flexible shapes."
  type        = number
  default     = 12
}

variable "image_id" {
  description = "Optional worker node image OCID. Keep empty for provider default."
  type        = string
  default     = ""
}

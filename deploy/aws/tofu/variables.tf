variable "aws_region" {
  description = "AWS region for all resources."
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

variable "tags" {
  description = "Additional resource tags."
  type        = map(string)
  default     = {}
}

variable "vpc_cidr" {
  description = "VPC CIDR block."
  type        = string
  default     = "10.70.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "Public subnet CIDRs (EKS requires at least two AZs)."
  type        = list(string)
  default     = ["10.70.1.0/24", "10.70.2.0/24"]
}

variable "private_subnet_cidrs" {
  description = "Private subnet CIDRs (used when NAT gateway is enabled)."
  type        = list(string)
  default     = ["10.70.11.0/24", "10.70.12.0/24"]
}

variable "enable_nat_gateway" {
  description = "NAT gateway + EIP (~$32/mo). Disable for cheap dev; nodes use public subnets."
  type        = bool
  default     = false
}

variable "kubernetes_version" {
  description = "EKS Kubernetes version."
  type        = string
  default     = "1.30"
}

variable "node_instance_types" {
  description = "EKS managed node group instance types. t3.xlarge fits SQL + dev stack on one node."
  type        = list(string)
  default     = ["t3.xlarge"]
}

variable "node_capacity_type" {
  description = "ON_DEMAND or SPOT (Spot is cheaper for non-prod)."
  type        = string
  default     = "SPOT"

  validation {
    condition     = contains(["ON_DEMAND", "SPOT"], var.node_capacity_type)
    error_message = "node_capacity_type must be ON_DEMAND or SPOT."
  }
}

variable "node_desired_size" {
  type    = number
  default = 1
}

variable "node_min_size" {
  type    = number
  default = 1
}

variable "node_max_size" {
  description = "Upper bound for cluster-autoscaler / manual scale-out."
  type        = number
  default     = 2
}

variable "create_backup_bucket" {
  description = "S3 bucket for SQL backups (prod overlay). Off by default for cheap dev."
  type        = bool
  default     = false
}

variable "backup_bucket_name" {
  description = "Optional explicit S3 bucket name when create_backup_bucket is true."
  type        = string
  default     = ""
}

variable "enable_external_secrets_irsa" {
  description = "IAM role for External Secrets Operator (prod overlay)."
  type        = bool
  default     = false
}

variable "name_prefix" {
  type = string
}

variable "github_repository" {
  description = "GitHub repo allowed to assume the deploy role (org/repo)."
  type        = string
}

variable "eks_cluster_name" {
  description = "EKS cluster name for access entry and kubectl."
  type        = string
}

variable "eks_cluster_arn" {
  description = "EKS cluster ARN (scopes eks:DescribeCluster)."
  type        = string
}

variable "github_oidc_provider_arn" {
  description = "Existing GitHub OIDC provider ARN. Leave empty to create one in this account."
  type        = string
  default     = ""
}

variable "tags" {
  type    = map(string)
  default = {}
}

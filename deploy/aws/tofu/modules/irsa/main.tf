variable "name_prefix" { type = string }
variable "oidc_provider_arn" { type = string }
variable "oidc_provider_url" { type = string }
variable "backup_bucket_arn" { type = string }
variable "secrets_manager_prefix" { type = string }
variable "create_sql_backup_role" { type = bool }
variable "create_external_secrets_role" { type = bool }
variable "tags" { type = map(string) }

locals {
  oidc_provider_host = replace(var.oidc_provider_url, "https://", "")
}

data "aws_iam_policy_document" "sql_backup_assume" {
  count = var.create_sql_backup_role ? 1 : 0

  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]
    principals {
      type        = "Federated"
      identifiers = [var.oidc_provider_arn]
    }
    condition {
      test     = "StringEquals"
      variable = "${local.oidc_provider_host}:sub"
      values   = ["system:serviceaccount:myapp-apps:sql-backup"]
    }
    condition {
      test     = "StringEquals"
      variable = "${local.oidc_provider_host}:aud"
      values   = ["sts.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "sql_backup" {
  count              = var.create_sql_backup_role ? 1 : 0
  name               = "${var.name_prefix}-sql-backup"
  assume_role_policy = data.aws_iam_policy_document.sql_backup_assume[0].json
  tags               = var.tags
}

data "aws_iam_policy_document" "sql_backup" {
  count = var.create_sql_backup_role ? 1 : 0

  statement {
    actions   = ["s3:PutObject", "s3:AbortMultipartUpload"]
    resources = ["${var.backup_bucket_arn}/*"]
  }
}

resource "aws_iam_role_policy" "sql_backup" {
  count  = var.create_sql_backup_role ? 1 : 0
  name   = "${var.name_prefix}-sql-backup"
  role   = aws_iam_role.sql_backup[0].id
  policy = data.aws_iam_policy_document.sql_backup[0].json
}

data "aws_iam_policy_document" "external_secrets_assume" {
  count = var.create_external_secrets_role ? 1 : 0

  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]
    principals {
      type        = "Federated"
      identifiers = [var.oidc_provider_arn]
    }
    condition {
      test     = "StringEquals"
      variable = "${local.oidc_provider_host}:aud"
      values   = ["sts.amazonaws.com"]
    }
    condition {
      test     = "StringLike"
      variable = "${local.oidc_provider_host}:sub"
      values = [
        "system:serviceaccount:myapp-apps:external-secrets",
        "system:serviceaccount:myapp-platform:external-secrets",
      ]
    }
  }
}

resource "aws_iam_role" "external_secrets" {
  count              = var.create_external_secrets_role ? 1 : 0
  name               = "${var.name_prefix}-external-secrets"
  assume_role_policy = data.aws_iam_policy_document.external_secrets_assume[0].json
  tags               = var.tags
}

data "aws_iam_policy_document" "external_secrets" {
  count = var.create_external_secrets_role ? 1 : 0

  statement {
    actions   = ["secretsmanager:GetSecretValue", "secretsmanager:DescribeSecret"]
    resources = ["arn:aws:secretsmanager:*:*:secret:${var.secrets_manager_prefix}*"]
  }
}

resource "aws_iam_role_policy" "external_secrets" {
  count  = var.create_external_secrets_role ? 1 : 0
  name   = "${var.name_prefix}-external-secrets"
  role   = aws_iam_role.external_secrets[0].id
  policy = data.aws_iam_policy_document.external_secrets[0].json
}

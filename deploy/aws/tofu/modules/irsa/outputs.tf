output "sql_backup_role_arn" {
  value = length(aws_iam_role.sql_backup) > 0 ? aws_iam_role.sql_backup[0].arn : null
}

output "external_secrets_role_arn" {
  value = length(aws_iam_role.external_secrets) > 0 ? aws_iam_role.external_secrets[0].arn : null
}

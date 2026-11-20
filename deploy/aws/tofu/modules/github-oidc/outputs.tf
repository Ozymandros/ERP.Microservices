output "deploy_role_arn" {
  description = "Set as GitHub Actions variable AWS_DEPLOY_ROLE_ARN."
  value       = aws_iam_role.github_actions_deploy.arn
}

output "oidc_provider_arn" {
  value = local.github_oidc_provider_arn
}

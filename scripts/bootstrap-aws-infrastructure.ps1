#Requires -Version 7.0
<#
.SYNOPSIS
  Local bootstrap: OpenTofu apply for EKS + GitHub OIDC provider + deploy role.

.DESCRIPTION
  Run once with admin AWS credentials (aws configure or env vars).
  Then set GitHub repository variable AWS_DEPLOY_ROLE_ARN from output.
#>
param(
    [ValidateSet('dev', 'prod')]
    [string] $Profile = 'dev',
    [string] $AwsRegion = '',
    [switch] $PlanOnly
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$TofuDir = Join-Path $Root 'deploy\aws\tofu'
Set-Location $TofuDir

$tfvars = Join-Path $TofuDir "environments\$Profile\terraform.tfvars"
if (-not (Test-Path $tfvars)) {
    $tfvars = Join-Path $TofuDir "environments\$Profile\terraform.tfvars.example"
}
if (-not (Test-Path $tfvars)) {
    throw "No tfvars found for profile $Profile"
}

$varArgs = @("-var-file=$tfvars")
if ($AwsRegion) {
    $varArgs += "-var=aws_region=$AwsRegion"
}

Write-Host "Using tfvars: $tfvars" -ForegroundColor Cyan
tofu fmt -recursive
tofu init -input=false
tofu validate @varArgs
if ($PlanOnly) {
    tofu plan @varArgs
    exit 0
}
tofu apply -auto-approve @varArgs

$role = tofu output -raw github_actions_deploy_role_arn
$cluster = tofu output -raw eks_cluster_name
$region = tofu output -raw aws_region

Write-Host ""
Write-Host "Bootstrap complete." -ForegroundColor Green
Write-Host "  EKS cluster : $cluster"
Write-Host "  Region      : $region"
Write-Host ""
Write-Host "Set GitHub repository variable:" -ForegroundColor Yellow
Write-Host "  AWS_DEPLOY_ROLE_ARN = $role"

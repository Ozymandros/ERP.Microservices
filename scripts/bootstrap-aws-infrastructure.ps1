#Requires -Version 7.0
<#
.SYNOPSIS
  Local bootstrap: OpenTofu apply for EKS + GitHub OIDC provider + deploy role.

.DESCRIPTION
  Idempotent apply with admin AWS credentials (aws configure or env vars).
  Ensures an S3 state bucket + DynamoDB lock table, adopts an existing GitHub
  OIDC provider when present, then plan/apply. Set GitHub repository variable
  AWS_DEPLOY_ROLE_ARN from the printed output.
#>
param(
    [ValidateSet('dev', 'prod')]
    [string] $Profile = 'dev',
    [string] $AwsRegion = '',
    [string] $StateBucket = '',
    [string] $LockTable = '',
    [switch] $SkipGithubOidc,
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
    throw "No tfvars found for profile $Profile. Add environments\$Profile\terraform.tfvars(.example)."
}

function Read-TfVar {
    param([string] $Name, [string] $File, [string] $Default = '')
    if (-not (Test-Path $File)) { return $Default }
    $line = Get-Content $File | Where-Object { $_ -match "^\s*$Name\s*=" } | Select-Object -Last 1
    if (-not $line) { return $Default }
    if ($line -match '=\s*"?([^"#[\s]+)"?') { return $Matches[1].Trim("'") }
    return $Default
}

if (-not $AwsRegion) {
    $AwsRegion = Read-TfVar -Name 'aws_region' -File $tfvars -Default 'eu-west-1'
}
if (-not $StateBucket) {
    $StateBucket = "myapp-tfstate-$Profile"
}
if (-not $LockTable) {
    $LockTable = 'myapp-tf-locks'
}
$StateKey = "aws/$Profile/eks/terraform.tfstate"

Write-Host "Using tfvars: $tfvars" -ForegroundColor Cyan
Write-Host "State: s3://$StateBucket/$StateKey (lock=$LockTable, region=$AwsRegion)" -ForegroundColor Cyan

# Ensure S3 backend bucket
$bucketExists = $true
try {
    aws s3api head-bucket --bucket $StateBucket 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { $bucketExists = $false }
} catch {
    $bucketExists = $false
}
if (-not $bucketExists) {
    Write-Host "Creating S3 bucket $StateBucket..." -ForegroundColor Yellow
    if ($AwsRegion -eq 'us-east-1') {
        aws s3api create-bucket --bucket $StateBucket | Out-Null
    } else {
        aws s3api create-bucket --bucket $StateBucket `
            --create-bucket-configuration "LocationConstraint=$AwsRegion" | Out-Null
    }
    if ($LASTEXITCODE -ne 0) { throw "Failed to create bucket $StateBucket" }
    aws s3api put-bucket-versioning --bucket $StateBucket `
        --versioning-configuration Status=Enabled | Out-Null
    aws s3api put-bucket-encryption --bucket $StateBucket `
        --server-side-encryption-configuration '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}' | Out-Null
    aws s3api put-public-access-block --bucket $StateBucket `
        --public-access-block-configuration `
        'BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true' | Out-Null
}

# Ensure DynamoDB lock table
$tableExists = $true
try {
    aws dynamodb describe-table --table-name $LockTable 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { $tableExists = $false }
} catch {
    $tableExists = $false
}
if (-not $tableExists) {
    Write-Host "Creating DynamoDB lock table $LockTable..." -ForegroundColor Yellow
    aws dynamodb create-table `
        --table-name $LockTable `
        --attribute-definitions AttributeName=LockID,AttributeType=S `
        --key-schema AttributeName=LockID,KeyType=HASH `
        --billing-mode PAY_PER_REQUEST | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to create lock table $LockTable" }
    aws dynamodb wait table-exists --table-name $LockTable
}

# Adopt existing GitHub OIDC provider when present
$oidcArn = ''
$providerList = aws iam list-open-id-connect-providers --query 'OpenIDConnectProviderList[].Arn' --output text
if ($LASTEXITCODE -eq 0 -and $providerList) {
    foreach ($arn in ($providerList -split '\s+')) {
        if ($arn -like '*:oidc-provider/token.actions.githubusercontent.com') {
            $oidcArn = $arn
            Write-Host "Adopting existing GitHub OIDC provider: $oidcArn" -ForegroundColor Cyan
            break
        }
    }
}

$varArgs = [System.Collections.Generic.List[string]]::new()
$varArgs.Add("-var-file=$tfvars")
$varArgs.Add("-var=aws_region=$AwsRegion")
if ($SkipGithubOidc) {
    $varArgs.Add('-var=enable_github_actions_deploy=false')
} else {
    $varArgs.Add('-var=enable_github_actions_deploy=true')
}
if ($oidcArn) {
    $varArgs.Add("-var=github_oidc_provider_arn=$oidcArn")
}

tofu fmt -recursive
tofu init -input=false `
    "-backend-config=bucket=$StateBucket" `
    "-backend-config=key=$StateKey" `
    "-backend-config=region=$AwsRegion" `
    "-backend-config=dynamodb_table=$LockTable" `
    '-backend-config=encrypt=true'
if ($LASTEXITCODE -ne 0) { throw 'tofu init failed' }

tofu validate @varArgs
if ($LASTEXITCODE -ne 0) { throw 'tofu validate failed' }

if ($PlanOnly) {
    tofu plan @varArgs
    exit $LASTEXITCODE
}

tofu apply -auto-approve @varArgs
if ($LASTEXITCODE -ne 0) { throw 'tofu apply failed' }

$cluster = tofu output -raw eks_cluster_name
$region = tofu output -raw aws_region

Write-Host ""
Write-Host "Bootstrap complete (idempotent)." -ForegroundColor Green
Write-Host "  EKS cluster : $cluster"
Write-Host "  Region      : $region"
Write-Host "  State       : s3://$StateBucket/$StateKey"
Write-Host ""

if (-not $SkipGithubOidc) {
    $role = tofu output -raw github_actions_deploy_role_arn
    Write-Host "Set GitHub repository variable:" -ForegroundColor Yellow
    Write-Host "  AWS_DEPLOY_ROLE_ARN = $role"
    Write-Host ""
    Write-Host "Optional overrides: AWS_TF_STATE_BUCKET / AWS_TF_LOCK_TABLE (Actions variables),"
    Write-Host "  or -StateBucket / -LockTable on this script."
}

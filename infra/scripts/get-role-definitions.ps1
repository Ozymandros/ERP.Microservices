#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Retrieves Azure built-in role definition IDs by role name.
    
.DESCRIPTION
    This script queries Azure for built-in role definitions and returns their IDs.
    This is used to pass role IDs to Bicep templates instead of hardcoding them.
    
.PARAMETER RoleNames
    Array of role names to look up (e.g., 'AcrPull', 'Key Vault Secrets User')
    
.PARAMETER SubscriptionId
    Azure subscription ID (optional, uses current context if not specified)
    
.EXAMPLE
    # Get single role ID
    & ./scripts/get-role-definitions.ps1 -RoleNames 'AcrPull'
    
    # Get multiple role IDs
    & ./scripts/get-role-definitions.ps1 -RoleNames @('AcrPull', 'Key Vault Secrets User', 'App Configuration Data Reader')
    
    # Get with specific subscription
    & ./scripts/get-role-definitions.ps1 -RoleNames 'AcrPull' -SubscriptionId 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
    
.OUTPUTS
    PSCustomObject with properties: RoleName, RoleId, Description
    
.NOTES
    Requires: Azure CLI or Azure PowerShell module
    Author: Infrastructure Team
    Date: 2025-10-30
#>

param(
    [Parameter(Mandatory = $true)]
    [string[]]$RoleNames,
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet('cli', 'powershell', 'auto')]
    [string]$UseCommand = 'auto'
)

# Determine which command-line tool to use
function Get-AvailableCommand {
    param([string]$Tool)
    
    $cmd = Get-Command $Tool -ErrorAction SilentlyContinue
    return $null -ne $cmd
}

# Try to use Azure CLI first (most reliable)
if ($UseCommand -eq 'auto' -or $UseCommand -eq 'cli') {
    if (Get-AvailableCommand 'az') {
        Write-Verbose "Using Azure CLI to fetch role definitions"
        
        $roles = @{}
        foreach ($roleName in $RoleNames) {
            try {
                $query = "[?roleName=='$roleName'].{id:id, name:roleName, description:description} | [0]"
                $result = az role definition list --query $query 2>$null | ConvertFrom-Json
                
                if ($result.id) {
                    $roles[$roleName] = $result
                    Write-Verbose "Found role: $roleName = $($result.id)"
                } else {
                    Write-Warning "Role '$roleName' not found in Azure RBAC"
                }
            }
            catch {
                Write-Error "Failed to fetch role definition for '$roleName': $_"
            }
        }
        
        if ($roles.Count -gt 0) {
            # Output as JSON for Bicep parameter file
            $output = @{}
            foreach ($roleName in $RoleNames) {
                if ($roles.ContainsKey($roleName)) {
                    # Convert role name to parameter-friendly format
                    $paramName = $roleName -replace '\s+', '' -replace '-', ''
                    $output[$paramName] = $roles[$roleName].id
                }
            }
            return $output | ConvertTo-Json
        }
    }
}

# Fallback to Azure PowerShell
if ($UseCommand -eq 'auto' -or $UseCommand -eq 'powershell') {
    if (Get-AvailableCommand 'Get-AzRoleDefinition') {
        Write-Verbose "Using Azure PowerShell to fetch role definitions"
        
        if ($SubscriptionId) {
            Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
        }
        
        $roles = @{}
        foreach ($roleName in $RoleNames) {
            try {
                $result = Get-AzRoleDefinition -Name $roleName -ErrorAction Stop
                $roles[$roleName] = @{
                    id          = $result.Id
                    name        = $result.Name
                    description = $result.Description
                }
                Write-Verbose "Found role: $roleName = $($result.Id)"
            }
            catch {
                Write-Warning "Role '$roleName' not found in Azure RBAC: $_"
            }
        }
        
        if ($roles.Count -gt 0) {
            # Output as JSON for Bicep parameter file
            $output = @{}
            foreach ($roleName in $RoleNames) {
                if ($roles.ContainsKey($roleName)) {
                    # Convert role name to parameter-friendly format
                    $paramName = $roleName -replace '\s+', '' -replace '-', ''
                    $output[$paramName] = $roles[$roleName].id
                }
            }
            return $output | ConvertTo-Json
        }
    }
}

# If we get here, no tool was available
Write-Error "Neither Azure CLI nor Azure PowerShell is available. Please install one to proceed."
exit 1

// ============================================================================
// NOTE: This module is a placeholder for documentation
// ============================================================================
// In practice, images should be built using:
// 1. GitHub Actions (recommended for CI/CD)
// 2. Azure Pipelines
// 3. Manual `az acr build` commands before running `azd up`
// 4. The build-push-images.ps1 script in ./scripts/
//
// To use this in production:
// Option A: Run manually before azd up
//   ./infra/scripts/build-push-images.ps1
//
// Option B: Use GitHub Actions (see .github/workflows/)
//
// Option C: Use Azure Pipelines with this Bicep deployment
// ============================================================================

@description('Container Registry name')
param containerRegistryName string = ''

@description('Image tag')
param imageTag string = 'latest'

// This module serves as documentation
// The actual image building happens via external CI/CD or pre-deployment scripts
// We intentionally keep this simple to avoid deployment script complexities

output containerRegistryName string = containerRegistryName
output imageTag string = imageTag
output message string = 'Images should be built and pushed to ACR before Container Apps deployment'

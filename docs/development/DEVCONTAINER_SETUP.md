# DevContainer Setup Guide

This guide explains how to set up and use DevContainers for local development and GitHub Codespaces with .NET Aspire and Dapr.

## Overview

The project includes **two DevContainer configurations** to support different environments:

- **Local Development**: Uses host Docker socket for optimal performance
- **GitHub Codespaces**: Uses Docker-in-Docker for cloud environments

## File Structure

```
.devcontainer/
├── devcontainer.json              # Base configuration (shared)
├── devcontainer.local.json        # Local development variant
└── devcontainer.codespaces.json  # Codespaces variant
```

## Base Configuration (`devcontainer.json`)

Contains common settings shared by both variants:

- **Image**: `mcr.microsoft.com/devcontainers/dotnet:10.0-bookworm`
- **Features**: Git support
- **Commands**:
  - `onCreateCommand`: Installs Aspire project templates
  - `postStartCommand`: Trusts HTTPS development certificates
- **VS Code Extensions**:
  - `ms-dotnettools.csdevkit` - Aspire SDK
  - `ms-dotnettools.csharp` - C# support
  - `ms-azuretools.vscode-docker` - Docker support
  - `ms-azuretools.vscode-dapr` - Dapr support

## Local Development (`devcontainer.local.json`)

**Extends**: `devcontainer.json`

**Additional Configuration**:
- Mounts host Docker socket: `/var/run/docker.sock`
- Uses host network: `--network=host`

**Why**: Aspire needs direct access to the host Docker daemon to create containers (Redis, SQL Server, Dapr sidecars, etc.). This configuration provides optimal performance and networking.

**Usage**:
1. Rename `devcontainer.local.json` to `devcontainer.json` temporarily, or
2. Configure VS Code to use the local variant

## GitHub Codespaces (`devcontainer.codespaces.json`)

**Extends**: `devcontainer.json`

**Additional Configuration**:
- Docker-in-Docker feature enabled
- Non-root Docker support
- Resource requirements:
  - 4 CPUs
  - 16GB RAM
  - 32GB storage

**Why**: Codespaces cannot mount the host Docker socket. Docker-in-Docker provides container isolation within the cloud environment.

**Usage**: Codespaces automatically detects and uses this configuration when opening the repository.

## Important Notes

### Dapr Version

**Aspire uses its own integrated Dapr runtime version** (currently 1.15.x). Installing Dapr CLI in the DevContainer does **not** affect the version Aspire uses for sidecars.

- Aspire always starts Dapr sidecars using its integrated runtime
- The Dapr CLI installation in DevContainer is only for development/testing purposes
- Scheduler and Placement connection errors are harmless warnings (these services are not used)

### Docker Configuration

**Local Development**:
- ✅ Uses host Docker socket
- ✅ Optimal performance
- ✅ Direct container access
- ✅ Works with Aspire's container orchestration

**Codespaces**:
- ✅ Uses Docker-in-Docker
- ✅ Required for cloud environments
- ✅ Isolated container environment
- ✅ Works with Aspire's container orchestration

## Quick Start

### Local Development

1. Open the project in VS Code
2. When prompted, select "Reopen in Container"
3. VS Code will build and start the DevContainer
4. Run `dotnet run` in the `src/AppHost` directory

### GitHub Codespaces

1. Open the repository in GitHub Codespaces
2. Codespaces automatically uses `devcontainer.codespaces.json`
3. Wait for the container to build
4. Run `dotnet run` in the `src/AppHost` directory

## Troubleshooting

### Port Forwarding

Aspire automatically configures port forwarding in DevContainers (Aspire 9.1+). If ports don't forward correctly:

1. Check VS Code port forwarding settings
2. Verify the Aspire dashboard URL
3. Ensure firewall rules allow port access

### Docker Access Issues

**Local Development**:
- Ensure Docker Desktop is running
- Verify Docker socket permissions
- Check `docker ps` works from terminal

**Codespaces**:
- Verify Docker-in-Docker feature is enabled
- Check resource allocation meets requirements
- Review Codespaces logs for errors

### Certificate Issues

If HTTPS certificates fail:

1. Run `dotnet dev-certs https --trust` manually
2. Restart the DevContainer
3. Clear browser certificate cache

## Resources

- [Aspire DevContainer Documentation](https://aspire.dev/get-started/dev-containers/)
- [VS Code DevContainers](https://code.visualstudio.com/docs/devcontainers/containers)
- [GitHub Codespaces](https://docs.github.com/en/codespaces)

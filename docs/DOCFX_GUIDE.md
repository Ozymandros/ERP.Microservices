# DocFX Documentation Guide

This project uses [DocFX](https://dotnet.github.io/docfx/) to automatically generate technical documentation from source code and markdown files.

## 📚 Documentation Site

The documentation is automatically published to GitHub Pages at:
**https://ozymandros.github.io/ERP.Microservices/**

## 🔨 Building Documentation Locally

### Prerequisites

- .NET 10 SDK
- DocFX (installed via `dotnet tool`)

### Install DocFX

```bash
dotnet tool update -g docfx
```

### Build Documentation

```bash
# Navigate to docs directory
cd docs

# Build the documentation
docfx docfx.json

# Serve the documentation locally (optional)
docfx serve _site
```

The documentation will be available at `http://localhost:8080`

## 📝 Adding Documentation

### API Documentation

API documentation is automatically generated from XML comments in C# code:

```csharp
/// <summary>
/// Gets a user by ID
/// </summary>
/// <param name="id">The user ID</param>
/// <returns>The user details</returns>
public async Task<UserDto> GetUserByIdAsync(string id)
{
    // Implementation
}
```

All projects have XML documentation enabled via `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in their `.csproj` files.

### Markdown Documentation

Add or update markdown files in the `/docs` directory:

- **Guides**: `/docs/guides/`
- **Architecture**: `/docs/architecture/`
- **Deployment**: `/docs/deployment/`
- **Infrastructure**: `/docs/infrastructure/`
- **Security**: `/docs/security/`

Update `/docs/toc.yml` to add new files to the navigation.

## 🚀 Deployment

Documentation is automatically built and deployed to GitHub Pages when:

- Changes are pushed to the `main` or `master` branch
- The GitHub Actions workflow completes successfully

See `.github/workflows/publish-docs.yml` for the deployment workflow.

## 📋 Configuration

The DocFX configuration is in `docs/docfx.json`:

- **metadata**: Defines which C# projects to extract API documentation from
- **build**: Defines which content to include (markdown files, API references)
- **globalMetadata**: Site-wide settings (title, footer, search, etc.)

## 🔍 Troubleshooting

### Missing API Documentation

If API documentation is not showing:

1. Ensure the project has `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in its `.csproj`
2. Add XML comments to public classes and methods
3. Rebuild the project: `dotnet build`
4. Rebuild documentation: `docfx docs/docfx.json`

### Build Warnings

DocFX may show warnings for:

- Missing files referenced in TOC
- Invalid internal links
- Missing XML comments

These are usually safe to ignore if the build succeeds.

## 📖 Resources

- [DocFX Documentation](https://dotnet.github.io/docfx/)
- [DocFX on GitHub](https://github.com/dotnet/docfx)
- [Markdown Syntax Guide](https://dotnet.github.io/docfx/docs/markdown.html)

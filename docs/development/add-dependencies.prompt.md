For each ERP module in the solution (except Sales), update all `.csproj` files to include the correct **project references** according to the standard layered architecture.

Apply the following reference rules **strictly**:

1. **Domain**  
   - No project references.

2. **Application.Contracts**  
   - Reference `Domain`.

3. **Application**  
   - Reference `Domain`.  
   - Reference `Application.Contracts`.  
   - Reference `Shared.Domain`.  
   - Reference `Shared.CQRS`.  

4. **Infrastructure** (e.g., `Module.Infrastructure.EntityFrameworkCore`)  
   - Reference `Domain`.  
   - Reference `Shared.Domain`.  
   - Reference `Microsoft.EntityFrameworkCore`.

5. **HttpApi**  or **Api**
   - Reference `Domain`.  
   - Reference `Application`.  
   - Reference `Application.Contracts`.  
   - Reference `Shared.Domain`.  
   - Reference `AutoMapper`.
   - Reference `Infrastructure`.

All references must be added using `<ProjectReference Include="..." />` inside a single `<ItemGroup>` in each `.csproj` file.  
Do **not** add any NuGet package references (`<PackageReference>`) unless explicitly listed in the rules above (none are listed—only project references apply).  
Do **not** modify any other part of the `.csproj` files (e.g., properties, SDK, target frameworks, or existing item groups).
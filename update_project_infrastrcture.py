import sys
import xml.etree.ElementTree as ET
from pathlib import Path
import shutil
import glob

def copy_dbcontext_file(csproj_path):
    """
    Looks for XXXDbContext.cs file in the script's directory and copies it
    to the csproj directory with renamed file based on csproj name.
    
    Args:
        csproj_path: Path to the .csproj file
    """
    try:
        # Get the script's directory (where the .py file is running from)
        script_dir = Path(__file__).parent
        
        # Find XXXDbContext.cs file in script directory
        dbcontext_files = list(script_dir.glob('*DbContext.cs'))
        
        if not dbcontext_files:
            print("No XXXDbContext.cs file found in script directory, skipping...")
            return
        
        if len(dbcontext_files) > 1:
            print(f"Warning: Multiple DbContext files found, using the first one: {dbcontext_files[0].name}")
        
        source_file = dbcontext_files[0]
        
        # Get csproj name and process it
        csproj_path_obj = Path(csproj_path)
        csproj_name = csproj_path_obj.stem  # Gets filename without extension
        
        # Split by "." and get the last segment
        name_segments = csproj_name.split('.')
        last_segment = name_segments[-2]
        
        # Remove final "s" if it exists
        if last_segment.endswith('s'):
            last_segment = last_segment[:-1]
        
        # Create new filename
        new_filename = f"{last_segment}DbContext.cs"
        
        # Create "Data" subfolder in csproj directory if it doesn't exist
        data_folder = csproj_path_obj.parent / "Data"
        data_folder.mkdir(exist_ok=True)
        
        destination = data_folder / new_filename
        
        # Read the source file content
        with open(source_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Get the original class name from source file (without DbContext)
        source_name = source_file.stem.replace('DbContext', '')
        
        # Replace XXX with the new name in the content
        content = content.replace(source_name, last_segment)
        
        # Write to destination with modified content
        with open(destination, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"Copied and renamed {source_file.name} to {destination}")
        print(f"Replaced '{source_name}' with '{last_segment}' in file contents")
        
    except Exception as e:
        print(f"Warning: Failed to copy DbContext file. {e}")


def add_ef_core_references(csproj_path):
    """
    Adds Entity Framework Core package references to a .csproj file.
    
    Args:
        csproj_path: Path to the .csproj file
    """
    try:
        # Parse the csproj file
        tree = ET.parse(csproj_path)
        root = tree.getroot()
        
        # Define the packages to add
        packages = [
            {"Include": "Microsoft.EntityFrameworkCore", "Version": "8.0.0"},
            {
                "Include": "Microsoft.EntityFrameworkCore.Design",
                "Version": "8.0.0",
                "IncludeAssets": "runtime; build; native; contentfiles; analyzers; buildtransitive",
                "PrivateAssets": "all"
            },
            {"Include": "Microsoft.EntityFrameworkCore.Relational", "Version": "8.0.0"},
            {"Include": "Microsoft.EntityFrameworkCore.SqlServer", "Version": "8.0.0"},
            {
                "Include": "Microsoft.EntityFrameworkCore.Tools",
                "Version": "8.0.0",
                "PrivateAssets": "all",
                "IncludeAssets": "runtime; build; native; contentfiles; analyzers; buildtransitive"
            },
            {"Include": "Microsoft.Extensions.Configuration.Json", "Version": "8.0.0"}
        ]
        
        # Create a new ItemGroup element
        item_group = ET.SubElement(root, 'ItemGroup')
        
        # Get existing package references to avoid duplicates
        existing_packages = set()
        for pkg_ref in root.findall('.//PackageReference'):
            include = pkg_ref.get('Include')
            if include:
                existing_packages.add(include)
        
        # Add new package references
        added_count = 0
        for package in packages:
            package_name = package["Include"]
            
            # Skip if package already exists
            if package_name in existing_packages:
                print(f"Package '{package_name}' already exists, skipping...")
                continue
            
            # Create PackageReference element
            pkg_ref = ET.SubElement(item_group, 'PackageReference')
            pkg_ref.set('Include', package["Include"])
            pkg_ref.set('Version', package["Version"])
            
            # Add optional child elements
            if "IncludeAssets" in package:
                include_assets = ET.SubElement(pkg_ref, 'IncludeAssets')
                include_assets.text = package["IncludeAssets"]
            
            if "PrivateAssets" in package:
                private_assets = ET.SubElement(pkg_ref, 'PrivateAssets')
                private_assets.text = package["PrivateAssets"]
            
            added_count += 1
            print(f"Added package: {package_name}")
        
        # Write back to file with proper formatting
        ET.indent(tree, space='  ')
        tree.write(csproj_path, encoding='utf-8', xml_declaration=True)
        
        print(f"\nSuccessfully added {added_count} package reference(s) to {csproj_path}")
        
    except FileNotFoundError:
        print(f"Error: File '{csproj_path}' not found.")
        sys.exit(1)
    except ET.ParseError as e:
        print(f"Error: Failed to parse XML file. {e}")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

def main():
    if len(sys.argv) != 2:
        print("Usage: python script.py <path_to_csproj_file>")
        sys.exit(1)
    
    csproj_path = sys.argv[1]
    
    # Validate file exists and has .csproj extension
    if not csproj_path.endswith('.csproj'):
        print("Error: File must have .csproj extension")
        sys.exit(1)
    
    if not Path(csproj_path).exists():
        print(f"Error: File '{csproj_path}' does not exist")
        sys.exit(1)
    
    # Copy DbContext file if it exists
    copy_dbcontext_file(csproj_path)
    
    # Add EF Core references
    add_ef_core_references(csproj_path)

if __name__ == "__main__":
    main()
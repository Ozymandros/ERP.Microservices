import os
import sys

print("Directori de treball actual:", os.getcwd())

# Obté el nom del fitxer de llista des dels arguments de la línia d'ordres
filename = sys.argv[1] if len(sys.argv) > 1 else 'list.txt'
print("Fitxer de llista:", filename)

# Obté el nom de l'aplicació des dels arguments de la línia d'ordres
appName = sys.argv[2] if len(sys.argv) > 2 else 'MyApp'
print("Nom de l'aplicació:", appName)

# Obté el directori on es troba el script actual
script_dir = os.path.dirname(os.path.abspath(__file__))
print("Directori de treball real:", script_dir)

# Ruta completa al fitxer list.txt dins del mateix directori
filename = os.path.join(script_dir, filename)
print("Ruta completa al fitxer list.txt:", filename)

# Llegeix el fitxer
with open(filename, 'r') as f:
    lines = f.readlines()


current_folder = None

for line in lines:
    line = line.strip()
    
    # Skip empty lines
    if not line:
        continue
    
    # Remove leading asterisk and spaces
    if line.startswith('-') or line[0].isnumeric():
        line = line[1:].strip()
    else:
        line = line[0:].strip()
    
    # Check if it's a first level (ends with :)
    if line.endswith(':'):
        # Remove the colon and create folder
        current_folder = f"{appName}.{line[:-1]}"

        # Obté el directori actual
        current_folder = os.path.join(script_dir, current_folder)
        print("Directori actual:", current_folder)

        if os.path.exists(current_folder):
            print(f"Folder {current_folder} already exists")
            continue
        os.makedirs(f"{current_folder}", exist_ok=True)
        print(f"Created folder: {current_folder}")
    
    # Second level item
    elif current_folder:
        if not line.startswith(os.path.basename(current_folder) + '.'):
            line = os.path.basename(current_folder) + '.' + line

        print("line:", line)
        
        # Obté el directori actual
        os.chdir(current_folder)
        print("Current directory after chdir:", os.getcwd())
        
        # Concatenate folder path with second level name
        full_path = os.path.join(current_folder, line)
        
        # Crea el projecte .NET
        if line.endswith(".API"):
            os.system(f"dotnet new webapi -n {line} -f net8.0 --no-restore")
        else:
            os.system(f"dotnet new classlib -n {line} -f net8.0")
        
        class_path = os.path.join(full_path, "Class1.cs")
        # delete file if it exists
        if os.path.exists(class_path):
            os.remove(class_path)
        print(f"Full path for remove: {class_path}")

        print("Current directory after chdir:", os.getcwd())
        #print(f"dotnet new classlib -n {full_path} -f net8.0")
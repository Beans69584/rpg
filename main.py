import os
import subprocess


# Define the file extensions to consider and directories to ignore
extensions = [".cs", ".razor", ".cshtml", ".cshtml.cs", ".scss", ".csproj", ".js", ".lua"]
ignored_dirs = ["node_modules", "build", "dist", "coverage", "public", ".next", ".nuxt", ".idea", ".vscode", ".git", ".vercel", "migrations", ".tw-patch", "docs", "content", ".storybook", ".contentlayer", "output", "types", "utils", "server", "contexts", "middleware", "locales", "libs", "api", "obj", "bin", "Migrations"]
ignored_files = ["next-env.d.ts", "i18n.ts", "Toast.tsx", "withAuth.tsx", "JapanMap.tsx", "Select.tsx", "Spinner.tsx", "prisma.ts", "verify.tsx", "japan-overview.tsx"]

def generate_unique_filename(base_path, filename):
    name, ext = os.path.splitext(filename)
    counter = 1
    new_filename = filename
    while os.path.exists(os.path.join(base_path, new_filename)):
        new_filename = f"{name}_{counter}{ext}"
        counter += 1
    return new_filename

# Initialize the text variable
text = ""

def walk_dir(dir_path, output_folder):
    global text
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)
    for root, dirs, files in os.walk(dir_path):
        dirs[:] = [d for d in dirs if d not in ignored_dirs]
        for file in files:
            if any(file.endswith(ext) for ext in extensions) and file not in ignored_files:
                file_path = os.path.join(root, file)
                with open(file_path, "r", encoding="utf-8") as f:
                    content = f.read()
                    # Append the content to the text variable
                    # but first append a relative path to the file
                    text += f"File: {file_path}\n"
                    text += content
                    text += "\n\n"
                
                # Create a new file with the same name in the output folder, put it in a mirrored directory structure
                # relative_path = os.path.relpath(root, dir_path)
                # output_path = os.path.join(output_folder, relative_path)
                # if not os.path.exists(output_path):
                #     os.makedirs(output_path)
                # new_filename = generate_unique_filename(output_path, file)
                # new_file_path = os.path.join(output_path, new_filename)
                # with open(new_file_path, "w", encoding="utf-8") as f:
                #     f.write(content)
                # print(f"Processed {file_path} -> {new_file_path}")

                # Create a new file with the same name in the output folder, put it in a flat structure
                new_filename = generate_unique_filename(output_folder, file)
                new_file_path = os.path.join(output_folder, new_filename)
                with open(new_file_path, "w", encoding="utf-8") as f:
                    f.write(content)
                print(f"Processed {file_path} -> {new_file_path}")



def empty_dir(dir_path):
    # delete all subdirectories and files in the specified directory
    for root, dirs, files in os.walk(dir_path, topdown=False):
        for file in files:
            os.remove(os.path.join(root, file))
        for dir in dirs:
            os.rmdir(os.path.join(root, dir))
    print(f"Emptied {dir_path}")

# Empty output folder
empty_dir("output")

# Output in X:\output
walk_dir(".", "output")

def generate_tree(dir_path, prefix=""):
    """
    Generate a tree-like structure of the directory contents.
    """
    contents = []
    files = []
    dirs = []

    # Sort files and directories separately and in alphabetical order
    for item in sorted(os.listdir(dir_path)):
        if os.path.isdir(os.path.join(dir_path, item)) and item not in ignored_dirs:
            dirs.append(item)
        elif os.path.isfile(os.path.join(dir_path, item)):
            files.append(item)
    
    # Process directories
    for i, directory in enumerate(dirs):
        connector = "├── " if i < len(dirs) - 1 or files else "└── "
        contents.append(f"{prefix}{connector}{directory}")
        contents.extend(generate_tree(os.path.join(dir_path, directory), prefix + ("│   " if i < len(dirs) - 1 else "    ")))

    # Process files
    for i, file in enumerate(files):
        connector = "├── " if i < len(files) - 1 else "└── "
        contents.append(f"{prefix}{connector}{file}")
    
    return contents


# Tree the src, prisma, and public directories, then output all to a combined file
with open("combined.tree.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(generate_tree(".")))
    f.write("\n")

# write text to a file
with open("combined.txt", "w", encoding="utf-8") as f:
    f.write(text)
import os

def render_puml_to_png_local(puml_file_path, output_path=None):
    try:
        if output_path is None:
            output_path = os.path.splitext(puml_file_path)[0] + '.png'
        
        # Read and modify the PUML content
        with open(puml_file_path, 'r') as f:
            content = f.read()
        
        # Add extreme scaling parameters
        modified_content = """
@startuml
scale max 32768x32768
page 1000x1000
skinparam dpi 300
skinparam maxMessageSize 1000
skinparam maxImageSize 32768
skinparam responseMessageBelowArrow true
skinparam svgDimensionStyle false
skinparam padding 2
skinparam nodesep 70
skinparam ranksep 70
""" + content

        if not content.startswith('@startuml'):
            modified_content = modified_content + "\n@enduml"
        
        # Write modified content to temporary file
        temp_file = puml_file_path + '.temp'
        with open(temp_file, 'w') as f:
            f.write(modified_content)

        # Command with extreme memory and size limits
        cmd = (
            'java '
            '-Xmx8192m '  # Increase heap size to 8GB
            '-XX:MaxHeapSize=8192m '
            '-DPLANTUML_LIMIT_SIZE=32768 '  # Increase size limit to maximum
            '-Djava.awt.headless=true '
            '-jar plantuml.jar '
            '-charset UTF-8 '
            '-tpng '
            f'-progress '
            f'"{temp_file}"'
        )
        
        # Execute the command
        result = os.system(cmd)
        
        # Clean up temporary file
        os.remove(temp_file)
        
        if result == 0:
            print(f"Successfully generated PNG: {output_path}")
        else:
            print(f"Error: PlantUML returned code {result}")
        
    except Exception as e:
        print(f"Error generating PNG: {str(e)}")

# Alternative approach: Split the diagram
def split_diagram(puml_file_path):
    """
    Splits a large PUML file into multiple smaller files
    """
    with open(puml_file_path, 'r') as f:
        content = f.read()
    
    # Split content based on packages or logical groupings
    # This is a simple example - you might need to modify based on your diagram structure
    parts = content.split('\npackage ')
    
    base_name = os.path.splitext(puml_file_path)[0]
    
    for i, part in enumerate(parts):
        if i == 0 and not part.strip().startswith('@startuml'):
            continue
            
        part_content = f"""@startuml
scale max 32768x32768
skinparam dpi 300
skinparam maxMessageSize 1000
skinparam maxImageSize 32768

{'package ' if i > 0 else ''}{part}
@enduml"""
        
        part_file = f"{base_name}_part{i}.puml"
        with open(part_file, 'w') as f:
            f.write(part_content)
        
        render_puml_to_png_local(part_file)

if __name__ == "__main__":
    puml_file = "puml_export/include.puml"
    
    print("Attempting to render full diagram...")
    render_puml_to_png_local(puml_file)
    
import os
import logging
import shutil
from typing import List, Set
from dataclasses import dataclass
from pathlib import Path
import yaml

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('file_processor.log'),
        logging.StreamHandler()
    ]
)


@dataclass
class Config:
    """Configuration class for file processing settings."""
    extensions: Set[str]
    ignored_dirs: Set[str]
    ignored_files: Set[str]
    output_dir: Path
    mirror_structure: bool

    @classmethod
    def from_yaml(cls, path: str = 'config.yaml') -> 'Config':
        """Load configuration from YAML file."""
        try:
            with open(path, 'r') as f:
                config = yaml.safe_load(f)
                return cls(
                    extensions=set(config.get('extensions', [])),
                    ignored_dirs=set(config.get('ignored_dirs', [])),
                    ignored_files=set(config.get('ignored_files', [])),
                    output_dir=Path(config.get('output_dir', 'output')),
                    mirror_structure=config.get(
                        'mirror_structure', False)  # Add this line
                )
        except FileNotFoundError:
            logging.warning(f"Config file {path} not found, using defaults")
            return cls.get_defaults()

    @classmethod
    def get_defaults(cls) -> 'Config':
        """Provide default configuration."""
        return cls(
            extensions={".cs", ".razor", ".cshtml", ".cshtml.cs", ".scss",
                        ".csproj", ".js", ".lua"},
            ignored_dirs={"node_modules", "build", "dist", "coverage",
                          "public", ".next", ".git", "bin", "obj"},
            ignored_files=set(),
            output_dir=Path("output"),
            mirror_structure=False  # Add this line
        )


class FileProcessor:
    def __init__(self, config: Config):
        self.config = config
        self.collected_text = []

    def generate_unique_filename(self, base_path: Path, filename: str) -> str:
        """Generate a unique filename in the given directory."""
        name, ext = os.path.splitext(filename)
        counter = 1
        new_filename = filename
        while (base_path / new_filename).exists():
            new_filename = f"{name}_{counter}{ext}"
            counter += 1
        return new_filename

    def process_directory(self, source_dir: Path) -> None:
        """Process directory and copy files to output location."""
        try:
            self.config.output_dir.mkdir(parents=True, exist_ok=True)

            for root, dirs, files in os.walk(source_dir):
                # Filter out ignored directories
                dirs[:] = [d for d in dirs if d not in self.config.ignored_dirs]

                for file in files:
                    if (any(file.endswith(ext) for ext in self.config.extensions)
                            and file not in self.config.ignored_files):
                        self._process_file(Path(root) / file)

        except Exception as e:
            logging.error(f"Error processing directory: {e}")
            raise

    def _process_file(self, file_path: Path) -> None:
        """Process individual file."""
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()
                self.collected_text.append(f"File: {file_path}\n{content}\n\n")

            if self.config.mirror_structure:
                rel_path = file_path.relative_to(Path("."))
                new_file_path = self.config.output_dir / rel_path
                new_file_path.parent.mkdir(parents=True, exist_ok=True)
            else:
                new_filename = self.generate_unique_filename(
                    self.config.output_dir,
                    file_path.name
                )
                new_file_path = self.config.output_dir / new_filename

            with open(new_file_path, "w", encoding="utf-8") as f:
                f.write(content)

            logging.info(f"Processed {file_path} -> {new_file_path}")

        except Exception as e:
            logging.error(f"Error processing file {file_path}: {e}")

    def generate_tree(self, dir_path: Path, prefix: str = "") -> List[str]:
        """Generate a tree-like structure of the directory contents."""
        try:
            contents = []
            items = sorted(os.listdir(dir_path))

            dirs = [item for item in items
                    if (dir_path / item).is_dir()
                    and item not in self.config.ignored_dirs]

            files = [item for item in items
                     if (dir_path / item).is_file()]

            for i, directory in enumerate(dirs):
                is_last = (i == len(dirs) - 1) and not files
                connector = "└── " if is_last else "├── "
                contents.append(f"{prefix}{connector}{directory}")
                new_prefix = prefix + ("    " if is_last else "│   ")
                contents.extend(self.generate_tree(
                    dir_path / directory, new_prefix))

            for i, file in enumerate(files):
                connector = "└── " if i == len(files) - 1 else "├── "
                contents.append(f"{prefix}{connector}{file}")

            return contents

        except Exception as e:
            logging.error(f"Error generating tree for {dir_path}: {e}")
            return []

    def save_outputs(self, source_dir: Path) -> None:
        """Save the collected text and tree structure to files."""
        try:
            # Save combined text
            with open("combined.txt", "w", encoding="utf-8") as f:
                f.write("".join(self.collected_text))

            # Save tree structure
            with open("combined.tree.txt", "w", encoding="utf-8") as f:
                tree_content = "\n".join(self.generate_tree(source_dir))
                f.write(f"{tree_content}\n")

        except Exception as e:
            logging.error(f"Error saving outputs: {e}")


def main():
    try:
        config = Config.from_yaml()

        # Clean output directory
        if config.output_dir.exists():
            shutil.rmtree(config.output_dir)

        processor = FileProcessor(config)
        processor.process_directory(Path("."))
        processor.save_outputs(Path("."))

        logging.info("Processing completed successfully")

    except Exception as e:
        logging.error(f"Fatal error: {e}")
        raise


if __name__ == "__main__":
    main()

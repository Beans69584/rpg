
import os
import shutil
import subprocess
import sys
from typing import List, Dict, Tuple
from rich.console import Console
from rich.panel import Panel
from rich.progress import Progress, SpinnerColumn, TextColumn, BarColumn, TimeElapsedColumn
from rich.table import Table
from rich.live import Live
from rich.layout import Layout
from rich import box
import questionary
from questionary import Choice
from datetime import datetime
import platform
import threading
import queue

console = Console()


class BuildSystem:
    def __init__(self):
        self.platforms = {
            'windows': ['x64', 'x86', 'arm64'],
            'linux': ['x64', 'arm64'],
            'osx': ['x64', 'arm64']
        }

        self.runtimes = {
            'windows': 'win',
            'linux': 'linux',
            'osx': 'osx'
        }

        self.file_extensions = {
            'windows': '.exe',
            'linux': '',
            'osx': ''
        }

        self.project_path = os.path.dirname(os.path.abspath(__file__))
        self.publish_path = os.path.join(self.project_path, 'publish')
        self.build_logs_path = os.path.join(self.project_path, 'build_logs')

        os.makedirs(self.publish_path, exist_ok=True)
        os.makedirs(self.build_logs_path, exist_ok=True)

        self.progress_markers = [
            (r"Determining projects to restore", 2),
            (r"Restored\s+[\w\s/]+?packages", 5),
            (r"Restored\s+[\w\s/]+?\.csproj", 8),
            (r"Build started", 10),
            (r"Compiling\s+[\w\s/]+?\.cs", 12),
            (r"CoreGenerateAssemblyInfo", 14),
            (r"GenerateTargetFrameworkMonikerAttribute", 15),
            (r"CoreCompile target", 18),
            (r"Csc target", 20),
            (r"_InitializeSourceControlInformation", 22),
            (r"GetCopyToOutputDirectoryItems", 25),
            (r"_CopySourceItemsToOutputDirectory", 28),
            (r"CopyFilesToOutputDirectory", 30),
            (r"Build succeeded", 35),
            (r"_InitializeIlcParameters", 38),
            (r"_WriteIlcRspFile", 40),
            (r"IlcCompile target", 42),
            (r"_LinkNative target", 45),
            (r"_CreateIlcDirectory", 48),
            (r"ComputeResolvedFilesToPublishList", 50),
            (r"CopyFilesToPublishDirectory", 52),
            (r"_CopyResolvedFilesToPublishLocal", 55),
            (r"_CopyResolvedFilesToPublishPreserveNewest", 58),
            (r"_DeploymentUnpublishable", 60),
            (r"GenerateNativeImages", 65),
            (r"Optimizing assemblies", 68),
            (r"RunNgeni target", 70),
            (r"ComputeIlToNativePaths", 72),
            (r"_StartupTracker", 75),
            (r"_ResolveCompileToolPaths", 78),
            (r"_ComputeIncrementalInputs", 80),
            (r"_GenerateCrossgenProfilingSymbols", 82),
            (r"_PublishBuildAlternative", 85),
            (r"_PublishNativeImages", 88),
            (r"_GenerateBundle", 90),
            (r"_CreateAppHost", 92),
            (r"Published\s+[\w\s/]+?\.csproj", 95),
            (r"Generating native code", 97),
            (r"Linking native binary", 98),
        ]

        self.error_markers = [
            r"Build FAILED",
            r"Error\s+[A-Z]+\d+",
            r"Could not find a part of the path",
            r"The system cannot find the path specified",
            r"EXEC : error",
            r"ILCompiler error",
            r"Native linking error",
            r"AOT Compilation failed",
            r"Compilation failed for",
            r"MSB\d+",
            r"NETSDK\d+",
            r"Exception during compilation:"
        ]

    def get_runtime_id(self, os_name: str, arch: str) -> str:
        return f"{self.runtimes[os_name]}-{arch}"

    def create_build_log(self, os_name: str, arch: str, success: bool, output: str):
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        log_file = os.path.join(
            self.build_logs_path,
            f"build_{os_name}_{arch}_{timestamp}.log"
        )

        with open(log_file, 'w', encoding='utf-8') as f:
            f.write(f"Build for {os_name}-{arch}\n")
            f.write(f"Status: {'Success' if success else 'Failed'}\n")
            f.write("=== Build Output ===\n")
            f.write(output)

    def parse_build_progress(self, line: str) -> int:
        """Parse a line of build output and return the estimated progress percentage"""
        import re

        for error in self.error_markers:
            if re.search(error, line, re.IGNORECASE):
                return -1

        highest_progress = None
        for pattern, percentage in self.progress_markers:
            if re.search(pattern, line, re.IGNORECASE):
                if highest_progress is None or percentage > highest_progress:
                    highest_progress = percentage

        return highest_progress

    def build_project(self, os_name: str, arch: str, config: str,
                      standalone: bool, progress_callback=None) -> Tuple[bool, str]:
        runtime = self.get_runtime_id(os_name, arch)
        output_path = os.path.join(self.publish_path, f"{os_name}-{arch}")

        try:
            if os.path.exists(output_path):
                shutil.rmtree(output_path)

            cmd = [
                'dotnet', 'publish',
                os.path.join(self.project_path, 'RPG.csproj'),
                '-c', config,
                '-r', runtime,
                '-o', output_path,
                '--self-contained' if standalone else '--no-self-contained',
                '/p:PublishSingleFile=true',
                '/p:EnableCompressionInSingleFile=true',
                '/p:DebugType=None',
                '/p:DebugSymbols=false'
            ]

            process = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                universal_newlines=True
            )

            output = []
            last_progress = 0
            output_queue = queue.Queue()

            def enqueue_output(pipe, queue):
                for line in iter(pipe.readline, ''):
                    queue.put(line)
                pipe.close()

            threads = []
            for pipe in [process.stdout, process.stderr]:
                thread = threading.Thread(
                    target=enqueue_output, args=(pipe, output_queue))
                thread.daemon = True
                thread.start()
                threads.append(thread)

            while True:
                try:
                    line = output_queue.get(timeout=0.1)
                except queue.Empty:
                    if process.poll() is not None:
                        break
                    continue
                line = line.strip()
                output.append(line)
                if progress_callback:
                    progress = self.parse_build_progress(line)
                    if progress == -1:
                        progress_callback(f"Error: {line}", last_progress)
                    elif progress is not None:
                        last_progress = max(last_progress, progress)
                        progress_callback(line, last_progress)
                    else:
                        progress_callback(line, last_progress)

            for thread in threads:
                thread.join()

            success = process.returncode == 0

            if success:
                if progress_callback:
                    progress_callback("Creating distribution package...", 95)

                archive_name = f"RPG-{os_name}-{arch}-{config.lower()}"
                if os_name == 'windows':
                    self.create_zip(output_path, archive_name)
                else:
                    self.create_tarball(output_path, archive_name)

                if progress_callback:
                    progress_callback("Build complete", 100)

            full_output = '\n'.join(output)
            self.create_build_log(os_name, arch, success, full_output)
            return success, full_output

        except Exception as e:
            error_msg = str(e)
            self.create_build_log(os_name, arch, False, error_msg)
            return False, error_msg

    def create_zip(self, source_path: str, archive_name: str):
        shutil.make_archive(
            os.path.join(self.publish_path, archive_name),
            'zip',
            source_path
        )

    def create_tarball(self, source_path: str, archive_name: str):
        shutil.make_archive(
            os.path.join(self.publish_path, archive_name),
            'gztar',
            source_path
        )


def check_prerequisites() -> List[str]:
    issues = []

    if sys.version_info < (3, 7):
        issues.append("Python 3.7 or higher is required")

    try:
        output = subprocess.check_output(['dotnet', '--version'], text=True)
        if not output.strip():
            issues.append(".NET SDK version could not be determined")
    except (subprocess.CalledProcessError, FileNotFoundError):
        issues.append(".NET SDK is not installed or not in PATH")

    return issues


def get_build_choices() -> Tuple[List[Tuple[str, str]], str, bool]:
    style = questionary.Style([
        ('question', 'fg:cyan bold'),
        ('answer', 'fg:green bold'),
        ('pointer', 'fg:cyan bold'),
        ('highlighted', 'fg:cyan bold'),
        ('selected', 'fg:green bold'),
    ])

    platforms = []
    platform_choices = []

    for os_name, archs in BuildSystem().platforms.items():
        for arch in archs:
            choice = Choice(
                title=f"{os_name}-{arch}",
                value=(os_name, arch)
            )
            platform_choices.append(choice)

    selected_platforms = questionary.checkbox(
        'Select target platforms:',
        choices=platform_choices,
        style=style
    ).ask()

    if not selected_platforms:
        console.print("[red]No platforms selected. Exiting...[/red]")
        sys.exit(1)

    config = questionary.select(
        'Select build configuration:',
        choices=['Debug', 'Release'],
        default='Release',
        style=style
    ).ask()

    standalone = questionary.confirm(
        'Create standalone builds? (includes runtime)',
        default=True,
        style=style
    ).ask()

    return selected_platforms, config, standalone


def main():
    console.print(Panel.fit(
        "[cyan]RPG Build System[/cyan]",
        box=box.DOUBLE,
        padding=(1, 2),
        title="v1.0"
    ))

    issues = check_prerequisites()
    if issues:
        console.print("\n[red]Prerequisites check failed:[/red]")
        for issue in issues:
            console.print(f"[red]✗[/red] {issue}")
        return

    selected_platforms, config, standalone = get_build_choices()

    builder = BuildSystem()

    table = Table(title="Build Summary", box=box.SIMPLE)
    table.add_column("Setting", style="cyan")
    table.add_column("Value", style="green")

    table.add_row(
        "Platforms",
        "\n".join(f"{os_name}-{arch}" for os_name, arch in selected_platforms)
    )
    table.add_row("Configuration", config)
    table.add_row("Standalone", "Yes" if standalone else "No")

    console.print(table)

    if not questionary.confirm(
        'Start build process?',
        default=True,
        style=questionary.Style([('question', 'fg:cyan bold')])
    ).ask():
        return

    with Progress(
        SpinnerColumn(),
        TextColumn("[progress.description]{task.description}"),
        BarColumn(),
        TextColumn("[progress.percentage]{task.percentage:>3.0f}%"),
        TimeElapsedColumn(),
        console=console
    ) as progress:
        overall_task = progress.add_task(
            "[cyan]Overall progress",
            total=len(selected_platforms)
        )

        for os_name, arch in selected_platforms:
            build_task = progress.add_task(
                f"[green]Building {os_name}-{arch}",
                total=100
            )

            def update_progress(line: str, percentage: int = None):

                progress.update(
                    build_task,
                    description=f"[green]{os_name}-{arch}: {line[:40]}..."
                )

                if percentage is not None:

                    increment = percentage - \
                        progress.tasks[build_task].completed
                    if increment > 0:
                        progress.update(build_task, advance=increment)

            success, output = builder.build_project(
                os_name, arch, config, standalone,
                progress_callback=update_progress
            )

            progress.update(build_task, completed=100)
            progress.update(overall_task, advance=1)

            if not success:
                console.print(f"\n[red]Build failed for {
                              os_name}-{arch}[/red]")
                console.print(f"[red]Error output:[/red]\n{output}")

    console.print("\n[cyan]Build Results:[/cyan]")
    results_table = Table(box=box.SIMPLE)
    results_table.add_column("Platform", style="cyan")
    results_table.add_column("Output", style="green")

    for os_name, arch in selected_platforms:
        archive_name = f"RPG-{os_name}-{arch}-{config.lower()}"
        archive_path = os.path.join(
            builder.publish_path,
            f"{archive_name}.{'zip' if os_name == 'windows' else 'tar.gz'}"
        )

        if os.path.exists(archive_path):
            size = os.path.getsize(archive_path) / (1024 * 1024)
            results_table.add_row(
                f"{os_name}-{arch}",
                f"✓ {archive_name} ({size:.1f} MB)"
            )
        else:
            results_table.add_row(
                f"{os_name}-{arch}",
                "✗ Build failed"
            )

    console.print(results_table)

    console.print(f"\nOutput directory: [green]{builder.publish_path}[/green]")
    console.print(f"Build logs: [green]{builder.build_logs_path}[/green]")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        console.print("\n[yellow]Build process cancelled by user[/yellow]")
        sys.exit(1)
    except Exception as e:
        console.print(f"\n[red]An unexpected error occurred:[/red] {str(e)}")
        sys.exit(1)

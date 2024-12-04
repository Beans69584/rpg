using System.Collections.Concurrent;
using System.Text;

namespace RPG
{
    public class Region
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Padding { get; set; } = 1;
        public int ZIndex { get; set; } = 0;
        public string? Name { get; set; }
        public ConsoleColor BorderColor { get; set; } = ConsoleColor.Gray;
        public ConsoleColor TitleColor { get; set; } = ConsoleColor.White;
        public bool IsVisible { get; set; } = true;
        public Action<Region>? RenderContent { get; set; }

        public Rectangle Bounds => new(X, Y, Width, Height);
        public Rectangle ContentBounds => new(
            X + 1,
            Y + (string.IsNullOrEmpty(Name) ? 0 : 1),  // Reduced top padding
            Width - 2,
            Height - (string.IsNullOrEmpty(Name) ? 1 : 2)  // Ensure content stays within borders
        );
    }

    public readonly struct Rectangle
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = Math.Max(width, 0);
            Height = Math.Max(height, 0);
        }

        public bool Intersects(Rectangle other) =>
            X < other.X + other.Width &&
            X + Width > other.X &&
            Y < other.Y + other.Height &&
            Y + Height > other.Y;
    }

    public class ConsoleWindowManager : IDisposable
    {
        private readonly Dictionary<string, Region> regions = new();
        private readonly object renderLock = new();
        private ConsoleBuffer buffer;
        private bool isDisposed;
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly Task renderTask;
        private string currentInputText = "";
        private ConsoleColor currentInputColor = ConsoleColor.White;
        private bool isDirty = true;
        private bool cursorVisible = true;
        private DateTime lastCursorBlink = DateTime.Now;
        private const int CURSOR_BLINK_MS = 530;
        private int lastConsoleWidth;
        private int lastConsoleHeight;
        private DateTime lastResize = DateTime.Now;
        private const int RESIZE_DEBOUNCE_MS = 100;
        private bool isResizing = false;

        // Add ASCII box drawing characters
        private static readonly Dictionary<string, char> AsciiBoxChars = new()
        {
            ["topLeft"] = '+',
            ["topRight"] = '+',
            ["bottomLeft"] = '+',
            ["bottomRight"] = '+',
            ["horizontal"] = '-',
            ["vertical"] = '|'
        };

        // Add Unicode box drawing characters
        private static readonly Dictionary<string, char> UnicodeBoxChars = new()
        {
            ["topLeft"] = '┌',
            ["topRight"] = '┐',
            ["bottomLeft"] = '└',
            ["bottomRight"] = '┘',
            ["horizontal"] = '─',
            ["vertical"] = '│'
        };

        // Add curved box drawing characters
        private static readonly Dictionary<string, char> CurvedBoxChars = new()
        {
            ["topLeft"] = '╭',
            ["topRight"] = '╮',
            ["bottomLeft"] = '╰',
            ["bottomRight"] = '╯',
            ["horizontal"] = '─',
            ["vertical"] = '│'
        };

        private Dictionary<string, char> BoxChars;
        private ConsoleDisplayConfig displayConfig;
        private const int DEFAULT_REFRESH_RATE = 16;

        // Update constructor to handle curved borders
        public ConsoleWindowManager()
        {
            Console.CursorVisible = false;
            lastConsoleWidth = Console.WindowWidth;
            lastConsoleHeight = Console.WindowHeight;
            buffer = new ConsoleBuffer(Console.WindowWidth, Console.WindowHeight);

            displayConfig = GameSettings.Instance.Display;
            BoxChars = displayConfig.UseUnicodeBorders
                ? (displayConfig.UseCurvedBorders ? CurvedBoxChars : UnicodeBoxChars)
                : AsciiBoxChars;

            renderTask = Task.Run(RenderLoop);
        }

        public void AddRegion(string name, Region region)
        {
            lock (renderLock)
            {
                ValidateAndAdjustRegion(region);
                regions[name] = region;
            }
        }

        public void UpdateRegion(string name, Action<Region> updateAction)
        {
            lock (renderLock)
            {
                if (regions.TryGetValue(name, out var region))
                {
                    updateAction(region);
                    ValidateAndAdjustRegion(region);
                }
            }
        }

        private void RenderLoop()
        {
            while (!cancellationSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (isDirty || (displayConfig.EnableCursorBlink &&
                        (DateTime.Now - lastCursorBlink).TotalMilliseconds >= displayConfig.CursorBlinkRateMs))
                    {
                        Render();
                        isDirty = false;
                    }
                    Thread.Sleep(displayConfig.RefreshRateMs);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public bool CheckResize()
        {
            var now = DateTime.Now;
            if (!isResizing && (Console.WindowWidth != lastConsoleWidth || Console.WindowHeight != lastConsoleHeight))
            {
                isResizing = true;
                if ((now - lastResize).TotalMilliseconds > RESIZE_DEBOUNCE_MS)
                {
                    lock (renderLock)
                    {
                        try
                        {
                            lastConsoleWidth = Console.WindowWidth;
                            lastConsoleHeight = Console.WindowHeight;

                            // Create entirely new buffer
                            buffer = new ConsoleBuffer(lastConsoleWidth, lastConsoleHeight);

                            // Clear console completely
                            Console.Clear();

                            lastResize = now;
                            isDirty = true;
                            return true;
                        }
                        finally
                        {
                            isResizing = false;
                        }
                    }
                }
            }
            return false;
        }


        private void ValidateAndAdjustRegion(Region region)
        {
            // Keep region within bounds
            region.X = Math.Min(region.X, buffer.Width - 4);
            region.Y = Math.Min(region.Y, buffer.Height - 3);

            // Maintain minimum size
            region.Width = Math.Max(region.Width, 4);
            region.Height = Math.Max(region.Height, 3);

            if (!string.IsNullOrEmpty(region.Name))
            {
                region.Width = Math.Max(region.Width, region.Name.Length + 4);
            }

            // Ensure region fits within new console bounds
            region.Width = Math.Min(region.Width, buffer.Width - region.X);
            region.Height = Math.Min(region.Height, buffer.Height - region.Y);

            // If region would be completely off screen, move it
            if (region.X >= buffer.Width)
            {
                region.X = Math.Max(0, buffer.Width - region.Width);
            }
            if (region.Y >= buffer.Height)
            {
                region.Y = Math.Max(0, buffer.Height - region.Height);
            }
        }

        private void Render()
        {
            buffer.Clear();

            foreach (var region in regions.Values.Where(r => r.IsVisible).OrderBy(r => r.ZIndex))
            {
                RenderRegion(region);

                // After rendering the region, if it's the input region, render the current input
                if (region.Name == "Input")
                {
                    RenderInput(region);
                }
            }

            buffer.Flush();
        }


        private void RenderRegion(Region region)
        {
            // First clear the entire region (inside the borders)
            for (int y = region.Y + 1; y < region.Y + region.Height - 1; y++)
            {
                for (int x = region.X + 1; x < region.X + region.Width - 1; x++)
                {
                    buffer.SetChar(x, y, ' ', region.BorderColor);
                }
            }

            // Draw border
            DrawBox(region);

            // If it's the input region, render input
            if (region.Name == "Input")
            {
                RenderInput(region);
            }
            // Otherwise render normal content
            else if (region.RenderContent != null)
            {
                var contentBounds = region.ContentBounds;
                region.RenderContent(new Region
                {
                    X = contentBounds.X,
                    Y = contentBounds.Y,
                    Width = contentBounds.Width,
                    Height = contentBounds.Height
                });
            }
        }

        private void DrawBox(Region region)
        {
            // Draw corners
            buffer.SetChar(region.X, region.Y, BoxChars["topLeft"], region.BorderColor);
            buffer.SetChar(region.X + region.Width - 1, region.Y, BoxChars["topRight"], region.BorderColor);
            buffer.SetChar(region.X, region.Y + region.Height - 1, BoxChars["bottomLeft"], region.BorderColor);
            buffer.SetChar(region.X + region.Width - 1, region.Y + region.Height - 1, BoxChars["bottomRight"], region.BorderColor);

            // Draw horizontal borders
            for (int x = region.X + 1; x < region.X + region.Width - 1; x++)
            {
                buffer.SetChar(x, region.Y, BoxChars["horizontal"], region.BorderColor);
                buffer.SetChar(x, region.Y + region.Height - 1, BoxChars["horizontal"], region.BorderColor);
            }

            // Draw vertical borders
            for (int y = region.Y + 1; y < region.Y + region.Height - 1; y++)
            {
                buffer.SetChar(region.X, y, BoxChars["vertical"], region.BorderColor);
                buffer.SetChar(region.X + region.Width - 1, y, BoxChars["vertical"], region.BorderColor);
            }

            // Draw title if present
            if (!string.IsNullOrEmpty(region.Name))
            {
                var title = $" {region.Name} ";
                int titleX = region.X + (region.Width - title.Length) / 2;
                buffer.WriteString(titleX, region.Y, title, region.TitleColor);
            }
        }

        public static bool IsDoubleWidth(char c)
        {
            // Check for CJK character ranges
            return (c >= 0x1100 && c <= 0x11FF) ||   // Hangul Jamo
                   (c >= 0x2E80 && c <= 0x9FFF) ||   // CJK Radicals through CJK Unified Ideographs
                   (c >= 0xAC00 && c <= 0xD7AF) ||   // Hangul Syllables
                   (c >= 0xF900 && c <= 0xFAFF) ||   // CJK Compatibility Ideographs
                   (c >= 0xFE30 && c <= 0xFE4F) ||   // CJK Compatibility Forms
                   (c >= 0xFF00 && c <= 0xFFEF);      // Halfwidth and Fullwidth Forms
        }

        public void RenderWrappedText(Region region, IEnumerable<string> lines, ConsoleColor color)
        {
            var bounds = region.ContentBounds;
            var allWrappedLines = new List<string>();

            foreach (var line in lines)
            {
                allWrappedLines.AddRange(WrapText(line, bounds.Width));
            }

            int totalLines = allWrappedLines.Count;
            int startLine = Math.Max(0, totalLines - bounds.Height);
            int currentY = bounds.Y;

            for (int i = startLine; i < totalLines && currentY < bounds.Y + bounds.Height; i++)
            {
                var line = allWrappedLines[i];
                int lineWidth = 0;
                var visibleLine = new StringBuilder();

                // Calculate visible portion considering double-width characters
                foreach (char c in line)
                {
                    int charWidth = IsDoubleWidth(c) ? 2 : 1;
                    if (lineWidth + charWidth > bounds.Width)
                        break;

                    visibleLine.Append(c);
                    lineWidth += charWidth;
                }

                // Pad with spaces to fill the width
                while (lineWidth < bounds.Width)
                {
                    visibleLine.Append(' ');
                    lineWidth++;
                }

                buffer.WriteString(bounds.X, currentY, visibleLine.ToString(), color);
                currentY++;
            }
        }

        private IEnumerable<string> WrapText(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield return string.Empty;
                yield break;
            }

            var currentLine = new StringBuilder();
            int currentWidth = 0;

            foreach (var word in text.Split(' '))
            {
                int wordWidth = word.Sum(c => IsDoubleWidth(c) ? 2 : 1);

                if (currentWidth + wordWidth + (currentWidth > 0 ? 1 : 0) > width)
                {
                    if (currentLine.Length > 0)
                    {
                        yield return currentLine.ToString();
                        currentLine.Clear();
                        currentWidth = 0;
                    }

                    // Handle words that are longer than the width
                    if (wordWidth > width)
                    {
                        var temp = new StringBuilder();
                        int tempWidth = 0;

                        foreach (char c in word)
                        {
                            int charWidth = IsDoubleWidth(c) ? 2 : 1;
                            if (tempWidth + charWidth > width)
                            {
                                yield return temp.ToString();
                                temp.Clear();
                                tempWidth = 0;
                            }
                            temp.Append(c);
                            tempWidth += charWidth;
                        }

                        if (temp.Length > 0)
                        {
                            currentLine.Append(temp);
                            currentWidth = tempWidth;
                        }
                        continue;
                    }
                }

                if (currentWidth > 0)
                {
                    currentLine.Append(' ');
                    currentWidth++;
                }

                currentLine.Append(word);
                currentWidth += wordWidth;
            }

            if (currentLine.Length > 0)
                yield return currentLine.ToString();
        }

        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            cancellationSource.Cancel();
            renderTask.Wait();
            cancellationSource.Dispose();
            Console.CursorVisible = true;
        }

        public void QueueRender()
        {
            lock (renderLock)
            {
                isDirty = true;
            }
        }
        private void RenderInput(Region inputRegion)
        {
            int x = inputRegion.X + 1;
            int y = inputRegion.Y + 1;
            string prompt = "> ";
            int maxInputLength = inputRegion.Width - 2 - prompt.Length;

            // Clear the input line
            for (int i = 0; i < inputRegion.Width - 2; i++)
            {
                buffer.SetChar(x + i, y, ' ', currentInputColor);
            }

            // Write prompt
            buffer.WriteString(x, y, prompt, currentInputColor);

            // Calculate visible portion of input text
            string displayText;
            if (currentInputText.Length > maxInputLength)
            {
                displayText = currentInputText.Substring(currentInputText.Length - maxInputLength);
            }
            else
            {
                displayText = currentInputText;
            }

            // Write input text
            if (!string.IsNullOrEmpty(displayText))
            {
                buffer.WriteString(x + prompt.Length, y, displayText, currentInputColor);
            }

            // Handle cursor blinking based on settings
            if (displayConfig.EnableCursorBlink &&
                (DateTime.Now - lastCursorBlink).TotalMilliseconds >= displayConfig.CursorBlinkRateMs)
            {
                cursorVisible = !cursorVisible;
                lastCursorBlink = DateTime.Now;
            }

            // Draw cursor at correct position
            int cursorX = x + prompt.Length + displayText.Length;
            if (cursorX < x + inputRegion.Width - 1)
            {
                char cursorChar = displayConfig.UseUnicodeBorders ? '▌' : '|';
                buffer.SetChar(cursorX, y, cursorVisible ? cursorChar : ' ', currentInputColor);
            }
        }

        public void UpdateInputText(string text, ConsoleColor color)
        {
            lock (renderLock)
            {
                currentInputText = text;
                currentInputColor = color;
                QueueRender();
            }
        }

        // Add method to update display settings
        public void UpdateDisplaySettings(ConsoleDisplayConfig newConfig)
        {
            lock (renderLock)
            {
                displayConfig = newConfig;
                BoxChars = displayConfig.UseUnicodeBorders
                    ? (displayConfig.UseCurvedBorders ? CurvedBoxChars : UnicodeBoxChars)
                    : AsciiBoxChars;
                isDirty = true;
            }
        }

        public void RenderMap(Region region, WorldData world, WorldRegion currentRegion)
        {
            if (!region.IsVisible) return;

            // Calculate map bounds with margins
            const float MARGIN = 2.0f; // Add margin to ensure regions aren't at edges
            var positions = world.Regions.Select(r => r.Position).ToList();
            var minX = positions.Min(p => p.X) - MARGIN;
            var maxX = positions.Max(p => p.X) + MARGIN;
            var minY = positions.Min(p => p.Y) - MARGIN;
            var maxY = positions.Max(p => p.Y) + MARGIN;

            // Ensure current region is in view by expanding bounds if needed
            minX = Math.Min(minX, currentRegion.Position.X - MARGIN);
            maxX = Math.Max(maxX, currentRegion.Position.X + MARGIN);
            minY = Math.Min(minY, currentRegion.Position.Y - MARGIN);
            maxY = Math.Max(maxY, currentRegion.Position.Y + MARGIN);

            // Calculate map dimensions
            var mapWidth = region.Width - 2;
            var mapHeight = region.Height - 2;

            // Calculate scale to fit the map while preserving aspect ratio
            float worldWidth = maxX - minX;
            float worldHeight = maxY - minY;
            float worldAspect = worldWidth / worldHeight;
            float mapAspect = mapWidth / (float)mapHeight;

            float scale;
            float offsetX = 0, offsetY = 0;

            if (worldAspect > mapAspect)
            {
                // World is wider than map
                scale = mapWidth / worldWidth;
                offsetY = (mapHeight - (worldHeight * scale)) / 2;
            }
            else
            {
                // World is taller than map
                scale = mapHeight / worldHeight;
                offsetX = (mapWidth - (worldWidth * scale)) / 2;
            }

            // Create and clear map buffer
            var map = new char[mapWidth, mapHeight];
            var colors = new ConsoleColor[mapWidth, mapHeight];
            for (int y = 0; y < mapHeight; y++)
                for (int x = 0; x < mapWidth; x++)
                {
                    map[x, y] = ' ';
                    colors[x, y] = ConsoleColor.Gray;
                }

            // Draw connections first
            foreach (var r in world.Regions)
            {
                foreach (var connIdx in r.Connections)
                {
                    var conn = world.Regions[connIdx];
                    DrawLine(r.Position, conn.Position, '·', ConsoleColor.DarkGray);
                }
            }

            // Draw regions
            foreach (var r in world.Regions)
            {
                var x = TransformX(r.Position.X);
                var y = TransformY(r.Position.Y);

                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                {
                    var symbol = r == currentRegion ? '☆' : '○';
                    var color = r == currentRegion ? ConsoleColor.Yellow : ConsoleColor.White;
                    map[x, y] = symbol;
                    colors[x, y] = color;
                }
            }

            // Render to buffer
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    buffer.SetChar(region.X + 1 + x, region.Y + 1 + y, map[x, y], colors[x, y]);
                }
            }

            // Local coordinate transformation functions
            int TransformX(float x) => (int)(((x - minX) * scale) + offsetX);
            int TransformY(float y) => (int)(((y - minY) * scale) + offsetY);

            void DrawLine(Vector2 start, Vector2 end, char symbol, ConsoleColor color)
            {
                int x1 = TransformX(start.X), y1 = TransformY(start.Y);
                int x2 = TransformX(end.X), y2 = TransformY(end.Y);

                int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
                int sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1;
                int err = dx - dy;

                while (true)
                {
                    if (x1 >= 0 && x1 < mapWidth && y1 >= 0 && y1 < mapHeight)
                    {
                        if (map[x1, y1] == ' ')
                        {
                            map[x1, y1] = symbol;
                            colors[x1, y1] = color;
                        }
                    }

                    if (x1 == x2 && y1 == y2) break;
                    int e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x1 += sx; }
                    if (e2 < dx) { err += dx; y1 += sy; }
                }
            }
        }

        public void RenderRegionMap(Region region, WorldRegion currentRegion)
        {
            if (!region.IsVisible) return;

            int mapWidth = region.Width - 2;
            int mapHeight = region.Height - 2;

            // Create a map buffer
            var mapChars = new char[mapWidth, mapHeight];
            var mapColors = new ConsoleColor[mapWidth, mapHeight];

            // Clear the map area
            for (int y = 0; y < mapHeight; y++)
                for (int x = 0; x < mapWidth; x++)
                {
                    buffer.SetChar(region.X + 1 + x, region.Y + 1 + y, ' ', ConsoleColor.Gray);
                }

            var locations = currentRegion.Locations;
            if (locations.Count == 0)
            {
                buffer.WriteString(region.X + 2, region.Y + 2, "No locations available", ConsoleColor.Gray);
                return;
            }

            // Calculate layout
            int maxLocationsPerRow = 3;  // Limit locations per row for better spacing
            int rows = (int)Math.Ceiling(locations.Count / (float)maxLocationsPerRow);
            int effectiveWidth = mapWidth - 4;  // Leave margins
            int effectiveHeight = mapHeight - 4;
            int cellWidth = effectiveWidth / Math.Min(locations.Count, maxLocationsPerRow);
            int cellHeight = effectiveHeight / rows;

            // Draw locations
            for (int i = 0; i < locations.Count; i++)
            {
                int row = i / maxLocationsPerRow;
                int col = i % maxLocationsPerRow;

                // Calculate center position for this location
                int centerX = region.X + 3 + (col * cellWidth) + (cellWidth / 2);
                int centerY = region.Y + 3 + (row * cellHeight) + (cellHeight / 2);

                var location = locations[i];
                char symbol = GetLocationSymbol(location);
                var color = GetLocationColor(location);

                // Draw location
                buffer.SetChar(centerX, centerY, symbol, color);

                // Draw name below the symbol
                if (centerY + 1 < region.Y + region.Height - 1)
                {
                    string name = location.NameId.ToString();
                    int nameX = Math.Max(centerX - (name.Length / 2), region.X + 2);
                    buffer.WriteString(nameX, centerY + 1, name, color);
                }

                // Draw paths to other locations
                DrawLocationConnections(i, location, locations, maxLocationsPerRow, centerX, centerY, color);
            }

            // Local helper to draw connections between locations
            void DrawLocationConnections(int currentIndex, Location current, List<Location> allLocations,
                int locPerRow, int startX, int startY, ConsoleColor color)
            {
                for (int j = 0; j < currentIndex; j++)
                {
                    var other = allLocations[j];

                    // Check if locations are connected (share NPCs or Items)
                    bool connected = current.NPCs.Intersect(other.NPCs).Any() ||
                                   current.Items.Intersect(other.Items).Any();

                    if (connected)
                    {
                        int otherRow = j / locPerRow;
                        int otherCol = j % locPerRow;
                        int otherX = region.X + 3 + (otherCol * cellWidth) + (cellWidth / 2);
                        int otherY = region.Y + 3 + (otherRow * cellHeight) + (cellHeight / 2);

                        DrawLine(startX, startY, otherX, otherY, '·', ConsoleColor.DarkGray);
                    }
                }
            }

            // Line drawing helper using Bresenham's algorithm
            void DrawLine(int x1, int y1, int x2, int y2, char symbol, ConsoleColor color)
            {
                int dx = Math.Abs(x2 - x1);
                int dy = Math.Abs(y2 - y1);
                int sx = x1 < x2 ? 1 : -1;
                int sy = y1 < y2 ? 1 : -1;
                int err = dx - dy;

                while (true)
                {
                    // Only draw if within bounds and not overlapping with location symbols
                    if (x1 >= region.X + 1 && x1 < region.X + region.Width - 1 &&
                        y1 >= region.Y + 1 && y1 < region.Y + region.Height - 1)
                    {
                        var currentChar = buffer.GetChar(x1, y1);
                        if (currentChar == ' ')  // Only draw line if space is empty
                        {
                            buffer.SetChar(x1, y1, symbol, color);
                        }
                    }

                    if (x1 == x2 && y1 == y2) break;
                    int e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x1 += sx; }
                    if (e2 < dx) { err += dx; y1 += sy; }
                }
            }
        }

        private char GetLocationSymbol(Location location)
        {
            // Assuming location.TypeId indicates the type of location
            return location.TypeId switch
            {
                var id when id == 0 => '◆',  // Important location
                var id when id == 1 => '■',  // Building
                var id when id == 2 => '▲',  // Mountain/Hill
                var id when id == 3 => '☘',  // Nature location
                var id when id == 4 => '♠',  // Forest location
                var id when id == 5 => '≈',  // Water location
                var id when id == 6 => '†',  // Temple/Shrine
                var id when id == 7 => '⌂',  // House/Inn
                var id when id == 8 => '♦',  // Shop/Market
                _ => '○'                     // Default unknown location
            };
        }

        private ConsoleColor GetLocationColor(Location location)
        {
            // Assuming location.TypeId indicates the type of location
            return location.TypeId switch
            {
                var id when id == 0 => ConsoleColor.Yellow,     // Important location
                var id when id == 1 => ConsoleColor.White,      // Building
                var id when id == 2 => ConsoleColor.DarkGray,   // Mountain/Hill
                var id when id == 3 => ConsoleColor.Green,      // Nature location
                var id when id == 4 => ConsoleColor.DarkGreen,  // Forest location
                var id when id == 5 => ConsoleColor.Blue,       // Water location
                var id when id == 6 => ConsoleColor.Cyan,       // Temple/Shrine
                var id when id == 7 => ConsoleColor.DarkYellow, // House/Inn
                var id when id == 8 => ConsoleColor.Magenta,    // Shop/Market
                _ => ConsoleColor.Gray                          // Default unknown location
            };
        }

    }

    public class ConsoleBuffer
    {
        private char[] chars;
        private char[] previousChars;
        private ConsoleColor[] colors;
        private ConsoleColor[] previousColors;
        private int width;
        private int height;
        private int contentWidth;  // Track the actual content width
        private int contentHeight; // Track the actual content height

        public int Width => width;
        public int Height => height;

        public ConsoleBuffer(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.contentWidth = width;
            this.contentHeight = height;

            ResizeBuffers(width, height);
        }

        private void ResizeBuffers(int newWidth, int newHeight)
        {
            var newSize = newWidth * newHeight;
            chars = new char[newSize];
            colors = new ConsoleColor[newSize];
            previousChars = new char[newSize];
            previousColors = new ConsoleColor[newSize];

            Array.Fill(chars, ' ');
            Array.Fill(colors, ConsoleColor.Gray);
            Array.Fill(previousChars, ' ');
            Array.Fill(previousColors, ConsoleColor.Gray);
        }

        public void Resize(int newWidth, int newHeight)
        {
            // If new size is larger in either dimension, preserve old content
            if (newWidth >= width || newHeight >= height)
            {
                var oldChars = chars;
                var oldColors = colors;
                var oldWidth = width;
                var oldHeight = height;

                ResizeBuffers(newWidth, newHeight);

                // Copy old content
                for (int y = 0; y < Math.Min(oldHeight, newHeight); y++)
                {
                    for (int x = 0; x < Math.Min(oldWidth, newWidth); x++)
                    {
                        int oldIndex = y * oldWidth + x;
                        int newIndex = y * newWidth + x;
                        chars[newIndex] = oldChars[oldIndex];
                        colors[newIndex] = oldColors[oldIndex];
                    }
                }
            }
            else
            {
                // If new size is smaller, create new buffer and copy visible portion
                var newChars = new char[newWidth * newHeight];
                var newColors = new ConsoleColor[newWidth * newHeight];
                var newPreviousChars = new char[newWidth * newHeight];
                var newPreviousColors = new ConsoleColor[newWidth * newHeight];

                Array.Fill(newChars, ' ');
                Array.Fill(newColors, ConsoleColor.Gray);
                Array.Fill(newPreviousChars, ' ');
                Array.Fill(newPreviousColors, ConsoleColor.Gray);

                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        int oldIndex = y * width + x;
                        int newIndex = y * newWidth + x;
                        newChars[newIndex] = chars[oldIndex];
                        newColors[newIndex] = colors[oldIndex];
                    }
                }

                chars = newChars;
                colors = newColors;
                previousChars = newPreviousChars;
                previousColors = newPreviousColors;
            }

            width = newWidth;
            height = newHeight;
            contentWidth = Math.Max(contentWidth, newWidth);
            contentHeight = Math.Max(contentHeight, newHeight);
        }

        public void Clear()
        {
            Array.Fill(chars, ' ');
            Array.Fill(colors, ConsoleColor.Gray);
        }

        public void SetChar(int x, int y, char c, ConsoleColor color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                int index = y * width + x;
                chars[index] = c;
                colors[index] = color;
            }
        }

        public void WriteString(int x, int y, string text, ConsoleColor color)
        {
            int currentX = x;
            foreach (char c in text)
            {
                if (currentX >= width) break;

                SetChar(currentX, y, c, color);
                currentX += IsDoubleWidth(c) ? 2 : 1;

                // Add a placeholder space for double-width characters
                if (IsDoubleWidth(c) && currentX < width)
                {
                    SetChar(currentX - 1, y, '\0', color); // Use null character as placeholder
                }
            }
        }

        private bool IsDoubleWidth(char c) => ConsoleWindowManager.IsDoubleWidth(c);

        public void Flush()
        {
            Console.SetCursorPosition(0, 0);
            var sb = new StringBuilder(chars.Length);
            ConsoleColor currentColor = colors[0];

            // Only change colors if enabled in settings
            if (GameSettings.Instance.Display.UseColors)
            {
                Console.ForegroundColor = currentColor;
            }

            for (int i = 0; i < chars.Length; i++)
            {
                if (GameSettings.Instance.Display.UseColors && colors[i] != currentColor)
                {
                    // Flush current buffer
                    Console.Write(sb.ToString());
                    sb.Clear();

                    // Change color
                    currentColor = colors[i];
                    Console.ForegroundColor = currentColor;
                }
                sb.Append(chars[i]);
            }

            // Flush remaining buffer
            Console.Write(sb.ToString());

            // Reset color if we were using colors
            if (GameSettings.Instance.Display.UseColors)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public char GetChar(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return chars[y * width + x];
            }
            return ' ';
        }
    }
}
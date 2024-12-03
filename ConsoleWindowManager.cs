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

        private static readonly Dictionary<string, char> BoxChars = new()
        {
            ["topLeft"] = '┌',
            ["topRight"] = '┐',
            ["bottomLeft"] = '└',
            ["bottomRight"] = '┘',
            ["horizontal"] = '─',
            ["vertical"] = '│'
        };

        public ConsoleWindowManager()
        {
            Console.CursorVisible = false;
            lastConsoleWidth = Console.WindowWidth;
            lastConsoleHeight = Console.WindowHeight;
            buffer = new ConsoleBuffer(Console.WindowWidth, Console.WindowHeight);
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

                    if (isDirty || (DateTime.Now - lastCursorBlink).TotalMilliseconds >= CURSOR_BLINK_MS)
                    {
                        Render();
                        isDirty = false;
                    }
                    Thread.Sleep(16);
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

            // Handle cursor blinking
            if ((DateTime.Now - lastCursorBlink).TotalMilliseconds >= CURSOR_BLINK_MS)
            {
                cursorVisible = !cursorVisible;
                lastCursorBlink = DateTime.Now;
            }

            // Draw cursor at correct position
            int cursorX = x + prompt.Length + displayText.Length;
            if (cursorX < x + inputRegion.Width - 1)
            {
                buffer.SetChar(cursorX, y, cursorVisible ? '▌' : ' ', currentInputColor);
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
            Console.ForegroundColor = currentColor;

            for (int i = 0; i < chars.Length; i++)
            {
                if (colors[i] != currentColor)
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
        }
    }
}
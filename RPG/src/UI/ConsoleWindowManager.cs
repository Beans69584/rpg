using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RPG.Core;
using RPG.Common;
using RPG.World.Data;

namespace RPG.UI
{

    /// <summary>
    /// Represents a rectangular region within a console window that can display content with borders and titles.
    /// </summary>
    public class Region
    {
        /// <summary>
        /// Gets or sets the X-coordinate of the region's top-left corner.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y-coordinate of the region's top-left corner.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Gets or sets the total width of the region, including borders.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the total height of the region, including borders.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the padding between the region's border and its content.
        /// </summary>
        public int Padding { get; set; } = 1;

        /// <summary>
        /// Gets or sets the Z-index which determines the drawing order when regions overlap.
        /// </summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the optional title text displayed at the top of the region.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the color of the region's border.
        /// </summary>
        public ConsoleColor BorderColor { get; set; } = ConsoleColor.Gray;

        /// <summary>
        /// Gets or sets the color of the region's title text.
        /// </summary>
        public ConsoleColor TitleColor { get; set; } = ConsoleColor.White;

        /// <summary>
        /// Gets or sets whether the region should be visible when rendered.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets the delegate responsible for rendering the region's content.
        /// </summary>
        public Action<Region>? RenderContent { get; set; }

        /// <summary>
        /// Gets the outer bounds of the region, including its borders.
        /// </summary>
        public Rectangle Bounds => new(X, Y, Width, Height);

        /// <summary>
        /// Gets the inner bounds of the region where content can be rendered, excluding borders and title area.
        /// </summary>
        public Rectangle ContentBounds => new(
            X + 1,
            Y + (string.IsNullOrEmpty(Name) ? 0 : 1),
            Width - 2,
            Height - (string.IsNullOrEmpty(Name) ? 1 : 2)
        );
    }

    /// <summary>
    /// Represents an immutable rectangular area defined by its position and dimensions.
    /// </summary>
    /// <param name="x">The X-coordinate of the rectangle's top-left corner.</param>
    /// <param name="y">The Y-coordinate of the rectangle's top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public readonly struct Rectangle(int x, int y, int width, int height)
    {
        /// <summary>
        /// Gets the X-coordinate of the rectangle's top-left corner.
        /// </summary>
        public int X { get; } = x;

        /// <summary>
        /// Gets the Y-coordinate of the rectangle's top-left corner.
        /// </summary>
        public int Y { get; } = y;

        /// <summary>
        /// Gets the width of the rectangle, ensuring it is never negative.
        /// </summary>
        public int Width { get; } = Math.Max(width, 0);

        /// <summary>
        /// Gets the height of the rectangle, ensuring it is never negative.
        /// </summary>
        public int Height { get; } = Math.Max(height, 0);

        /// <summary>
        /// Determines whether this rectangle intersects with another rectangle.
        /// </summary>
        /// <param name="other">The rectangle to test for intersection.</param>
        /// <returns>True if the rectangles intersect; otherwise, false.</returns>
        public bool Intersects(Rectangle other)
        {
            return X < other.X + other.Width &&
                   X + Width > other.X &&
                   Y < other.Y + other.Height &&
                   Y + Height > other.Y;
        }
    }

    /// <summary>
    /// Represents a region within a console window that can display content with borders and titles.
    /// </summary>
    public class ConsoleWindowManager : IDisposable
    {
        private readonly Dictionary<string, Region> regions = [];
        private readonly Lock renderLock = new();
        private ConsoleBuffer buffer;
        private bool isDisposed;
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly Task renderTask;
        private string currentInputText = "";
        private ConsoleColor currentInputColor = ConsoleColor.White;
        private bool isDirty = true;
        private bool cursorVisible = true;
        private DateTime lastCursorBlink = DateTime.UtcNow;
        private int lastConsoleWidth;
        private int lastConsoleHeight;
        private DateTime lastResize = DateTime.Now;
        private const int RESIZE_DEBOUNCE_MS = 100;
        private bool isResizing = false;

        private static readonly Dictionary<string, char> AsciiBoxChars = new()
        {
            ["topLeft"] = '+',
            ["topRight"] = '+',
            ["bottomLeft"] = '+',
            ["bottomRight"] = '+',
            ["horizontal"] = '-',
            ["vertical"] = '|'
        };

        private static readonly Dictionary<string, char> UnicodeBoxChars = new()
        {
            ["topLeft"] = '┌',
            ["topRight"] = '┐',
            ["bottomLeft"] = '└',
            ["bottomRight"] = '┘',
            ["horizontal"] = '─',
            ["vertical"] = '│'
        };

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

        /// <summary>
        /// Initializes a new instance of the ConsoleWindowManager class.
        /// </summary>
        public ConsoleWindowManager()
        {
            Console.CursorVisible = false;
            lastConsoleWidth = Console.WindowWidth;
            lastConsoleHeight = Console.WindowHeight;
            buffer = new ConsoleBuffer(Console.WindowWidth, Console.WindowHeight);

            displayConfig = GameSettings.Instance.Display;
#pragma warning disable IDE0045 // Convert to conditional expression
            if (displayConfig.UseUnicodeBorders)
            {
                BoxChars = displayConfig.UseCurvedBorders ? CurvedBoxChars : UnicodeBoxChars;
            }
            else
            {
                BoxChars = AsciiBoxChars;
            }
#pragma warning restore IDE0045 // Convert to conditional expression

            renderTask = Task.Run(RenderLoop);
        }

        /// <summary>
        /// Adds a new region to the console window manager with the specified name.
        /// </summary>
        /// <param name="name">Name of the region.</param>
        /// <param name="region">Region to add.</param>
        public void AddRegion(string name, Region region)
        {
            lock (renderLock)
            {
                ValidateAndAdjustRegion(region);
                regions[name] = region;
            }
        }

        /// <summary>
        /// Updates an existing region in the console window manager.
        /// </summary>
        /// <param name="name">Name of the region to update.</param>
        /// <param name="updateAction">Action to perform on the region before removal.</param>
        public void UpdateRegion(string name, Action<Region> updateAction)
        {
            lock (renderLock)
            {
                if (regions.TryGetValue(name, out Region? region))
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
                        (DateTime.UtcNow - lastCursorBlink).TotalMilliseconds >= displayConfig.CursorBlinkRateMs))
                    {
                        lock (renderLock)
                        {
                            if (!isDisposed)
                            {
                                Render();
                                isDirty = false;
                            }
                        }
                    }
                    Thread.Sleep(displayConfig.RefreshRateMs);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if the console window has been resized and updates the buffer if needed.
        /// </summary>
        /// <returns>True if the console window was resized; otherwise, false.</returns>
        public bool CheckResize()
        {
            DateTime now = DateTime.Now;
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

            foreach (Region? region in regions.Values.Where(r => r.IsVisible).OrderBy(r => r.ZIndex))
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
                Rectangle contentBounds = region.ContentBounds;
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
                string title = $" {region.Name} ";
                int titleX = region.X + ((region.Width - title.Length) / 2);
                buffer.WriteString(titleX, region.Y, title, region.TitleColor);
            }
        }

        /// <summary>
        /// Determines if a character is double-width based on Unicode character ranges.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is double-width; otherwise, false.</returns>
        public static bool IsDoubleWidth(char c)
        {
            // Check for CJK character ranges
            return c is (>= (char)0x1100 and <= (char)0x11FF) or   // Hangul Jamo
                   (>= (char)0x2E80 and <= (char)0x9FFF) or   // CJK Radicals through CJK Unified Ideographs
                   (>= (char)0xAC00 and <= (char)0xD7AF) or   // Hangul Syllables
                   (>= (char)0xF900 and <= (char)0xFAFF) or   // CJK Compatibility Ideographs
                   (>= (char)0xFE30 and <= (char)0xFE4F) or   // CJK Compatibility Forms
                   (>= (char)0xFF00 and <= (char)0xFFEF);      // Halfwidth and Fullwidth Forms
        }

        /// <summary>
        /// Renders a list of lines of text to the specified region, wrapping long lines as needed.
        /// </summary>
        /// <param name="region">The region to render the text to.</param>
        /// <param name="lines">The lines of text to render.</param>
        public void RenderWrappedText(Region region, IEnumerable<ColoredText> lines)
        {
            Rectangle bounds = region.ContentBounds;
            List<ColoredText> allWrappedLines = [];

            foreach (ColoredText line in lines)
            {
                foreach (string wrappedLine in WrapText(line.Text, bounds.Width))
                {
                    allWrappedLines.Add(new ColoredText(wrappedLine, line.Color));
                }
            }

            int totalLines = allWrappedLines.Count;
            int startLine = Math.Max(0, totalLines - bounds.Height);
            int currentY = bounds.Y;

            for (int i = startLine; i < totalLines && currentY < bounds.Y + bounds.Height; i++)
            {
                ColoredText line = allWrappedLines[i];
                int lineWidth = 0;
                StringBuilder visibleLine = new();

                // Calculate visible portion considering double-width characters
                foreach (char c in line.Text)
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

                buffer.WriteString(bounds.X, currentY, visibleLine.ToString(), line.Color);
                currentY++;
            }
        }

        private static IEnumerable<string> WrapText(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield return string.Empty;
                yield break;
            }

            StringBuilder currentLine = new();
            int currentWidth = 0;

            foreach (string word in text.Split(' '))
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
                        StringBuilder temp = new();
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
        private readonly Lock disposeLock = new();

        /// <summary>
        /// Releases all resources used by the ConsoleWindowManager.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCoreAsync().ConfigureAwait(false);
            Dispose(disposing: false);
        }

        private async ValueTask DisposeAsyncCoreAsync()
        {
            if (isDisposed) return;

            lock (disposeLock)
            {
                if (isDisposed) return;
                StopRendering();
                ClearDisplay();
            }

            await Task.CompletedTask; // Ensure async completion
        }

        /// <summary>
        /// Stops the render loop immediately
        /// </summary>
        private void StopRendering()
        {
            cancellationSource.Cancel();
            try
            {
                renderTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (OperationCanceledException)
            {
                // Expected during cleanup
            }
        }

        /// <summary>
        /// Cleans up the console display
        /// </summary>
        public void ClearDisplay()
        {
            lock (renderLock)
            {
                buffer.Clear();
                buffer.Flush();
                Console.Clear();
            }
        }

        /// <summary>
        /// Releases all resources used by the ConsoleWindowManager.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the ConsoleWindowManager and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            lock (disposeLock)
            {
                if (isDisposed) return;

                if (disposing)
                {
                    StopRendering();
                    ClearDisplay();
                    cancellationSource.Dispose();
                }

                Console.CursorVisible = true;
                isDisposed = true;
            }
        }

        /// <summary>
        /// Queues a region to be rendered on the next update.
        /// </summary>
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
            int MaxInputLength = inputRegion.Width - 2 - prompt.Length;

            // Clear the input line
            for (int i = 0; i < inputRegion.Width - 2; i++)
            {
                buffer.SetChar(x + i, y, ' ', currentInputColor);
            }

            // Write prompt
            buffer.WriteString(x, y, prompt, currentInputColor);

            // Calculate visible portion of input text
            string displayText = currentInputText.Length > MaxInputLength
                ? currentInputText[^MaxInputLength..]
                : currentInputText;

            // Write input text
            if (!string.IsNullOrEmpty(displayText))
            {
                buffer.WriteString(x + prompt.Length, y, displayText, currentInputColor);
            }

            // Handle cursor blinking based on settings
            if (displayConfig.EnableCursorBlink &&
                (DateTime.UtcNow - lastCursorBlink).TotalMilliseconds >= displayConfig.CursorBlinkRateMs)
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

        /// <summary>
        /// Updates the input text displayed in the input region.
        /// </summary>
        /// <param name="text">The new input text to display.</param>
        /// <param name="color">The color to use for the input text.</param>
        public void UpdateInputText(string text, ConsoleColor color)
        {
            lock (renderLock)
            {
                currentInputText = text;
                currentInputColor = color;
                QueueRender();
            }
        }

        /// <summary>
        /// Updates the display settings for the console window manager.
        /// </summary>
        /// <param name="newConfig">The new display settings to apply.</param>
        public void UpdateDisplaySettings(ConsoleDisplayConfig newConfig)
        {
            lock (renderLock)
            {
                displayConfig = newConfig;
                if (displayConfig.UseUnicodeBorders)
                {
                    BoxChars = displayConfig.UseCurvedBorders ? CurvedBoxChars : UnicodeBoxChars;
                }
                else
                {
                    BoxChars = AsciiBoxChars;
                }
                isDirty = true;
            }
        }

        /// <summary>
        /// Gets the current regions dictionary.
        /// </summary>
        public Dictionary<string, Region> GetRegions()
        {
            lock (renderLock)
            {
                return new Dictionary<string, Region>(regions);
            }
        }

        /// <summary>
        /// Removes a region from the window manager.
        /// </summary>
        /// <param name="name">The name of the region to remove.</param>
        public void RemoveRegion(string name)
        {
            lock (renderLock)
            {
                regions.Remove(name);
            }
        }
    }

    /// <summary>
    /// Represents a buffer for managing console output with character and color information.
    /// </summary>
    public class ConsoleBuffer
    {
        private char[] chars = [];
        private ConsoleColor[] colors = [];
        private int contentWidth;  // Track the actual content width
        private int contentHeight; // Track the actual content height

        /// <summary>
        /// Gets the width of the console buffer in characters.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Gets the height of the console buffer in characters.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ConsoleBuffer class with specified dimensions.
        /// </summary>
        /// <param name="width">The width of the buffer in characters.</param>
        /// <param name="height">The height of the buffer in characters.</param>
        public ConsoleBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            contentWidth = width;
            contentHeight = height;

            ResizeBuffers(width, height);
        }

        private void ResizeBuffers(int newWidth, int newHeight)
        {
            int newSize = newWidth * newHeight;
            chars = new char[newSize];
            colors = new ConsoleColor[newSize];

            Array.Fill(chars, ' ');
            Array.Fill(colors, ConsoleColor.Gray);
        }

        /// <summary>
        /// Resizes the console buffer to the specified dimensions.
        /// </summary>
        /// <param name="newWidth">The new width of the buffer in characters.</param>
        /// <param name="newHeight">The new height of the buffer in characters.</param>
        public void Resize(int newWidth, int newHeight)
        {
            // If new size is larger in either dimension, preserve old content
            if (newWidth >= Width || newHeight >= Height)
            {
                char[] oldChars = chars;
                ConsoleColor[] oldColors = colors;
                int oldWidth = Width;
                int oldHeight = Height;

                ResizeBuffers(newWidth, newHeight);

                // Copy old content
                for (int y = 0; y < Math.Min(oldHeight, newHeight); y++)
                {
                    for (int x = 0; x < Math.Min(oldWidth, newWidth); x++)
                    {
                        int oldIndex = (y * oldWidth) + x;
                        int newIndex = (y * newWidth) + x;
                        chars[newIndex] = oldChars[oldIndex];
                        colors[newIndex] = oldColors[oldIndex];
                    }
                }
            }
            else
            {
                // If new size is smaller, create new buffer and copy visible portion
                char[] newChars = new char[newWidth * newHeight];
                ConsoleColor[] newColors = new ConsoleColor[newWidth * newHeight];

                Array.Fill(newChars, ' ');
                Array.Fill(newColors, ConsoleColor.Gray);

                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        int oldIndex = (y * Width) + x;
                        int newIndex = (y * newWidth) + x;
                        newChars[newIndex] = chars[oldIndex];
                        newColors[newIndex] = colors[oldIndex];
                    }
                }

                chars = newChars;
                colors = newColors;
            }

            Width = newWidth;
            Height = newHeight;
            contentWidth = Math.Max(contentWidth, newWidth);
            contentHeight = Math.Max(contentHeight, newHeight);
        }

        /// <summary>
        /// Clears the console buffer by filling it with spaces and default colors.
        /// </summary>
        public void Clear()
        {
            Array.Fill(chars, ' ');
            Array.Fill(colors, ConsoleColor.Gray);
        }

        /// <summary>
        /// Sets the character and color at the specified position in the buffer.
        /// </summary>
        /// <param name="x">The X-coordinate of the character.</param>
        /// <param name="y">The Y-coordinate of the character.</param>
        /// <param name="c">The character to set.</param>
        /// <param name="color">The color to set.</param>
        public void SetChar(int x, int y, char c, ConsoleColor color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                int index = (y * Width) + x;
                chars[index] = c;
                colors[index] = color;
            }
        }

        /// <summary>
        /// Writes a string with automatic wrapping and bounds checking.
        /// </summary>
        /// <param name="x">The X-coordinate of the start of the string.</param>
        /// <param name="y">The Y-coordinate of the start of the string.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="color">The color to set.</param>
        public void WriteString(int x, int y, string text, ConsoleColor color)
        {
            if (y < 0 || y >= Height) return;
            if (x >= Width) return;

            int startX = Math.Max(0, x);
            int currentX = startX;
            int currentY = y;

            foreach (char c in text)
            {
                // Handle line breaks
                if (c == '\n')
                {
                    currentY++;
                    currentX = startX;
                    if (currentY >= Height) break;
                    continue;
                }

                int charWidth = IsDoubleWidth(c) ? 2 : 1;

                // Check if we need to wrap to next line
                if (currentX + charWidth > Width)
                {
                    currentY++;
                    currentX = startX;
                    if (currentY >= Height) break;
                }

                // Set the character if we're within bounds
                if (currentY < Height)
                {
                    SetChar(currentX, currentY, c, color);

                    // For double-width characters, add a placeholder
                    if (IsDoubleWidth(c) && currentX + 1 < Width)
                    {
                        SetChar(currentX + 1, currentY, '\0', color);
                    }

                    currentX += charWidth;
                }
            }
        }

        private static bool IsDoubleWidth(char c)
        {
            return ConsoleWindowManager.IsDoubleWidth(c);
        }

        /// <summary>
        /// Flushes the console buffer to the console window.
        /// </summary>
        public void Flush()
        {
            Console.SetCursorPosition(0, 0);
            StringBuilder sb = new(chars.Length);
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

        /// <summary>
        /// Gets the character at the specified position in the buffer.
        /// </summary>
        /// <param name="x">The X-coordinate of the character.</param>
        /// <param name="y">The Y-coordinate of the character.</param>
        public char GetChar(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height ? chars[(y * Width) + x] : ' ';
        }
    }
}
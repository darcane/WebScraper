using System.Collections.Frozen;

namespace WebScraper
{
    internal static class Helpers
    {
        public const string BaseUrl = @"https://books.toscrape.com/";

        private static readonly Dictionary<int, string> _userMessages = new()
        {
            {0, "Initializing..."},
            {5, "Checking permissions..."},
            {10, "Connecting to site..."},
            {15, "Analyzing page structure..."},
            {20, "Downloading HTML files..."},
            {25, "Fetching additional resources..."},
            {30, "Gathering images..."},
            {35, "Extracting metadata..."},
            {40, "Capturing scripts..."},
            {45, "Preparing for dynamic content..."},
            {50, "Halfway there! Processing data..."},
            {55, "Assembling the puzzle..."},
            {60, "Saving files locally..."},
            {65, "Sorting and organizing..."},
            {70, "Optimizing download speed..."},
            {75, "Conducting final checks..."},
            {80, "Verifying downloaded content..."},
            {85, "Enhancing user interface..."},
            {90, "Finalizing..."},
            {95, "Almost there! Wrapping up..."},
            {100, "Thank you for your patience!"},
        };

        public static FrozenDictionary<int, string> UserMessages => _userMessages.ToFrozenDictionary();
        public static int MapDoubleToNearestKey(double value)
        {
            int percentage = (int)Math.Round(value * 100 / 5) * 5;
            return _userMessages.Keys.Contains(percentage) ? percentage : _userMessages.Keys.Min(k => Math.Abs(k - percentage));
        }
                
        public static async Task WriteProgress(int visited, int remaining)
        {
            Console.SetCursorPosition(0, 3);
            await Console.Out.WriteLineAsync($"Processed pages = {visited:N0}".PadRight(Console.BufferWidth));
            await Console.Out.WriteLineAsync($"Remaining pages = {remaining:N0}".PadRight(Console.BufferWidth));
            await Console.Out.WriteLineAsync($"".PadRight(Console.BufferWidth));
            double progress = (double)visited / (visited + remaining);
            await Console.Out.WriteLineAsync($"Progress : {progress:P2}".PadRight(Console.BufferWidth));
            await Console.Out.WriteLineAsync(Helpers.UserMessages[Helpers.MapDoubleToNearestKey(progress)].PadRight(Console.BufferWidth));
        }
    }
}

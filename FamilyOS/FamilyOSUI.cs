using System;
using System.Collections.Generic;

namespace PocketFence.FamilyOS.UI
{
    /// <summary>
    /// Enhanced UI utility for modern, colorful console interface
    /// Provides consistent styling, animations, and improved user experience
    /// </summary>
    public static class FamilyOSUI
    {
        // ASCII-only output toggle
        public static bool UseAscii { get; private set; } = false;
        public static void EnableAscii() { UseAscii = true; }

        // Color schemes for different user types
        private static readonly Dictionary<string, ConsoleColor> UserTypeColors = new()
        {
            { "Parent", ConsoleColor.Blue },
            { "Teen", ConsoleColor.Cyan },
            { "Elementary", ConsoleColor.Green },
            { "Child", ConsoleColor.Yellow }
        };

        private static readonly Dictionary<string, ConsoleColor> StatusColors = new()
        {
            { "Success", ConsoleColor.Green },
            { "Error", ConsoleColor.Red },
            { "Warning", ConsoleColor.Yellow },
            { "Info", ConsoleColor.Cyan },
            { "Primary", ConsoleColor.Blue },
            { "Secondary", ConsoleColor.Gray }
        };

        // Modern box drawing characters
        private static string TopLeft => UseAscii ? "+" : "╭";
        private static string TopRight => UseAscii ? "+" : "╮";
        private static string BottomLeft => UseAscii ? "+" : "╰";
        private static string BottomRight => UseAscii ? "+" : "╯";
        private static string Horizontal => UseAscii ? "-" : "─";
        private static string Vertical => UseAscii ? "|" : "│";
        private static string Cross => UseAscii ? "+" : "┼";

        private static string Sanitize(string s)
        {
            if (!UseAscii || string.IsNullOrEmpty(s)) return s;
            // Map common emojis/icons to ASCII tokens
            var map = new Dictionary<string, string>
            {
                { "🏠", "[HOME]" },
                { "🛡️", "[SECURITY]" },
                { "🔐", "[AUTH]" },
                { "⚠️", "[WARN]" },
                { "✅", "[OK]" },
                { "❌", "[ERR]" },
                { "🎉", "[DONE]" },
                { "👨‍👩‍👧‍👦", "[FAMILY]" },
                { "📊", "[STATS]" },
                { "🔧", "[CONFIG]" },
                { "📋", "[LIST]" },
                { "⏱️", "[TIME]" },
                { "📱", "[APPS]" },
                { "🔍", "[SEARCH]" },
                { "🔓", "[UNLOCK]" },
                { "🕒", "[CLOCK]" },
                { "🚪", "[EXIT]" },
                { "🔄", "[RETRY]" },
                { "🎯", "[SELECT]" },
                { "🥷", "[STEALTH]" },
                { "👧", "[CHILD]" },
                { "🖥️", "[WINDOWS DEMO]" },
                { "🟢", "[ON]" },
                { "⚫", "[OFF]" },
                { "🌐", "[BROWSER]" },
                { "📚", "[EDU]" },
                { "🎮", "[GAMES]" },
                { "💬", "[CHAT]" },
                { "📁", "[FILES]" }
            };
            foreach (var kv in map)
            {
                s = s.Replace(kv.Key, kv.Value);
            }
            // Replace box characters just in case
            s = s.Replace("╭", "+").Replace("╮", "+").Replace("╰", "+").Replace("╯", "+")
                 .Replace("─", "-").Replace("│", "|").Replace("┼", "+");
            return s;
        }

        private static void WriteAsciiAware(string s, ConsoleColor? color = null)
        {
            var msg = Sanitize(s);
            if (color.HasValue) Console.ForegroundColor = color.Value;
            Console.WriteLine(msg);
            if (color.HasValue) Console.ResetColor();
        }

        /// <summary>
        /// Clear screen and display animated welcome header
        /// </summary>
        public static async Task ShowWelcomeHeaderAsync()
        {
            Console.Clear();
            
            // Animated header display
            var title = UseAscii ? "PocketFence FamilyOS" : "🏠 PocketFence FamilyOS";
            var subtitle = "Safe Computing Environment for the Whole Family";
            
            WriteColoredLine("", ConsoleColor.White);
            WriteBoxedTitle(title, ConsoleColor.Magenta);
            await Task.Delay(200);
            
            WriteCentered(subtitle, ConsoleColor.Gray);
            WriteColoredLine("", ConsoleColor.White);
            
            // Feature highlights with icons and colors
            await ShowFeatureHighlights();
        }

        /// <summary>
        /// Display feature highlights with animations
        /// </summary>
        private static async Task ShowFeatureHighlights()
        {
            var features = new[]
            {
                (UseAscii ? "[SECURITY]" : "🛡️", "Enterprise-Grade Security", ConsoleColor.Red),
                (UseAscii ? "[EDU]" : "📚", "Educational Content Priority", ConsoleColor.Blue),
                (UseAscii ? "[TIME]" : "⏰", "Smart Screen Time Management", ConsoleColor.Yellow),
                (UseAscii ? "[FAMILY]" : "👨‍👩‍👧‍👦", "Family-Friendly Interface", ConsoleColor.Green),
                (UseAscii ? "[SELECT]" : "🎯", "Age-Appropriate Content", ConsoleColor.Cyan),
                (UseAscii ? "[AUTH]" : "🔒", "Privacy Protection Built-In", ConsoleColor.Magenta)
            };

            foreach (var (icon, text, color) in features)
            {
                WriteIndented($"{icon} {text}", 2, color);
                await Task.Delay(100);
            }
            
            WriteColoredLine("", ConsoleColor.White);
        }

        /// <summary>
        /// Display modern login interface
        /// </summary>
        public static void ShowLoginInterface()
        {
            Console.Clear();
            WriteBoxedTitle(UseAscii ? "[AUTH] FamilyOS Login" : "🔐 FamilyOS Login", ConsoleColor.Blue);
            WriteColoredLine("", ConsoleColor.White);
            
            // Display available accounts with visual styling
            WriteColoredLine(UseAscii ? "[FAMILY] Family Members Available:" : "👨‍👩‍👧‍👦 Family Members Available:", ConsoleColor.Cyan);
            WriteIndented("Parents: mom, dad", 2, ConsoleColor.Blue);
            WriteIndented("Children: sarah, alex", 2, ConsoleColor.Green);
            WriteColoredLine("", ConsoleColor.White);
        }

        /// <summary>
        /// Display enhanced main menu with user-specific styling
        /// </summary>
        public static void ShowMainMenu(string userName, string ageGroup, string role, DateTime lastLogin)
        {
            Console.Clear();
            
            // User-specific header color
            var userColor = GetUserColor(ageGroup, role);
            var headerIcon = GetUserIcon(ageGroup, role);
            WriteBoxedTitle($"{Sanitize(headerIcon)} Welcome, {userName}!", userColor);
            
            // User info panel
            WriteUserInfoPanel(ageGroup, role, lastLogin, userColor);
            
            // Main menu sections with modern styling
            ShowApplicationsMenu();
            ShowSystemOptionsMenu(role);
            ShowNavigationOptions(role);
            
            WriteColoredLine("", ConsoleColor.White);
        }

        /// <summary>
        /// Display user information panel with modern styling
        /// </summary>
        private static void WriteUserInfoPanel(string ageGroup, string role, DateTime lastLogin, ConsoleColor userColor)
        {
            WriteColoredLine("╭─ User Information ──────────────────────╮", userColor);
            WriteColoredLine($"│ 👤 Age Group: {ageGroup.PadRight(26)} │", userColor);
            WriteColoredLine($"│ 🎭 Role: {role.PadRight(30)} │", userColor);
            WriteColoredLine($"│ 🕒 Last Login: {lastLogin:HH:mm:ss dd/MM/yyyy}   │", userColor);
            WriteColoredLine("╰─────────────────────────────────────────╯", userColor);
            WriteColoredLine("", ConsoleColor.White);
        }

        /// <summary>
        /// Display applications menu with icons and descriptions
        /// </summary>
        private static void ShowApplicationsMenu()
        {
            WriteColoredLine(UseAscii ? "[APPS] Available Applications" : "📱 Available Applications", ConsoleColor.Yellow);
            WriteColoredLine("═══════════════════════", ConsoleColor.Yellow);
            
            var apps = new[]
            {
                ("1", UseAscii ? "[BROWSER]" : "🌐", "Safe Browser", "Secure internet browsing with content filtering"),
                ("2", UseAscii ? "[EDU]" : "📚", "Educational Hub", "Learning resources and educational content"),
                ("3", UseAscii ? "[GAMES]" : "🎮", "Family Game Center", "Age-appropriate games and entertainment"),
                ("4", UseAscii ? "[CHAT]" : "💬", "Family Chat", "Secure family communication platform"),
                ("5", UseAscii ? "[FILES]" : "📁", "Family File Manager", "Organize and share family files safely"),
                ("6", UseAscii ? "[TIME]" : "⏰", "Screen Time Manager", "Monitor and manage screen time usage")
            };

            foreach (var (num, icon, name, desc) in apps)
            {
                WriteMenuOption(num, $"{icon} {name}", desc, ConsoleColor.Green);
            }
            
            WriteColoredLine("", ConsoleColor.White);
        }

        /// <summary>
        /// Display system options menu
        /// </summary>
        private static void ShowSystemOptionsMenu(string role)
        {
            WriteColoredLine(UseAscii ? "[CONFIG] System Options" : "🛠️  System Options", ConsoleColor.Cyan);
            WriteColoredLine("═════════════════", ConsoleColor.Cyan);
            
            WriteMenuOption("7", UseAscii ? "[STATS] System Status" : "📊 System Status", "View system health and statistics", ConsoleColor.Cyan);
            WriteMenuOption("8", UseAscii ? "[AUTH] Change Password" : "🔐 Change Password", "Update your account password", ConsoleColor.Cyan);
            
            if (role == "Parent")
            {
                WriteMenuOption("9", UseAscii ? "[FAMILY] Family Management" : "👨‍👩‍👧‍👦 Family Management", "Manage family members and settings (Parent Only)", ConsoleColor.Red);
                WriteMenuOption("10", UseAscii ? "[CONFIG] Password Management" : "🔧 Password Management", "Manage family passwords and security (Parent Only)", ConsoleColor.Red);
            }
            
            WriteColoredLine("", ConsoleColor.White);
        }

        /// <summary>
        /// Display navigation options
        /// </summary>
        private static void ShowNavigationOptions(string role)
        {
            WriteColoredLine(UseAscii ? "[EXIT] Navigation" : "🚪 Navigation", ConsoleColor.Magenta);
            WriteColoredLine("═══════════", ConsoleColor.Magenta);
            
            var switchOption = role == "Parent" ? "11" : "9";
            WriteMenuOption(switchOption, UseAscii ? "[RETRY] Switch User" : "🔄 Switch User", "Log in as different family member", ConsoleColor.Magenta);
            WriteMenuOption("0", UseAscii ? "[ERR] Exit FamilyOS" : "❌ Exit FamilyOS", "Close the application safely", ConsoleColor.Red);
        }

        /// <summary>
        /// Display a menu option with consistent formatting
        /// </summary>
        private static void WriteMenuOption(string number, string title, string description, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write($"  {number}. ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{title.PadRight(25)} ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"- {description}");
            Console.ResetColor();
        }

        /// <summary>
        /// Show animated loading indicator
        /// </summary>
        public static async Task ShowLoadingAsync(string message, int durationMs = 2000)
        {
            var spinner = UseAscii ? new[] { "-", "\\", "|", "/" } : new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            var startTime = DateTime.Now;
            var index = 0;

            Console.Write($"{message} ");
            var originalLeft = Console.CursorLeft;

            while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
            {
                Console.SetCursorPosition(originalLeft, Console.CursorTop);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(spinner[index]);
                Console.ResetColor();
                
                index = (index + 1) % spinner.Length;
                await Task.Delay(100);
            }

            Console.SetCursorPosition(originalLeft, Console.CursorTop);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅");
            Console.ResetColor();
        }

        /// <summary>
        /// Display success message with animation
        /// </summary>
        public static async Task ShowSuccessAsync(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("✅ ");
            Console.ForegroundColor = ConsoleColor.White;
            
            foreach (char c in message)
            {
                Console.Write(c);
                await Task.Delay(20);
            }
            
            Console.WriteLine();
            Console.ResetColor();
        }

        /// <summary>
        /// Display error message with styling
        /// </summary>
        public static void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("❌ ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Display warning message with styling
        /// </summary>
        public static void ShowWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("⚠️  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Display info message with styling
        /// </summary>
        public static void ShowInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ℹ️  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Create a progress bar for operations
        /// </summary>
        public static async Task ShowProgressAsync(string operation, int steps = 10)
        {
            Console.Write($"{operation} [");
            var progressBar = new string('─', 20);
            Console.Write(progressBar);
            Console.Write("]");
            
            var startLeft = Console.CursorLeft - 21;
            
            for (int i = 0; i <= steps; i++)
            {
                var filled = (int)((double)i / steps * 20);
                Console.SetCursorPosition(startLeft + 1, Console.CursorTop);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(new string('█', filled));
                Console.ResetColor();
                
                await Task.Delay(200);
            }
            
            Console.SetCursorPosition(startLeft + 22, Console.CursorTop);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ✅");
            Console.ResetColor();
        }

        /// <summary>
        /// Display a centered boxed title
        /// </summary>
        private static void WriteBoxedTitle(string title, ConsoleColor color)
        {
            var width = Math.Max(title.Length + 4, 50);
            var padding = (width - title.Length) / 2;
            
            Console.ForegroundColor = color;
            Console.WriteLine(TopLeft + new string(Horizontal.ToCharArray()[0], width - 2) + TopRight);
            Console.WriteLine(Vertical + new string(' ', padding - 1) + title + new string(' ', width - title.Length - padding - 1) + Vertical);
            Console.WriteLine(BottomLeft + new string(Horizontal.ToCharArray()[0], width - 2) + BottomRight);
            Console.ResetColor();
        }

        /// <summary>
        /// Write centered text
        /// </summary>
        private static void WriteCentered(string text, ConsoleColor color)
        {
            var padding = (Console.WindowWidth - text.Length) / 2;
            Console.ForegroundColor = color;
            Console.WriteLine(new string(' ', Math.Max(0, padding)) + text);
            Console.ResetColor();
        }

        /// <summary>
        /// Write indented text with color
        /// </summary>
        private static void WriteIndented(string text, int indentLevel, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(new string(' ', indentLevel * 2) + text);
            Console.ResetColor();
        }

        /// <summary>
        /// Write colored line
        /// </summary>
        private static void WriteColoredLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        /// <summary>
        /// Get user-specific color based on age group and role
        /// </summary>
        private static ConsoleColor GetUserColor(string ageGroup, string role)
        {
            return role switch
            {
                "Parent" => ConsoleColor.Blue,
                _ => ageGroup switch
                {
                    "Teen" => ConsoleColor.Cyan,
                    "Elementary" => ConsoleColor.Green,
                    "Child" => ConsoleColor.Yellow,
                    _ => ConsoleColor.White
                }
            };
        }

        /// <summary>
        /// Get user-specific icon based on age group and role
        /// </summary>
        private static string GetUserIcon(string ageGroup, string role)
        {
            return role switch
            {
                "Parent" => "👨‍👩‍👧‍👦",
                _ => ageGroup switch
                {
                    "Teen" => "🧑‍💻",
                    "Elementary" => "🧒",
                    "Child" => "👶",
                    _ => "👤"
                }
            };
        }

        /// <summary>
        /// Create an interactive confirmation dialog
        /// </summary>
        public static bool ShowConfirmation(string message, string confirmText = "Yes", string cancelText = "No")
        {
            Console.WriteLine();
            WriteColoredLine($"❓ {message}", ConsoleColor.Yellow);
            WriteColoredLine($"   Press [Y] for {confirmText}, [N] for {cancelText}", ConsoleColor.Gray);
            Console.Write("   Your choice: ");
            
            while (true)
            {
                var key = Console.ReadKey(true);
                
                if (key.Key == ConsoleKey.Y)
                {
                    Console.WriteLine("Y");
                    return true;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    Console.WriteLine("N");
                    return false;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return false; // Default to No
                }
            }
        }

        /// <summary>
        /// Display family members with enhanced formatting
        /// </summary>
        public static void ShowFamilyMembers(IEnumerable<dynamic> members)
        {
            Console.Clear();
            WriteBoxedTitle("👨‍👩‍👧‍👦 Family Members", ConsoleColor.Blue);
            WriteColoredLine("", ConsoleColor.White);

            foreach (var member in members)
            {
                var userColor = GetUserColor(member.AgeGroup?.ToString() ?? "Child", member.Role?.ToString() ?? "Child");
                var userIcon = GetUserIcon(member.AgeGroup?.ToString() ?? "Child", member.Role?.ToString() ?? "Child");
                
                WriteColoredLine($"╭─ {userIcon} {member.DisplayName ?? member.Username} ────────────", userColor);
                WriteColoredLine($"│ 👤 Username: {member.Username}", userColor);
                WriteColoredLine($"│ 🎭 Age Group: {member.AgeGroup}", userColor);
                WriteColoredLine($"│ 🎯 Role: {member.Role}", userColor);
                WriteColoredLine($"│ 🛡️ Filter Level: {member.FilterLevel ?? "Standard"}", userColor);
                WriteColoredLine($"│ ⏰ Screen Time Limit: {member.DailyScreenTimeLimit ?? 480} minutes", userColor);
                WriteColoredLine($"│ 🕒 Last Login: {member.LastLoginTime:yyyy-MM-dd HH:mm}", userColor);
                WriteColoredLine("╰────────────────────────────────────────", userColor);
                WriteColoredLine("", ConsoleColor.White);
            }
        }

        /// <summary>
        /// Wait for user input with styled prompt
        /// </summary>
        public static void WaitForInput(string prompt = "Press any key to continue...")
        {
            WriteColoredLine($"   {prompt}", ConsoleColor.Gray);
            Console.ReadKey(true);
        }

        /// <summary>
        /// Get styled user input
        /// </summary>
        public static string GetInput(string prompt, bool isPassword = false)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{prompt}: ");
            Console.ResetColor();
            
            if (isPassword)
            {
                return ReadPassword();
            }
            else
            {
                return Console.ReadLine()?.Trim() ?? "";
            }
        }

        /// <summary>
        /// Read password with asterisk masking
        /// </summary>
        private static string ReadPassword()
        {
            var password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }
    }
}
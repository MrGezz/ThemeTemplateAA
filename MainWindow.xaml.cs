using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace AutoThemerApp
{
    public partial class MainWindow : Window
    {
        private bool isDark = false;

        // Theme Definitions for the Exe itself
        private readonly Dictionary<string, Dictionary<string, string>> THEMES = new Dictionary<string, Dictionary<string, string>>
        {
            { "light", new Dictionary<string, string> {
                { "WindowBg", "#F5F5F5" }, { "TextMain", "#333333" }, { "TextSecondary", "#00529B" },
                { "WarningRed", "#D32F2F" }, { "BorderBrush", "#CCCCCC" }, { "InputBg", "#FFFFFF" }, { "InputText", "#000000" }
            }},
            { "dark", new Dictionary<string, string> {
                { "WindowBg", "#2D2D2D" }, { "TextMain", "#EEEEEE" }, { "TextSecondary", "#64B5F6" },
                { "WarningRed", "#FF5252" }, { "BorderBrush", "#444444" }, { "InputBg", "#1E1E1E" }, { "InputText", "#FFFFFF" }
            }}
        };

        // Code to inject into the Python scripts
        private const string NEW_THEME_DICT = @"# -------------------------------------------------------------
# THEME CONFIGURATION
# -------------------------------------------------------------
def get_brush(hex_code):
    from System.Windows.Media import ColorConverter, SolidColorBrush
    color = ColorConverter.ConvertFromString(hex_code)
    return SolidColorBrush(color)

THEMES = {
    ""light"": {
        ""WindowBg"": ""#F5F5F5"", ""TextMain"": ""#333333"", ""TextSecondary"": ""#00529B"", 
        ""WarningRed"": ""#D32F2F"", ""BorderBrush"": ""#CCCCCC"", ""InputBg"": ""#FFFFFF"", ""InputText"": ""#000000""
    },
    ""dark"": {
        ""WindowBg"": ""#2D2D2D"", ""TextMain"": ""#EEEEEE"", ""TextSecondary"": ""#64B5F6"", 
        ""WarningRed"": ""#FF5252"", ""BorderBrush"": ""#444444"", ""InputBg"": ""#1E1E1E"", ""InputText"": ""#FFFFFF""
    }
}
";

        private const string NEW_CLASS_METHODS = @"
    # --- AUTOMATIC THEME METHODS ---
    def apply_theme(self, theme_name):
        theme_data = THEMES[theme_name]
        for key, hex_value in theme_data.items():
            self.Resources[key] = get_brush(hex_value)

    def ToggleTheme(self, sender, args):
        self.is_dark = not getattr(self, 'is_dark', False)
        if self.is_dark:
            self.apply_theme(""dark"")
            sender.Content = ""☀️ Light Mode""
        else:
            self.apply_theme(""light"")
            sender.Content = ""🌑 Dark Mode""
";

        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme("light");
        }

        // --- APP THEME ENGINE ---
        private void ApplyTheme(string themeName)
        {
            var themeData = THEMES[themeName];
            var converter = new BrushConverter();
            foreach (var kvp in themeData)
            {
                this.Resources[kvp.Key] = (SolidColorBrush)converter.ConvertFromString(kvp.Value);
            }
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDark = !isDark;
            ApplyTheme(isDark ? "dark" : "light");
            btnTheme.Content = isDark ? "☀️ Light Mode" : "🌑 Dark Mode";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- PATCH ENGINE LOGIC ---
        private void Patch_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select a .pushbutton folder to patch";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string targetDir = fbd.SelectedPath;
                    string pyPath = Path.Combine(targetDir, "script.py");
                    string xamlPath = Path.Combine(targetDir, "ui.xaml");

                    if (!File.Exists(pyPath) || !File.Exists(xamlPath))
                    {
                        MessageBox.Show("Folder must contain both script.py and ui.xaml", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        // Create Backups
                        File.Copy(pyPath, pyPath + ".bak", true);
                        File.Copy(xamlPath, xamlPath + ".bak", true);

                        PatchXaml(xamlPath);
                        PatchPython(pyPath);

                        lblStatus.Text = "✅ Successfully patched: " + new DirectoryInfo(targetDir).Name;
                        lblStatus.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to patch:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void PatchXaml(string path)
        {
            string content = File.ReadAllText(path);

            // 1. Clean up <Window> tag to prevent duplicate attributes
            content = Regex.Replace(content, @"(<Window\b[^>]*?)\s+Background=""[^""]*""", "$1", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"(<Window\b[^>]*?)\s+Foreground=""[^""]*""", "$1", RegexOptions.IgnoreCase);

            // 2. Safely add the Dynamic Background back to the <Window> tag
            if (!content.Contains("Background=\"{DynamicResource WindowBg}\""))
            {
                var regex = new Regex(@"<Window ");
                content = regex.Replace(content, "<Window Background=\"{DynamicResource WindowBg}\" ", 1);
            }

            // 3. AUTOMATICALLY INJECT THE DARK MODE BUTTON
            // We look for the first <StackPanel> or <Grid> and inject a DockPanel with the button right after it.
            if (!content.Contains("x:Name=\"btnTheme\""))
            {
                string buttonXaml = "\n                <!-- INJECTED BY AUTOTHEMER -->\n" +
                                    "                <DockPanel Margin=\"0,0,0,15\">\n" +
                                    "                    <Button x:Name=\"btnTheme\" Content=\"☀️ Light Mode\" HorizontalAlignment=\"Right\" Width=\"100\" Click=\"ToggleTheme\" Background=\"Transparent\" Foreground=\"{DynamicResource TextMain}\" BorderThickness=\"0\" Cursor=\"Hand\"/>\n" +
                                    "                </DockPanel>\n";

                var containerRegex = new Regex(@"<(StackPanel|Grid)[^>]*>");
                if (containerRegex.IsMatch(content))
                {
                    content = containerRegex.Replace(content, "$0" + buttonXaml, 1);
                }
            }

            // 4. Replace Hardcoded Colors (RegexOptions.IgnoreCase makes it case insensitive)
            var colorMap = new Dictionary<string, string>
            {
                {@"=""#F5F5F5""", @"=""{DynamicResource WindowBg}"""},
                {@"=""#2D2D2D""", @"=""{DynamicResource WindowBg}"""},
                {@"=""#2D2D30""", @"=""{DynamicResource WindowBg}"""}, // Specific to your MEP tool
                {@"=""#333333""", @"=""{DynamicResource TextMain}"""},
                {@"=""#EEEEEE""", @"=""{DynamicResource TextMain}"""},
                {@"=""White""",   @"=""{DynamicResource TextMain}"""},
                {@"=""Black""",   @"=""{DynamicResource TextMain}"""},
                {@"=""Gray""",    @"=""{DynamicResource TextSecondary}"""},
                {@"=""#00529B""", @"=""{DynamicResource TextSecondary}"""},
                {@"=""#64B5F6""", @"=""{DynamicResource TextSecondary}"""},
                {@"=""#D32F2F""", @"=""{DynamicResource WarningRed}"""},
                {@"=""#FF5252""", @"=""{DynamicResource WarningRed}"""},
                {@"=""#CCCCCC""", @"=""{DynamicResource BorderBrush}"""},
                {@"=""#444444""", @"=""{DynamicResource BorderBrush}"""},
                {@"=""#FFFFFF""", @"=""{DynamicResource InputBg}"""},
                {@"=""#1E1E1E""", @"=""{DynamicResource InputBg}"""},
                {@"=""#000000""", @"=""{DynamicResource InputText}"""}
            };

            foreach (var kvp in colorMap)
            {
                content = Regex.Replace(content, kvp.Key, kvp.Value, RegexOptions.IgnoreCase);
            }

            File.WriteAllText(path, content);
        }

        private void PatchPython(string path)
        {
            string content = File.ReadAllText(path);

            // 1. STRIP OLD THEME DATA
            content = Regex.Replace(content, @"THEMES\s*=\s*\{.*?\n\}\n", "", RegexOptions.Singleline);
            content = Regex.Replace(content, @"def get_brush\(hex_code\):.*?return SolidColorBrush\(color\)\n", "", RegexOptions.Singleline);
            content = Regex.Replace(content, @"\s+def apply_theme\(self, theme_name\):.*?(?=\n\s+def |\Z)", "", RegexOptions.Singleline);
            content = Regex.Replace(content, @"\s+def ToggleTheme\(self, sender, args\):.*?(?=\n\s+def |\Z)", "", RegexOptions.Singleline);
            content = content.Replace("    # --- AUTOMATIC THEME METHODS ---", "");

            // 2. PROTECT ENCODING DECLARATION
            string codingStr = "# -*- coding: utf-8 -*-";
            if (content.Contains(codingStr))
            {
                content = content.Replace(codingStr, "");
            }

            // 3. INJECT NEW GLOBALS (Above the first class)
            var classMatch = Regex.Match(content, @"^class\s+\w+\(", RegexOptions.Multiline);
            if (classMatch.Success)
            {
                content = content.Insert(classMatch.Index, NEW_THEME_DICT + "\n\n");
            }
            else
            {
                content = NEW_THEME_DICT + "\n\n" + content;
            }

            // Put encoding back at the absolute top so Python doesn't crash
            content = codingStr + "\n" + content.TrimStart();

            // 4. INJECT THEME INIT (Bulletproof pyRevit WPF catch)
            string wpfInitCall = @"forms\.WPFWindow\.__init__\(self, xaml_file_name\)";
            if (!content.Contains("self.apply_theme("))
            {
                // Defaulting to "dark" so tools like your MEP script start out looking correct
                content = Regex.Replace(content, wpfInitCall, "$0\n        self.is_dark = True\n        self.apply_theme(\"dark\")");
            }

            // 5. INJECT CLASS METHODS (Strictly into the WPF class)
            if (!content.Contains("def apply_theme"))
            {
                // Find exactly where the WPF window initializes
                int wpfInitIndex = content.IndexOf("forms.WPFWindow.__init__");
                if (wpfInitIndex != -1)
                {
                    // Find the very next method definition inside the WPF class
                    var nextDefMatch = Regex.Match(content.Substring(wpfInitIndex), @"\n    def ");
                    if (nextDefMatch.Success)
                    {
                        int insertPos = wpfInitIndex + nextDefMatch.Index;
                        content = content.Insert(insertPos, "\n" + NEW_CLASS_METHODS + "\n");
                    }
                    else
                    {
                        content += "\n" + NEW_CLASS_METHODS + "\n";
                    }
                }
            }

            File.WriteAllText(path, content);
        }
    }
}
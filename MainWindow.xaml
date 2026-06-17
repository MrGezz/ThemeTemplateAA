using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace AutoThemerApp
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Data Models
    // ═══════════════════════════════════════════════════════════════════════════

    public class ControlDef
    {
        public string Name     { get; set; }
        public string XamlType { get; set; }
        public string Label    { get; set; }
    }

    public class ScriptAnalysis
    {
        /// <summary>wpf_with_xaml | wpf_no_xaml | swf_only | no_ui</summary>
        public string Mode           { get; set; }
        public bool   HasWpfClass    { get; set; }
        public string WpfClassName   { get; set; }
        public string XamlFileName   { get; set; } = "ui.xaml";
        public string SuggestedTitle { get; set; }
        public bool   HasSWFCode     { get; set; }   // System.Windows.Forms present
        public List<ControlDef> Controls      { get; set; } = new List<ControlDef>();
        public List<string>     EventHandlers { get; set; } = new List<string>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Main Window
    // ═══════════════════════════════════════════════════════════════════════════
    public partial class MainWindow : Window
    {
        private bool           isDark     = false;
        private string         _targetDir = null;
        private ScriptAnalysis _analysis  = null;

        // ── Theme definitions for the patcher app's own UI ────────────────────
        private readonly Dictionary<string, Dictionary<string, string>> THEMES =
            new Dictionary<string, Dictionary<string, string>>
        {
            { "light", new Dictionary<string, string> {
                { "WindowBg","#F5F5F5" },{ "TextMain","#333333" },{ "TextSecondary","#00529B" },
                { "WarningRed","#D32F2F" },{ "BorderBrush","#CCCCCC" },{ "InputBg","#FFFFFF" },{ "InputText","#000000" }
            }},
            { "dark", new Dictionary<string, string> {
                { "WindowBg","#2D2D2D" },{ "TextMain","#EEEEEE" },{ "TextSecondary","#64B5F6" },
                { "WarningRed","#FF5252" },{ "BorderBrush","#444444" },{ "InputBg","#1E1E1E" },{ "InputText","#FFFFFF" }
            }}
        };

        // ── Python code injected into target scripts ──────────────────────────
        private const string INJECT_THEME_DICT = @"# ---------------------------------------------------------------
# THEME CONFIGURATION  (injected by AutoThemer)
# ---------------------------------------------------------------
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

        private const string INJECT_THEME_METHODS = @"
    # --- AUTOMATIC THEME METHODS  (injected by AutoThemer) ---
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

        // ── Control name-prefix → XAML element mapping ────────────────────────
        private static readonly Dictionary<string, string> PREFIX_TO_XAML =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cb_",   "CheckBox"    }, { "chk_",  "CheckBox"    },
            { "cbo_",  "ComboBox"    }, { "cmb_",  "ComboBox"    }, { "ddl_",  "ComboBox"    },
            { "txt_",  "TextBox"     }, { "tbx_",  "TextBox"     }, { "num_",  "TextBox"     },
            { "btn_",  "Button"      },
            { "lbl_",  "TextBlock"   },
            { "lst_",  "ListBox"     },
            { "dgr_",  "DataGrid"    },
            { "rdv_",  "RadioButton" }, { "rdo_",  "RadioButton" },
            { "sldr_", "Slider"      },
            { "dp_",   "DatePicker"  },
            { "img_",  "Image"       },
        };

        // ─────────────────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme("light");
        }

        // ═════════════════════════════════════════════════════════════════════
        // PATCHER APP — THEME ENGINE
        // ═════════════════════════════════════════════════════════════════════
        private void ApplyTheme(string name)
        {
            var conv = new BrushConverter();
            foreach (var kv in THEMES[name])
                this.Resources[kv.Key] = (SolidColorBrush)conv.ConvertFromString(kv.Value);
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDark = !isDark;
            ApplyTheme(isDark ? "dark" : "light");
            btnTheme.Content = isDark ? "☀️ Light Mode" : "🌑 Dark Mode";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        // ═════════════════════════════════════════════════════════════════════
        // STEP 1 — ANALYZE FOLDER
        // ═════════════════════════════════════════════════════════════════════
        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select a .pushbutton folder to analyze";
                if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                _targetDir = fbd.SelectedPath;
                string pyPath = Path.Combine(_targetDir, "script.py");

                if (!File.Exists(pyPath))
                {
                    lblMode.Text       = "❌  No script.py found in selected folder.";
                    txtLog.Text        = "Expected:\n" + pyPath;
                    btnPatch.IsEnabled = false;
                    return;
                }

                try
                {
                    _analysis = AnalyzePythonScript(pyPath);
                    RenderAnalysis(_analysis);
                }
                catch (Exception ex)
                {
                    lblMode.Text       = "❌  Analysis failed — see log.";
                    txtLog.Text        = ex.ToString();
                    btnPatch.IsEnabled = false;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PYTHON SCRIPT ANALYZER
        // ─────────────────────────────────────────────────────────────────────
        private ScriptAnalysis AnalyzePythonScript(string pyPath)
        {
            string src  = File.ReadAllText(pyPath);
            var    info = new ScriptAnalysis();

            // 1. Detect System.Windows.Forms usage
            info.HasSWFCode = src.Contains("System.Windows.Forms") ||
                              src.Contains("from System.Windows.Forms");

            // 2. Find the forms.WPFWindow subclass
            var wpfClass = Regex.Match(src, @"class\s+(\w+)\s*\(\s*forms\.WPFWindow\s*\)");
            if (!wpfClass.Success)
            {
                info.Mode = info.HasSWFCode ? "swf_only" : "no_ui";
                return info;
            }

            info.HasWpfClass  = true;
            info.WpfClassName = wpfClass.Groups[1].Value;

            // 3. Resolve XAML filename — check __init__ first, then the call site at the bottom
            var initCall = Regex.Match(src,
                @"WPFWindow\.__init__\s*\(\s*self\s*,\s*[""']([^""']+)[""']");
            if (initCall.Success)
                info.XamlFileName = initCall.Groups[1].Value;

            var callSite = Regex.Match(src,
                info.WpfClassName + @"\s*\(\s*[""']([^""']+)[""']\s*\)");
            if (callSite.Success)
                info.XamlFileName = callSite.Groups[1].Value;   // call-site wins (most explicit)

            // 4. Work inside the class body only
            string body = src.Substring(wpfClass.Index);

            // 5. Detect named controls via self.xxx references
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // These are Python instance attributes that look like controls but aren't XAML elements
            var skipAttr = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "is_dark","success","final_path","buffer_value",
                "Resources","Close","Title","Text","Content"
            };

            foreach (Match m in Regex.Matches(body, @"self\.([a-zA-Z_]\w*)"))
            {
                string name = m.Groups[1].Value;
                if (skipAttr.Contains(name) || seen.Contains(name)) continue;

                foreach (var kv in PREFIX_TO_XAML)
                {
                    if (name.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        seen.Add(name);
                        info.Controls.Add(new ControlDef
                        {
                            Name     = name,
                            XamlType = kv.Value,
                            Label    = PrefixToLabel(name, kv.Key)
                        });
                        break;
                    }
                }
            }

            // 6. Detect event handlers — def Name(self, sender, args/e):
            foreach (Match m in Regex.Matches(body,
                @"def\s+(\w+)\s*\(\s*self\s*,\s*sender\s*,\s*(?:args|e)\s*\)"))
            {
                string name = m.Groups[1].Value;
                if (name != "__init__" && !info.EventHandlers.Contains(name))
                    info.EventHandlers.Add(name);
            }

            // 7. Derive window title (XAML attribute → class name → folder name)
            var titleMatch = Regex.Match(src, @"Title\s*=\s*[""']([^""']+)[""']");
            if (titleMatch.Success)
            {
                info.SuggestedTitle = titleMatch.Groups[1].Value;
            }
            else
            {
                string bare = Regex.Replace(info.WpfClassName, @"(Window|Form|Dialog|Panel)$", "");
                info.SuggestedTitle = HumanizePascal(bare);
            }

            if (string.IsNullOrWhiteSpace(info.SuggestedTitle))
            {
                string folder = new DirectoryInfo(Path.GetDirectoryName(pyPath)).Name;
                info.SuggestedTitle = HumanizePascal(folder.Replace(".pushbutton", "").Replace("_", ""));
            }

            // 8. Determine operating mode
            bool xamlExists = File.Exists(
                Path.Combine(Path.GetDirectoryName(pyPath), info.XamlFileName));

            info.Mode = (info.HasWpfClass && xamlExists) ? "wpf_with_xaml" : "wpf_no_xaml";
            return info;
        }

        // ─────────────────────────────────────────────────────────────────────
        // RENDER ANALYSIS INTO LOG PANEL
        // ─────────────────────────────────────────────────────────────────────
        private void RenderAnalysis(ScriptAnalysis info)
        {
            var sb = new StringBuilder();

            switch (info.Mode)
            {
                case "wpf_with_xaml":
                    lblMode.Text       = "✅  WPF class + " + info.XamlFileName + " found — ready to patch";
                    btnPatch.IsEnabled = true;
                    btnPatch.Content   = "PATCH THEME";
                    sb.AppendLine("MODE  : PATCH EXISTING XAML");
                    sb.AppendLine("CLASS : " + info.WpfClassName);
                    sb.AppendLine("XAML  : " + info.XamlFileName + "  ✓ exists");
                    if (info.HasSWFCode)
                        sb.AppendLine("NOTE  : Also uses System.Windows.Forms (e.g. ThemePopup) — those classes are not converted");
                    break;

                case "wpf_no_xaml":
                    lblMode.Text       = "⚠️   WPF class found, " + info.XamlFileName + " MISSING — will generate scaffold";
                    btnPatch.IsEnabled = true;
                    btnPatch.Content   = "GENERATE + PATCH";
                    sb.AppendLine("MODE  : GENERATE SCAFFOLD → PATCH");
                    sb.AppendLine("CLASS : " + info.WpfClassName);
                    sb.AppendLine("XAML  : " + info.XamlFileName + "  ✗ missing  (will be created)");
                    sb.AppendLine("TITLE : \"" + info.SuggestedTitle + "\"");
                    if (info.HasSWFCode)
                        sb.AppendLine("NOTE  : Also uses System.Windows.Forms — those classes are left as-is");
                    break;

                case "swf_only":
                    lblMode.Text       = "⚡  System.Windows.Forms only — cannot auto-generate";
                    btnPatch.IsEnabled = false;
                    btnPatch.Content   = "NOT SUPPORTED";
                    sb.AppendLine("MODE  : UNSUPPORTED");
                    sb.AppendLine("This script builds its UI in code with System.Windows.Forms.");
                    sb.AppendLine("Auto-conversion to WPF / XAML is not yet implemented.");
                    sb.AppendLine("");
                    sb.AppendLine("To enable theming manually:");
                    sb.AppendLine("  1. Wrap your UI logic in a class inheriting forms.WPFWindow");
                    sb.AppendLine("  2. Create ui.xaml alongside script.py");
                    sb.AppendLine("  3. Re-run this tool — it will generate + patch from there");
                    break;

                default: // no_ui
                    lblMode.Text       = "❔  No UI detected in this script";
                    btnPatch.IsEnabled = false;
                    btnPatch.Content   = "NO UI FOUND";
                    sb.AppendLine("MODE  : NO UI FOUND");
                    sb.AppendLine("Neither forms.WPFWindow nor System.Windows.Forms UI was detected.");
                    break;
            }

            if (info.Controls.Count > 0)
            {
                sb.AppendLine("");
                sb.AppendLine($"── Controls detected ({info.Controls.Count}) ─────────────────────");
                foreach (var c in info.Controls)
                    sb.AppendLine($"  {c.XamlType,-14} x:Name=\"{c.Name}\"");
            }

            if (info.EventHandlers.Count > 0)
            {
                sb.AppendLine("");
                sb.AppendLine($"── Event handlers ({info.EventHandlers.Count}) ──────────────────────");
                foreach (var h in info.EventHandlers)
                    sb.AppendLine($"  Click=\"{h}\"");
            }

            txtLog.Text    = sb.ToString().TrimEnd();
            lblStatus.Text = btnPatch.IsEnabled
                ? "Analysis complete. Click the green button to proceed."
                : "Analysis complete.";
            lblStatus.Foreground = (SolidColorBrush)this.Resources["TextMain"];
        }

        // ═════════════════════════════════════════════════════════════════════
        // STEP 2 — PATCH / GENERATE
        // ═════════════════════════════════════════════════════════════════════
        private void Patch_Click(object sender, RoutedEventArgs e)
        {
            if (_targetDir == null || _analysis == null)
            {
                MessageBox.Show("Click ANALYZE FOLDER first.", "Step required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string pyPath   = Path.Combine(_targetDir, "script.py");
            string xamlPath = Path.Combine(_targetDir, _analysis.XamlFileName);

            try
            {
                File.Copy(pyPath, pyPath + ".bak", overwrite: true);

                // Generate scaffold XAML if the file is missing
                if (_analysis.Mode == "wpf_no_xaml")
                {
                    string scaffold = GenerateScaffoldXaml(_analysis);
                    File.WriteAllText(xamlPath, scaffold, Encoding.UTF8);
                    LogLine("\n✅ Generated: " + _analysis.XamlFileName);
                    LogLine("\n   NOTE: Scaffold is a starting point — review and adjust layout as needed.");
                }

                // Patch XAML + Python
                File.Copy(xamlPath, xamlPath + ".bak", overwrite: true);
                PatchXaml(xamlPath);
                PatchPython(pyPath);

                lblStatus.Text       = "✅  Done — " + new DirectoryInfo(_targetDir).Name;
                lblStatus.Foreground = new SolidColorBrush(Colors.Green);
                LogLine("\n\n✅ Patch complete. Original files saved as .bak");
            }
            catch (Exception ex)
            {
                lblStatus.Text       = "❌  Failed — see error dialog.";
                lblStatus.Foreground = new SolidColorBrush(Colors.Red);
                MessageBox.Show("Error:\n" + ex.Message, "Patch Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogLine(string msg)
        {
            txtLog.AppendText(msg);
            txtLog.ScrollToEnd();
        }

        // ═════════════════════════════════════════════════════════════════════
        // XAML SCAFFOLD GENERATOR
        // Generates a themed ui.xaml from the detected controls + handlers.
        // The output is a working starting point — the developer should review
        // layout groupings after generation.
        // ═════════════════════════════════════════════════════════════════════
        private string GenerateScaffoldXaml(ScriptAnalysis info)
        {
            var sb       = new StringBuilder();
            var rendered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Classify controls
            var buttons    = info.Controls.Where(c => c.XamlType == "Button").ToList();
            var checkboxes = info.Controls.Where(c => c.XamlType == "CheckBox").ToList();
            var inputs     = info.Controls.Where(c => c.XamlType != "Button" && c.XamlType != "CheckBox").ToList();

            // Classify handlers
            var browseHandlers = info.EventHandlers.Where(IsBrowseHandler).ToList();
            var actionHandlers = info.EventHandlers
                .Where(h => !IsBrowseHandler(h) && !IsThemeHandler(h))
                .ToList();

            // ── Window header ─────────────────────────────────────────────────
            sb.AppendLine("<Window Background=\"{DynamicResource WindowBg}\"");
            sb.AppendLine("        xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
            sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            sb.AppendLine($"        Title=\"{XmlEsc(info.SuggestedTitle)}\"");
            sb.AppendLine("        Height=\"600\" Width=\"460\"");
            sb.AppendLine("        WindowStartupLocation=\"CenterScreen\">");
            sb.AppendLine("    <ScrollViewer VerticalScrollBarVisibility=\"Auto\">");
            sb.AppendLine("        <Border Padding=\"20\">");
            sb.AppendLine("            <StackPanel>");
            sb.AppendLine();

            // ── Theme toggle (always present) ─────────────────────────────────
            sb.AppendLine("                <!-- Theme toggle — injected by AutoThemer -->");
            sb.AppendLine("                <DockPanel Margin=\"0,0,0,15\">");
            sb.AppendLine("                    <Button x:Name=\"btnTheme\" Content=\"🌑 Dark Mode\"");
            sb.AppendLine("                            HorizontalAlignment=\"Right\" Width=\"110\"");
            sb.AppendLine("                            Click=\"ToggleTheme\" Background=\"Transparent\"");
            sb.AppendLine("                            Foreground=\"{DynamicResource TextMain}\"");
            sb.AppendLine("                            BorderThickness=\"0\" Cursor=\"Hand\"/>");
            sb.AppendLine("                </DockPanel>");
            sb.AppendLine();

            // ── Browse-button + TextBox pairs ─────────────────────────────────
            // Case A: explicit btn_ control exists AND is a browse-type button
            foreach (var btn in buttons.Where(b => IsBrowseButton(b.Name)).ToList())
            {
                var paired = inputs.FirstOrDefault(c =>
                    !rendered.Contains(c.Name) &&
                    c.XamlType == "TextBox" && IsPathControl(c.Name));

                EmitBrowsePair(sb, btn.Name, paired, MatchHandler(btn.Name, info.EventHandlers));
                rendered.Add(btn.Name);
                if (paired != null) rendered.Add(paired.Name);
            }

            // Case B: there's a browse handler but no explicit btn_ for it
            foreach (var handler in browseHandlers)
            {
                // Skip if a btn_ already claimed this handler
                bool alreadyClaimed = buttons.Any(b =>
                    rendered.Contains(b.Name) && MatchHandler(b.Name, info.EventHandlers) == handler);
                if (alreadyClaimed) continue;

                var paired = inputs.FirstOrDefault(c =>
                    !rendered.Contains(c.Name) &&
                    c.XamlType == "TextBox" && IsPathControl(c.Name));

                string btnName = "btn_" + Regex.Replace(handler, @"([A-Z])", "_$1")
                                                .TrimStart('_').ToLower();
                EmitBrowsePair(sb, btnName, paired, handler);
                if (paired != null) rendered.Add(paired.Name);
                // Note: btnName is synthetic (not in info.Controls) so skip adding to rendered
            }

            // ── Remaining input controls (ComboBox, TextBox, etc.) ────────────
            foreach (var ctrl in inputs.Where(c => !rendered.Contains(c.Name)))
            {
                sb.AppendLine($"                <TextBlock Text=\"{ctrl.Label}:\"");
                sb.AppendLine("                           Foreground=\"{DynamicResource TextSecondary}\"");
                sb.AppendLine("                           Margin=\"0,8,0,2\"/>");
                EmitInputControl(sb, ctrl);
                rendered.Add(ctrl.Name);
            }

            // ── CheckBoxes grouped in a UniformGrid ───────────────────────────
            if (checkboxes.Count > 0)
            {
                int cols = checkboxes.Count > 3 ? 2 : 1;
                sb.AppendLine();
                sb.AppendLine($"                <UniformGrid Columns=\"{cols}\" Margin=\"0,10,0,15\">");
                foreach (var cb in checkboxes)
                {
                    sb.AppendLine($"                    <CheckBox x:Name=\"{cb.Name}\" Content=\"{cb.Label}\"");
                    sb.AppendLine("                              Foreground=\"{DynamicResource TextMain}\" Margin=\"0,4\"/>");
                    rendered.Add(cb.Name);
                }
                sb.AppendLine("                </UniformGrid>");
            }

            // ── Action buttons at the bottom ──────────────────────────────────
            // Collect: remaining btn_ controls + handlers with no btn_ counterpart
            var bottomBtns = buttons
                .Where(b => !rendered.Contains(b.Name))
                .Select(b => (
                    name:    b.Name,
                    label:   b.Label,
                    handler: MatchHandler(b.Name, info.EventHandlers)
                ))
                .ToList();

            // Add handlers that weren't matched to any existing btn_ control
            var mappedHandlers = new HashSet<string>(
                bottomBtns.Select(b => b.handler).Where(h => h != null));

            foreach (var h in actionHandlers.Where(h => !mappedHandlers.Contains(h)))
            {
                string syntheticName = "btn_" + Regex.Replace(h, @"([A-Z])", "_$1")
                                                     .TrimStart('_').ToLower();
                bottomBtns.Add((name: syntheticName, label: HumanizePascal(h), handler: h));
            }

            // Sort so that run/execute-type handlers end up as the rightmost primary button
            bottomBtns = bottomBtns
                .OrderBy(b => IsRunHandler(b.handler) ? 1 : 0)
                .ToList();

            if (bottomBtns.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("                <DockPanel Margin=\"0,20,0,0\" LastChildFill=\"False\">");
                for (int i = 0; i < bottomBtns.Count; i++)
                {
                    var (name, label, handler) = bottomBtns[i];
                    bool isPrimary = i == bottomBtns.Count - 1;
                    string click  = handler != null ? $" Click=\"{handler}\"" : "";
                    string dock   = isPrimary ? "DockPanel.Dock=\"Right\"" : "DockPanel.Dock=\"Left\"";
                    string bg     = isPrimary ? "\"#2E7D32\"" : "\"{DynamicResource InputBg}\"";
                    string fg     = isPrimary ? "\"White\""   : "\"{DynamicResource TextMain}\"";
                    sb.AppendLine($"                    <Button x:Name=\"{name}\" Content=\"{label}\" {dock}");
                    sb.AppendLine($"                            Width=\"130\" Height=\"38\" FontWeight=\"Bold\"{click}");
                    sb.AppendLine($"                            Background={bg} Foreground={fg} BorderThickness=\"0\" Margin=\"0,0,8,0\"/>");
                }
                sb.AppendLine("                </DockPanel>");
            }

            sb.AppendLine();
            sb.AppendLine("            </StackPanel>");
            sb.AppendLine("        </Border>");
            sb.AppendLine("    </ScrollViewer>");
            sb.AppendLine("</Window>");

            return sb.ToString();
        }

        // ── Emit a browse-button / TextBox DockPanel pair ─────────────────────
        private void EmitBrowsePair(StringBuilder sb, string btnName,
                                    ControlDef paired, string handler)
        {
            string click = handler != null ? $" Click=\"{handler}\"" : "";
            string label = paired != null ? paired.Label : "Path";

            sb.AppendLine($"                <TextBlock Text=\"{label}:\"");
            sb.AppendLine("                           Foreground=\"{DynamicResource TextSecondary}\"");
            sb.AppendLine("                           Margin=\"0,8,0,2\"/>");
            sb.AppendLine("                <DockPanel Margin=\"0,0,0,10\">");
            sb.AppendLine($"                    <Button x:Name=\"{btnName}\" Content=\"Browse\"");
            sb.AppendLine($"                            DockPanel.Dock=\"Right\" Width=\"65\"{click}");
            sb.AppendLine("                            Background=\"{DynamicResource InputBg}\"");
            sb.AppendLine("                            Foreground=\"{DynamicResource TextMain}\"/>");
            if (paired != null)
            {
                sb.AppendLine($"                    <TextBox x:Name=\"{paired.Name}\"");
                sb.AppendLine("                             Height=\"25\" VerticalContentAlignment=\"Center\"");
                sb.AppendLine("                             Margin=\"0,0,5,0\"");
                sb.AppendLine("                             Background=\"{DynamicResource InputBg}\"");
                sb.AppendLine("                             Foreground=\"{DynamicResource InputText}\"/>");
            }
            sb.AppendLine("                </DockPanel>");
        }

        // ── Emit a single non-checkbox, non-button input control ──────────────
        private void EmitInputControl(StringBuilder sb, ControlDef ctrl)
        {
            switch (ctrl.XamlType)
            {
                case "ComboBox":
                    sb.AppendLine($"                <ComboBox x:Name=\"{ctrl.Name}\" Height=\"25\" Margin=\"0,0,0,5\"/>");
                    break;
                case "TextBox":
                    sb.AppendLine($"                <TextBox x:Name=\"{ctrl.Name}\" Height=\"25\" Margin=\"0,0,0,5\"");
                    sb.AppendLine("                         VerticalContentAlignment=\"Center\"");
                    sb.AppendLine("                         Background=\"{DynamicResource InputBg}\"");
                    sb.AppendLine("                         Foreground=\"{DynamicResource InputText}\"/>");
                    break;
                case "ListBox":
                    sb.AppendLine($"                <ListBox x:Name=\"{ctrl.Name}\" Height=\"120\" Margin=\"0,0,0,5\"/>");
                    break;
                case "DataGrid":
                    sb.AppendLine($"                <DataGrid x:Name=\"{ctrl.Name}\" Height=\"150\" Margin=\"0,0,0,5\"");
                    sb.AppendLine("                          AutoGenerateColumns=\"False\" CanUserAddRows=\"False\"/>");
                    break;
                case "Slider":
                    sb.AppendLine($"                <Slider x:Name=\"{ctrl.Name}\" Minimum=\"0\" Maximum=\"100\" Value=\"50\"");
                    sb.AppendLine("                        Margin=\"0,0,0,10\"/>");
                    break;
                case "DatePicker":
                    sb.AppendLine($"                <DatePicker x:Name=\"{ctrl.Name}\" Height=\"25\" Margin=\"0,0,0,5\"/>");
                    break;
                case "RadioButton":
                    sb.AppendLine($"                <RadioButton x:Name=\"{ctrl.Name}\" Content=\"{ctrl.Label}\"");
                    sb.AppendLine("                             Foreground=\"{DynamicResource TextMain}\" Margin=\"0,4\"/>");
                    break;
                case "TextBlock":
                    sb.AppendLine($"                <TextBlock x:Name=\"{ctrl.Name}\" Text=\"\"");
                    sb.AppendLine("                           Foreground=\"{DynamicResource TextMain}\" Margin=\"0,0,0,5\"/>");
                    break;
                default:
                    sb.AppendLine($"                <!-- TODO: Unknown control type for '{ctrl.Name}' ({ctrl.XamlType}) -->");
                    break;
            }
        }

        // ── Predicates ────────────────────────────────────────────────────────
        private bool IsBrowseButton(string name)
        {
            string lo = name.ToLower();
            return lo.Contains("browse") || lo.Contains("pick") ||
                   (lo.Contains("btn") && (lo.Contains("folder") || lo.Contains("path") || lo.Contains("dir")));
        }

        private bool IsBrowseHandler(string h)
        {
            string lo = h.ToLower();
            return lo.Contains("browse") || lo.Contains("pickfolder") ||
                   lo.Contains("selectfolder") || lo.Contains("choosepath");
        }

        private bool IsThemeHandler(string h)
        {
            string lo = h.ToLower();
            return lo.Contains("theme") || lo.Contains("darkmode") || lo.Contains("toggletheme");
        }

        private bool IsPathControl(string name)
        {
            string lo = name.ToLower();
            return lo.Contains("folder") || lo.Contains("path") ||
                   lo.Contains("dir") || lo.Contains("file");
        }

        private bool IsRunHandler(string h)
        {
            if (h == null) return false;
            string lo = h.ToLower();
            return lo.Contains("run") || lo.Contains("execute") || lo.Contains("generate") ||
                   lo.Contains("process") || lo.Contains("apply") || lo.Contains("start") ||
                   lo.Contains("submit") || lo.Contains("export");
        }

        // ── Match a button name to its most likely event handler ──────────────
        private string MatchHandler(string btnName, List<string> handlers)
        {
            // Strip "btn_" prefix, then look for a handler containing that word
            string suffix = Regex.Replace(btnName, @"^btn_", "", RegexOptions.IgnoreCase)
                                 .ToLower();

            var direct = handlers.FirstOrDefault(h => h.ToLower().Contains(suffix));
            if (direct != null) return direct;

            // Semantic fallback table
            var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "run",    new[] { "run","execute","process","generate","apply","start","submit" } },
                { "go",     new[] { "run","execute","process","generate","apply","start" } },
                { "browse", new[] { "browse","folder","pick","select","path","dir" } },
                { "close",  new[] { "close","cancel","exit","dismiss" } },
                { "ok",     new[] { "ok","confirm","accept","submit","apply" } },
                { "export", new[] { "export","save","write","output" } },
                { "clear",  new[] { "clear","reset","clean","wipe" } },
                { "load",   new[] { "load","import","open","read","fetch" } },
                { "add",    new[] { "add","append","insert","create","new" } },
                { "remove", new[] { "remove","delete","del","erase" } },
                { "refresh",new[] { "refresh","reload","update","sync" } },
            };

            if (aliases.TryGetValue(suffix, out var words))
                foreach (var w in words)
                {
                    var m = handlers.FirstOrDefault(h => h.ToLower().Contains(w));
                    if (m != null) return m;
                }

            return null;
        }

        // ── String helpers ────────────────────────────────────────────────────
        /// <summary>Strip prefix, convert snake_case remainder to Title Case label.</summary>
        private string PrefixToLabel(string name, string prefix)
        {
            string bare = name.Length > prefix.Length ? name.Substring(prefix.Length) : name;
            return string.Join(" ",
                bare.Split('_')
                    .Where(p => p.Length > 0)
                    .Select(p => char.ToUpper(p[0]) + p.Substring(1)));
        }

        /// <summary>Convert PascalCase or mixed name to "Human Readable" label.</summary>
        private string HumanizePascal(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            return string.Join(" ",
                Regex.Split(name.Trim(), @"(?=[A-Z])|_")
                     .Where(p => p.Length > 0)
                     .Select(p => char.ToUpper(p[0]) + p.Substring(1)));
        }

        private string XmlEsc(string s) =>
            s?.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;") ?? "";

        // ═════════════════════════════════════════════════════════════════════
        // XAML PATCHER  (original logic, hardened)
        // ═════════════════════════════════════════════════════════════════════
        private void PatchXaml(string path)
        {
            string content = File.ReadAllText(path);

            // 1. Remove hardcoded Background/Foreground from <Window> tag
            content = Regex.Replace(content,
                @"(<Window\b[^>]*?)\s+Background=""[^""]*""", "$1", RegexOptions.IgnoreCase);
            content = Regex.Replace(content,
                @"(<Window\b[^>]*?)\s+Foreground=""[^""]*""", "$1", RegexOptions.IgnoreCase);

            // 2. Add DynamicResource Background to <Window>
            if (!content.Contains("Background=\"{DynamicResource WindowBg}\""))
                content = new Regex(@"<Window ").Replace(content,
                    "<Window Background=\"{DynamicResource WindowBg}\" ", 1);

            // 3. Inject theme-toggle button after the first container opening tag
            if (!content.Contains("x:Name=\"btnTheme\""))
            {
                string btn =
                    "\n                <!-- INJECTED BY AUTOTHEMER -->\n" +
                    "                <DockPanel Margin=\"0,0,0,15\">\n" +
                    "                    <Button x:Name=\"btnTheme\" Content=\"🌑 Dark Mode\"" +
                    " HorizontalAlignment=\"Right\" Width=\"110\"\n" +
                    "                            Click=\"ToggleTheme\" Background=\"Transparent\"" +
                    " Foreground=\"{DynamicResource TextMain}\" BorderThickness=\"0\" Cursor=\"Hand\"/>\n" +
                    "                </DockPanel>\n";

                content = new Regex(@"<(StackPanel|Grid)[^>]*>")
                    .Replace(content, "$0" + btn, 1);
            }

            // 4. Replace hardcoded color literals with DynamicResource keys
            var colorMap = new Dictionary<string, string>
            {
                { "=\"#F5F5F5\"", "=\"{DynamicResource WindowBg}\""     },
                { "=\"#2D2D2D\"", "=\"{DynamicResource WindowBg}\""     },
                { "=\"#2D2D30\"", "=\"{DynamicResource WindowBg}\""     },  // VS dark bg variant
                { "=\"#333333\"", "=\"{DynamicResource TextMain}\""     },
                { "=\"#EEEEEE\"", "=\"{DynamicResource TextMain}\""     },
                { "=\"White\"",   "=\"{DynamicResource TextMain}\""     },
                { "=\"Black\"",   "=\"{DynamicResource TextMain}\""     },
                { "=\"Gray\"",    "=\"{DynamicResource TextSecondary}\""},
                { "=\"#00529B\"", "=\"{DynamicResource TextSecondary}\""},
                { "=\"#64B5F6\"", "=\"{DynamicResource TextSecondary}\""},
                { "=\"#D32F2F\"", "=\"{DynamicResource WarningRed}\""   },
                { "=\"#FF5252\"", "=\"{DynamicResource WarningRed}\""   },
                { "=\"#CCCCCC\"", "=\"{DynamicResource BorderBrush}\""  },
                { "=\"#444444\"", "=\"{DynamicResource BorderBrush}\""  },
                { "=\"#FFFFFF\"", "=\"{DynamicResource InputBg}\""      },
                { "=\"#1E1E1E\"", "=\"{DynamicResource InputBg}\""      },
                { "=\"#000000\"", "=\"{DynamicResource InputText}\""    },
            };

            foreach (var kv in colorMap)
                content = Regex.Replace(content,
                    Regex.Escape(kv.Key), kv.Value, RegexOptions.IgnoreCase);

            File.WriteAllText(path, content);
        }

        // ═════════════════════════════════════════════════════════════════════
        // PYTHON PATCHER  (original logic, hardened)
        // ═════════════════════════════════════════════════════════════════════
        private void PatchPython(string path)
        {
            string content = File.ReadAllText(path);

            // 1. Strip previous AutoThemer injections to avoid double-injection
            content = Regex.Replace(content,
                @"# -+\r?\n# THEME CONFIGURATION.*?# -+\r?\n", "", RegexOptions.Singleline);
            content = Regex.Replace(content,
                @"THEMES\s*=\s*\{.*?\n\}\n", "", RegexOptions.Singleline);
            content = Regex.Replace(content,
                @"def get_brush\(hex_code\):.*?return SolidColorBrush\(color\)\n", "", RegexOptions.Singleline);
            content = Regex.Replace(content,
                @"\s+def apply_theme\(self,\s*theme_name\):.*?(?=\n\s+def |\Z)", "", RegexOptions.Singleline);
            content = Regex.Replace(content,
                @"\s+def ToggleTheme\(self,\s*sender,\s*args\):.*?(?=\n\s+def |\Z)", "", RegexOptions.Singleline);
            content = content
                .Replace("    # --- AUTOMATIC THEME METHODS ---", "")
                .Replace("    # --- AUTOMATIC THEME METHODS  (injected by AutoThemer) ---", "");

            // 2. Protect the encoding declaration so we can reinsert it at the top
            const string CODING = "# -*- coding: utf-8 -*-";
            content = content.Replace(CODING, "");

            // 3. Inject THEMES dict above the first class definition
            var classMatch = Regex.Match(content, @"^class\s+\w+\(", RegexOptions.Multiline);
            content = classMatch.Success
                ? content.Insert(classMatch.Index, INJECT_THEME_DICT + "\n\n")
                : INJECT_THEME_DICT + "\n\n" + content;

            // 4. Restore encoding declaration at the very top
            content = CODING + "\n" + content.TrimStart();

            // 5. Inject apply_theme call in __init__, right after WPFWindow.__init__(...)
            //    Flexible pattern — accepts any argument style (variable or literal string)
            const string WPF_INIT_RE = @"forms\.WPFWindow\.__init__\(self\s*,\s*[^)]+\)";
            if (!content.Contains("self.apply_theme("))
                content = Regex.Replace(content, WPF_INIT_RE,
                    "$0\n        self.is_dark = True\n        self.apply_theme(\"dark\")");

            // 6. Inject apply_theme + ToggleTheme methods into the WPF class body
            if (!content.Contains("def apply_theme"))
            {
                int initIdx = content.IndexOf("forms.WPFWindow.__init__", StringComparison.Ordinal);
                if (initIdx >= 0)
                {
                    // Insert just before the next method definition inside the class
                    var nextDef = Regex.Match(content.Substring(initIdx), @"\n    def ");
                    int insertAt = nextDef.Success
                        ? initIdx + nextDef.Index
                        : content.Length;
                    content = content.Insert(insertAt, "\n" + INJECT_THEME_METHODS + "\n");
                }
                else
                {
                    content += "\n" + INJECT_THEME_METHODS + "\n";
                }
            }

            File.WriteAllText(path, content);
        }
    }
}

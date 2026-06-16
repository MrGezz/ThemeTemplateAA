STILL IN ALPHA PHASE
# 🎨 PyRevit UI Auto-Themer (C#)

A standalone desktop utility designed to automates the conversion of legacy hardcoded PyRevit UIs into a dynamic, theme-swappable framework supporting **Light and Dark Modes**.

---

## 🚀 Key Features

*   **Automatic Color Mapping:** Scans `ui.xaml` files and converts hardcoded HEX colors (e.g., `#F5F5F5`, `#2D2D2D`) into `{DynamicResource}` bindings.
*   **Intelligent UI Injection:** Automatically detects the main container in your XAML (Grid or StackPanel) and injects a styled "Theme Toggle" button.
*   **Python Logic Patching:** Injects a complete theme engine into your `script.py`, including the `THEMES` dictionary, hex-to-brush converters, and class-level toggle methods.
*   **Safe Code Modification:**
    *   **Auto-Backups:** Creates `.bak` copies of your files before any changes are made.
    *   **Encoding Protection:** Specifically ensures the `# -*- coding: utf-8 -*-` line remains at the top of your Python script to prevent IronPython execution errors.
    *   **WPF-Class Targeting:** Uses advanced Regex to ensure theme methods are injected into the correct `WPFWindow` class, even if your script contains multiple helper classes.

---

## 🛠️ How it Works

The tool acts as a "Code Refactorer." When you select a pyRevit `.pushbutton` folder:

1.  **XAML Refactoring:** It cleans the `<Window>` tag of duplicate background attributes and maps every UI element to a centralized resource dictionary.
2.  **Python Injection:** It injects a localized theme engine into your script. This allows the tool to run without needing any external dependencies or shared libraries.
3.  **Dynamic Binding:** By the end of the process, your UI is no longer static; it responds instantly to the `ToggleTheme` event.

---

## 📖 Usage

1.  **Build:** Compile the project in Visual Studio (Release mode) to generate the `AutoThemerApp.exe`.
2.  **Run:** Launch the executable.
3.  **Select Folder:** Click **Select Folder & Patch** and navigate to your target `.pushbutton` folder.
4.  **Reload:** In Revit, click **PyRevit > Reload**. Your tool now features a Dark/Light mode toggle!

---

## ⚙️ Technical Specifications

*   **Platform:** Windows Standalone Executable (.exe)
*   **Language:** C# / WPF
*   **Framework:** .NET Framework 4.8 (For maximum compatibility with Revit/pyRevit environments)
*   **Logic:** Regex-based AST (Abstract Syntax Tree) simulation for Python and XAML parsing.

---

## 🎨 Theme Mappings

The tool automatically maps standard Revit/Windows colors to the following dynamic resources:

| Hex Code | Dynamic Resource | Light Mode | Dark Mode |
| :--- | :--- | :--- | :--- |
| `#F5F5F5` | `WindowBg` | Off-White | Dark Grey |
| `#333333` | `TextMain` | Dark Grey | Near-White |
| `#00529B` | `TextSecondary`| Professional Blue | Sky Blue |
| `#D32F2F` | `WarningRed` | Alert Red | Bright Coral |
| `#FFFFFF` | `InputBg` | Pure White | Charcoal |

---

## ⚠️ Disclaimer
This tool uses Regular Expressions to modify source code. While it includes safety checks and automatic backups, it is recommended to run this on a version-controlled repository (Git) or detached copies of your scripts.

---

*Standardizing BIM toolsets, one pushbutton at a time.*

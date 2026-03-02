# Keaton — History

## Core Context

**Project:** SnippingTool — WPF C# tray application  
**User:** Dimitar Radenkov  
**Stack:** C# / .NET 10 / WPF, Win32 P/Invoke, Hardcodet.Wpf.TaskbarNotification  

**Architecture:**
- `App.xaml.cs` — startup, global hotkey (PrintScreen via RegisterHotKey), tray icon, capture orchestration
- `OverlayWindow` — fullscreen transparent overlay for rubber-band selection
- `PreviewWindow` — separate annotation/preview window (DECISION: to be eliminated, merged into overlay)
- `ScreenCapture.cs` — Win32 BitBlt-based screen capture

## Learnings

### 2026-03-02: UI/UX direction decided
Dimitar wants no separate PreviewWindow. The overlay stays open after selection. Annotation tools and action bar float over the overlay. Tools: Arrow, Text, Highlight, Pen, Line, Circle. Actions: Copy + Close only. No resize handles on selection.

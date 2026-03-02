# Fenster — History

## Core Context

**Project:** SnippingTool — WPF C# tray application  
**User:** Dimitar Radenkov  
**Stack:** C# / .NET 10 / WPF, Win32 P/Invoke, Hardcodet.Wpf.TaskbarNotification  

**Key files:**
- `SnippingTool/OverlayWindow.xaml` + `.cs` — fullscreen capture overlay
- `SnippingTool/PreviewWindow.xaml` + `.cs` — annotation/preview window
- `SnippingTool/App.xaml.cs` — hotkey, tray, orchestration
- `SnippingTool/ScreenCapture.cs` — screen capture

**Patterns:**
- WPF canvas-based annotation (Lines, Polylines, Rectangles, Ellipses as UIElements)
- DPI scale via `PresentationSource.CompositionTarget.TransformToDevice`
- Virtual screen coverage: `SystemParameters.VirtualScreenLeft/Top/Width/Height`
- Undo/redo via `List<List<UIElement>>` stacks

## Learnings

### 2026-03-02: Overlay rewrite tasked
The entire OverlayWindow + PreviewWindow is being rewritten as a single overlay-only experience:
- Phase 1 (selection): dim overlay, rubber-band selection, size label
- Phase 2 (annotation): overlay stays open, selected region shows clear, dashed border around it, vertical annotation toolbar floats right of selection, horizontal action bar (Copy + Close) floats below selection
- No separate window ever opens
- Annotation tools: Arrow, Text, Highlight, Pen, Line, Circle
- App.xaml.cs SnipCompleted event and PreviewWindow wiring will need updating

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace D3.Grc.Desktop;

internal enum Glyph
{
    Dashboard, Requirements, Shield, Risk, Supplier, Tasks, Mapping, Evidence, Audit, Finding,
    Ai, Code, Sources, Templates, Documents, Glossary, History, Save, Copy, Close, Settings,
    Export, Print, Open, Plus, Filter, Reset, Logout, Person, Arrow, Info, Cleanup
}

internal static class Icons
{
    public static PathIcon Draw(Glyph glyph, double size = 16, IBrush? color = null)
    {
        var icon = new PathIcon
        {
            Data = Symbols[glyph],
            Width = size,
            Height = size,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        if (color is not null)
            icon.Foreground = color;
        return icon;
    }

    private static readonly Dictionary<Glyph, Geometry> Symbols = new Dictionary<Glyph, string>
    {
        [Glyph.Dashboard] = "M3,4 H11 V11 H3 Z M13,4 H21 V14 H13 Z M3,13 H11 V20 H3 Z M13,16 H21 V20 H13 Z",
        [Glyph.Requirements] = "F0 M5,3 H19 V21 H5 Z M7,5 H17 V19 H7 Z M9,8 H15 V10 H9 Z M9,12 H15 V14 H9 Z M9,16 H13 V17 H9 Z",
        [Glyph.Shield] = "F0 M12,2 L21,6 V11 Q21,18 12,22 Q3,18 3,11 V6 Z M12,4 L5,7 V11 Q5,17 12,20 Q19,17 19,11 V7 Z M8,11 L11,14 L16,9 L17,10 L11,16 L7,12 Z",
        [Glyph.Risk] = "F0 M12,3 L23,21 H1 Z M12,7 L5,19 H19 Z M11,10 H13 V14 H11 Z M11,16 H13 V18 H11 Z",
        [Glyph.Supplier] = "F0 M3,4 H13 V8 L17,6 V9 L21,7 V20 H3 Z M5,6 V18 H19 V10 L15,12 V9 L11,12 V6 Z M8,14 H11 V16 H8 Z M13,14 H16 V16 H13 Z",
        [Glyph.Tasks] = "M3,6 L5,8 L9,4 L10,5 L5,10 L2,7 Z M12,6 H22 V8 H12 Z M3,15 L5,17 L9,13 L10,14 L5,19 L2,16 Z M12,15 H22 V17 H12 Z",
        [Glyph.Mapping] = "F0 M3,4 H9 V10 H3 Z M5,6 V8 H7 V6 Z M15,14 H21 V20 H15 Z M17,16 V18 H19 V16 Z M5,10 H7 V16 H15 V18 H5 Z M13,6 H19 V12 H17 V8 H13 Z",
        [Glyph.Evidence] = "F0 M9,2 H16 L21,7 V22 H3 V7 H9 Z M11,4 V9 H5 V20 H19 V9 H14 V4 Z M8,12 L11,15 L16,10 L17,11 L11,17 L7,13 Z",
        [Glyph.Audit] = "F0 M5,5 H8 V2 H16 V5 H19 V22 H5 Z M7,7 V20 H17 V7 H15 V8 H9 V7 Z M10,4 V6 H14 V4 Z M9,11 H15 V13 H9 Z M9,15 H14 V17 H9 Z",
        [Glyph.Finding] = "F0 M3,4 H21 V17 H12 L7,22 V17 H3 Z M5,6 V15 H9 V18 L11,15 H19 V6 Z M11,8 H13 V11 H11 Z M11,12 H13 V14 H11 Z",
        [Glyph.Ai] = "F0 M6,6 H18 V18 H6 Z M8,8 V16 H16 V8 Z M10,10 H14 V14 H10 Z M8,2 H10 V6 H8 Z M14,2 H16 V6 H14 Z M8,18 H10 V22 H8 Z M14,18 H16 V22 H14 Z M2,8 H6 V10 H2 Z M2,14 H6 V16 H2 Z M18,8 H22 V10 H18 Z M18,14 H22 V16 H18 Z",
        [Glyph.Code] = "M7,6 L9,7 L4,12 L9,17 L7,18 L1,12 Z M17,6 L23,12 L17,18 L15,17 L20,12 L15,7 Z M12,4 H14 L12,20 H10 Z",
        [Glyph.Sources] = "F0 M3,4 H10 L12,6 L14,4 H21 V19 H14 L12,21 L10,19 H3 Z M5,6 V17 H10 L11,18 V8 L9,6 Z M19,6 H15 L13,8 V18 L14,17 H19 Z",
        [Glyph.Templates] = "F0 M3,4 H17 V18 H3 Z M5,6 V16 H15 V6 Z M7,8 H13 V10 H7 Z M7,12 H12 V14 H7 Z M17,7 H21 V22 H7 V18 H9 V20 H19 V9 H17 Z",
        [Glyph.Documents] = "F0 M4,2 H14 L20,8 V22 H4 Z M6,4 V20 H18 V9 H13 V4 Z M15,5 V7 H17 Z M8,11 H16 V13 H8 Z M8,15 H16 V17 H8 Z",
        [Glyph.Glossary] = "F0 M4,3 H19 V21 H4 Z M6,5 V19 H17 V5 Z M8,15 L11,8 H12 L15,15 H13 L12.5,13 H10.5 L10,15 Z M11,12 H12 L11.5,10 Z",
        [Glyph.History] = "F0 M12,3 A9,9 0 1 1 4,16 L6,15 A7,7 0 1 0 6,8 H9 V10 H2 V3 H4 V7 Q7,3 12,3 Z M11,7 H13 V12 L17,14 L16,16 L11,13 Z",
        [Glyph.Save] = "F0 M4,3 H17 L21,7 V21 H3 V3 Z M5,5 V19 H7 V13 H17 V19 H19 V8 L16,5 V10 H8 V5 Z M10,5 V8 H14 V5 Z M9,15 V19 H15 V15 Z",
        [Glyph.Copy] = "F0 M8,7 H21 V22 H8 Z M10,9 V20 H19 V9 Z M3,2 H17 V5 H15 V4 H5 V17 H6 V19 H3 Z",
        [Glyph.Close] = "M5,6 L6,5 L12,11 L18,5 L19,6 L13,12 L19,18 L18,19 L12,13 L6,19 L5,18 L11,12 Z",
        [Glyph.Settings] = "F0 M10,2 H14 L15,5 L18,6 L21,5 L23,9 L21,11 V13 L23,15 L21,19 L18,18 L15,19 L14,22 H10 L9,19 L6,18 L3,19 L1,15 L3,13 V11 L1,9 L3,5 L6,6 L9,5 Z M12,7 A5,5 0 1 0 12,17 A5,5 0 1 0 12,7 Z",
        [Glyph.Export] = "M11,3 H13 V13 L17,9 L18,10 L12,16 L6,10 L7,9 L11,13 Z M4,15 H6 V19 H18 V15 H20 V21 H4 Z",
        [Glyph.Print] = "F0 M7,3 H17 V7 H21 V17 H17 V21 H7 V17 H3 V7 H7 Z M9,5 V7 H15 V5 Z M5,9 V15 H7 V13 H17 V15 H19 V9 Z M9,15 V19 H15 V15 Z M16,10 H18 V12 H16 Z",
        [Glyph.Open] = "F0 M3,5 H10 L12,7 H20 V10 H23 L19,20 H2 V5 Z M4,7 V15 L6,10 H18 V9 H11 L9,7 Z M7,12 L4,18 H18 L20,12 Z",
        [Glyph.Plus] = "M11,4 H13 V11 H20 V13 H13 V20 H11 V13 H4 V11 H11 Z",
        [Glyph.Filter] = "F0 M3,4 H21 V6 L15,13 V20 L9,22 V13 L3,6 Z M6,6 L11,12 V19 L13,18 V12 L18,6 Z",
        [Glyph.Reset] = "M11,3 A9,9 0 1 1 4,17 L6,16 A7,7 0 1 0 6,8 H10 V10 H2 V3 H4 V7 Q7,3 11,3 Z",
        [Glyph.Logout] = "M4,4 H13 V6 H6 V18 H13 V20 H4 Z M15,7 L21,12 L15,17 L14,15 L17,13 H9 V11 H17 L14,9 Z",
        [Glyph.Person] = "F0 M12,3 A4,4 0 1 1 12,11 A4,4 0 1 1 12,3 Z M12,5 A2,2 0 1 0 12,9 A2,2 0 1 0 12,5 Z M4,21 V18 Q4,13 12,13 Q20,13 20,18 V21 Z M6,19 H18 V18 Q18,15 12,15 Q6,15 6,18 Z",
        [Glyph.Arrow] = "M13,5 L20,12 L13,19 L11.5,17.5 L16,13 H4 V11 H16 L11.5,6.5 Z",
        [Glyph.Info] = "F0 M12,2 A10,10 0 1 1 12,22 A10,10 0 1 1 12,2 Z M12,4 A8,8 0 1 0 12,20 A8,8 0 1 0 12,4 Z M11,7 H13 V9 H11 Z M11,11 H13 V17 H11 Z",
        [Glyph.Cleanup] = "F0 M3,5 H9 V3 H15 V5 H21 V7 H19 L18,21 H6 L5,7 H3 Z M7,7 L8,19 H16 L17,7 Z M10,9 H11 V17 H10 Z M13,9 H14 V17 H13 Z"
    }.ToDictionary(pair => pair.Key, pair => Geometry.Parse(pair.Value));
}

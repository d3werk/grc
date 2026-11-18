using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
namespace D3.Grc.Desktop;
internal static class Ui
{
    public const string KeyboardHelp = "F1 zeigt Hinweise zum fokussierten Element.";
    public const string AccentColor = "#21624D";
    public static readonly IBrush Green = Brush.Parse(AccentColor);
    public static readonly IBrush Blue = Brush.Parse("#225D80");
    public static readonly IBrush Ink = Brush.Parse("#172924");
    public static readonly IBrush Muted = Brush.Parse("#4E645B");
    public static readonly IBrush Canvas = Brush.Parse("#F1F5F3");
    public static readonly IBrush Surface = Brushes.White;
    public static readonly IBrush Line = Brush.Parse("#D6E2DC");
    public static readonly IBrush SoftGreen = Brush.Parse("#E6F2EC");
    private static readonly IBrush SoftBlue = Brush.Parse("#E9F2F7");

    public static TextBlock Text(string text,int size=14,bool bold=false)=>new(){Text=text,FontSize=size,FontWeight=bold?FontWeight.SemiBold:FontWeight.Normal,Foreground=Ink,TextWrapping=TextWrapping.Wrap};
    public static TextBlock Secondary(string text, int size = 13)
    {
        var block = Text(text, size);
        block.Foreground = Muted;
        return block;
    }

    public static TextBlock Heading(string text, int size = 20, int level = 2)
    {
        var heading = Text(text, size, true);
        AutomationProperties.SetHeadingLevel(heading, level);
        return heading;
    }

    public static StackPanel Stack(params Control[] controls){var p=new StackPanel{Spacing=12};foreach(var c in controls)p.Children.Add(c);return p;}
    public static WrapPanel Row(params Control[] controls)
    {
        var panel = new WrapPanel { ItemSpacing = 8, LineSpacing = 8 };
        foreach (var control in controls)
            panel.Children.Add(control);
        return panel;
    }

    public static Border Card(Control content) => new()
    {
        Background = Surface,
        BorderBrush = Line,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(18),
        Child = content
    };

    public static Border Notice(string text) => new()
    {
        Background = SoftBlue,
        Padding = new Thickness(12),
        CornerRadius = new CornerRadius(8),
        Child = Text(text, 13)
    };

    public static UniformGrid AdaptiveCards(int maximumColumns, int minimumCardWidth, params Control[] cards)
    {
        const double spacing = 12;
        var grid = new UniformGrid { Columns = 1, ColumnSpacing = spacing, RowSpacing = spacing };
        int columnLimit = Math.Max(1, Math.Min(maximumColumns, cards.Length));
        foreach (var card in cards)
            grid.Children.Add(card);
        grid.SizeChanged += (_, e) =>
        {
            var available = e.NewSize.Width + spacing;
            var columns = Math.Clamp((int)(available / (minimumCardWidth + spacing)), 1, columnLimit);
            grid.Columns = columnLimit == 4 && columns == 3 ? 2 : columns;
        };
        return grid;
    }
    public static TextBox Input(string hint="",bool multiline=false)=>new(){Watermark=hint,MinHeight=multiline?150:38,AcceptsReturn=multiline,TextWrapping=multiline?TextWrapping.Wrap:TextWrapping.NoWrap};
    public static ComboBox Combo()=>new(){HorizontalAlignment=HorizontalAlignment.Stretch,MinHeight=38,MaxDropDownHeight=320};
    public static T Help<T>(T control, string text, string? accessibleName = null) where T : Control
    {
        ToolTip.SetTip(control, new TextBlock { Text = text, MaxWidth = 320, TextWrapping = TextWrapping.Wrap });
        ToolTip.SetPlacement(control, PlacementMode.Bottom);
        ToolTip.SetVerticalOffset(control, 6);
        AutomationProperties.SetHelpText(control, text);
        if (accessibleName is not null)
            AutomationProperties.SetName(control, accessibleName);

        control.KeyDown += (_, e) =>
        {
            if (e.Key == Key.F1)
            {
                ToolTip.SetIsOpen(control, !ToolTip.GetIsOpen(control));
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && ToolTip.GetIsOpen(control))
            {
                ToolTip.SetIsOpen(control, false);
                e.Handled = true;
            }
        };
        control.LostFocus += (_, _) => ToolTip.SetIsOpen(control, false);
        return control;
    }

    public static Control Field(string label, Control input, string? help = null)
    {
        var heading = Text(label, 13, true);
        AutomationProperties.SetLabeledBy(input, heading);
        if (label.EndsWith(" *", StringComparison.Ordinal))
        {
            AutomationProperties.SetName(input, label[..^2]);
            AutomationProperties.SetIsRequiredForForm(input, true);
        }
        if (help is not null)
            Help(input, help);
        var field = Stack(heading, input);
        field.Spacing = 5;
        return field;
    }
    public static ScrollViewer Scroll(Control c)=>new(){Content=c,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
    public static Grid Split(Control left, Control right)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions
            {
                new(2, GridUnitType.Star) { MinWidth = 160 },
                new(3, GridUnitType.Star) { MinWidth = 415 }
            },
            ColumnSpacing = 12
        };
        grid.Children.Add(left);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }
    public static Grid Fill(Control top,Control body,Control? bottom=null){var g=new Grid{RowDefinitions=new RowDefinitions(bottom is null?"Auto,*":"Auto,*,Auto")};top.Margin=new Thickness(0,0,0,12);g.Children.Add(top);Grid.SetRow(body,1);g.Children.Add(body);if(bottom is not null){Grid.SetRow(bottom,2);bottom.Margin=new Thickness(0,12,0,0);g.Children.Add(bottom);}return g;}
    public static TabItem Tab(string title, Control body, string? help = null)
    {
        var tab = new TabItem { Header = title, Content = body };
        return help is null ? tab : Help(tab, help);
    }
    public static async Task<string?> Prompt(Window owner,string title,bool secret=false)
    {
        var w=new Window{Title="D3 GRC",Width=600,SizeToContent=SizeToContent.Height,WindowStartupLocation=WindowStartupLocation.CenterOwner};var input=Input();if(secret)input.PasswordChar='*';var ok=new Button{Content="Weiter",MinHeight=38};var cancel=new Button{Content="Abbrechen",MinHeight=38};ok.Click+=(_,_)=>{if(!string.IsNullOrWhiteSpace(input.Text))w.Close(input.Text);};cancel.Click+=(_,_)=>w.Close(null);var p=Stack(Field(title,input),Row(cancel,ok));p.Margin=new Thickness(24);w.Content=p;var value=await w.ShowDialog<string?>(owner);input.Text="";return value;
    }
    public static async Task<bool> Confirm(Window owner,string text,string yes="Bestätigen")
    {
        var w=new Window{Title="D3 GRC",Width=640,SizeToContent=SizeToContent.Height,WindowStartupLocation=WindowStartupLocation.CenterOwner};var ok=new Button{Content=yes,MinHeight=38};var cancel=new Button{Content="Abbrechen",MinHeight=38};ok.Click+=(_,_)=>w.Close(true);cancel.Click+=(_,_)=>w.Close(false);var p=Stack(Text(text),Row(cancel,ok));p.Margin=new Thickness(24);w.Content=p;return await w.ShowDialog<bool>(owner);
    }
    public static async Task<object?> Pick(Window owner,string title,IEnumerable<object> choices)
    {
        var w=new Window{Title=title,Width=760,Height=500,WindowStartupLocation=WindowStartupLocation.CenterOwner};var list=new ListBox{ItemsSource=choices.ToList()};var ok=new Button{Content="Auswählen"};ok.Click+=(_,_)=>w.Close(list.SelectedItem);var cancel=new Button{Content="Abbrechen"};cancel.Click+=(_,_)=>w.Close(null);var heading=Text(title,18,true);AutomationProperties.SetLabeledBy(list,heading);var p=Fill(heading,list,Row(cancel,ok));p.Margin=new Thickness(20);w.Content=p;return await w.ShowDialog<object?>(owner);
    }
}

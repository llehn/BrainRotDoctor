using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace BrainRotDoctor.DesignSystem;

/// <summary>A read-only row on a sunken background: icon, title and caption, and a status at the end.</summary>
public sealed class InfoRow : Border
{
    public InfoRow(string glyph, string title, string caption, Control trailing)
    {
        var row = new DockPanel { LastChildFill = true };
        var icon = new Icon(glyph, 16, TextTone.Tertiary) { Margin = new Thickness(0, 0, Space.S3, 0) };
        DockPanel.SetDock(icon, Dock.Left);
        DockPanel.SetDock(trailing, Dock.Right);
        trailing.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(icon);
        row.Children.Add(trailing);
        row.Children.Add(Layout.Column(1, new TextLine(title, TextStyle.Strong), new TextLine(caption, TextStyle.Caption)));

        MinHeight = 56;
        Padding = new Thickness(14, Space.S2);
        CornerRadius = new CornerRadius(Radius.Control);
        Child = row;
        this[!BackgroundProperty] = Palette.Dyn(Palette.AppBg);
    }
}

/// <summary>A titled card of rows separated by dividers (e.g. a settings section).</summary>
public sealed class Group : Border
{
    public Group(string title, params Control[] rows)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextLine(title, TextStyle.Overline) { Margin = new Thickness(22, 18, 22, 6) });
        for (int i = 0; i < rows.Length; i++)
        {
            if (i > 0)
            {
                stack.Children.Add(new Divider());
            }

            stack.Children.Add(rows[i]);
        }

        Child = new Card(stack, CardStyle.Surface, CardPadding.None);
    }

    /// <summary>Free content as a row of a group, with the group's padding.</summary>
    public static Control Row(Control content) => new Border { Padding = new Thickness(22, 14), Child = content };
}

/// <summary>A row of a <see cref="Group"/>: a title and help text on the left, the control on the right.</summary>
public sealed class FormRow : Grid
{
    public FormRow(string title, string help, Control control)
    {
        ColumnDefinitions = new ColumnDefinitions("*,Auto");
        Margin = new Thickness(22, 14);
        Children.Add(Layout.Column(2, new TextLine(title, TextStyle.Strong), new TextLine(help, TextStyle.Caption)));
        control.VerticalAlignment = VerticalAlignment.Center;
        control.Margin = new Thickness(Space.S4, 0, 0, 0);
        SetColumn(control, 1);
        Children.Add(control);
    }
}

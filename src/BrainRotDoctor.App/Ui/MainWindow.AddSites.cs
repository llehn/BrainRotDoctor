using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Add websites (full window) ----------

    private readonly List<string> _pickedCatalog = new();
    private readonly List<EditableConfiguration.EditableSite> _pickedCustom = new();
    private string _siteQuery = "";
    private string _customAddress = "";
    private string? _customError;

    private Border? _tilesHost;
    private Border? _pickedHost;
    private PillButton? _addButton;

    private void OpenAddSites()
    {
        _pickedCatalog.Clear();
        _pickedCustom.Clear();
        _siteQuery = "";
        _customAddress = "";
        _customError = null;
        Navigate(Page.AddSites);
    }

    private Control BuildAddSites()
    {
        EditableConfiguration.EditableRule rule = _editingRule!;

        PillButton back = UiTheme.Ghost(string.IsNullOrWhiteSpace(rule.Name) ? Loc.T("untitled") : rule.Name, Icons.ArrowLeft);
        back.Click += (_, _) => Navigate(Page.Editor);
        PillButton cancel = UiTheme.Secondary(Loc.T("cancel"));
        cancel.Click += (_, _) => Navigate(Page.Editor);
        _addButton = UiTheme.Primary(Loc.T("add"));
        _addButton.Click += (_, _) => CommitPickedSites();

        var search = new TextBox
        {
            Text = _siteQuery,
            Watermark = Loc.T("search_websites"),
            FontSize = 15,
            MinHeight = 46,
            InnerLeftContent = new Border { Margin = new Thickness(14, 0, 4, 0), Child = Icons.Make(Icons.Search, 18, UiTheme.TextTertiary) },
        };
        search.TextChanged += (_, _) =>
        {
            _siteQuery = search.Text ?? "";
            RebuildTiles();
        };

        _tilesHost = new Border();
        _pickedHost = new Border();
        RebuildTiles();

        Control left = UiTheme.VStack(18, search, _tilesHost, CustomSiteCard());
        return FullScreen(
            FullScreenBar(back, UiTheme.Text(Loc.T("add_websites"), 20, FontWeight.SemiBold, UiTheme.TextPrimary), UiTheme.HStack(10, cancel, _addButton)),
            Columns(left, _pickedHost, 320, 940, fixedRight: true));
    }

    private void RebuildTiles()
    {
        if (_tilesHost is null || _editingRule is not { } rule)
        {
            return;
        }

        string q = _siteQuery.Trim();
        var tiles = SiteCatalog.Sites
            .Where(site => q.Length == 0
                || site.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || site.Host.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(site => SiteTile(site, already: rule.Sites.Any(s => s.CatalogId == site.Id)))
            .ToList();

        _tilesHost.Child = tiles.Count == 0
            ? UiTheme.Muted(Loc.T("no_matching_sites"))
            : TileGrid(tiles, 200, 12);
        RebuildPicked();
    }

    private Control SiteTile(CatalogSite site, bool already)
    {
        bool picked = already || _pickedCatalog.Contains(site.Id);

        var check = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            BorderThickness = new Thickness(1.5),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Child = picked ? new Border { HorizontalAlignment = HorizontalAlignment.Center, Child = Icons.Make(Icons.Check, 13, UiTheme.InkText, 3) } : null,
            [!Border.BackgroundProperty] = UiTheme.Dyn(picked ? (already ? UiTheme.TextTertiary : UiTheme.Ink) : UiTheme.Surface),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(picked ? (already ? UiTheme.TextTertiary : UiTheme.Ink) : UiTheme.BorderStrong),
        };

        var top = new Grid();
        Border mono = UiTheme.Monogram(site.Id, site.Name);
        mono.HorizontalAlignment = HorizontalAlignment.Left;
        top.Children.Add(mono);
        top.Children.Add(check);

        string sub = already ? Loc.T("already_in_rule") : string.Join(", ", site.Pages.Select(p => p.Name));
        var content = UiTheme.VStack(12, top, UiTheme.VStack(3, UiTheme.Strong(site.Name, 15), UiTheme.Caption(sub)));

        var tile = new Border
        {
            MinHeight = 140,
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1.5),
            Child = content,
            Opacity = already ? 0.65 : 1,
            Cursor = new Cursor(already ? StandardCursorType.Arrow : StandardCursorType.Hand),
            [!Border.BackgroundProperty] = UiTheme.Dyn(already ? UiTheme.SurfaceAlt : UiTheme.Surface),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(picked && !already ? UiTheme.Ink : UiTheme.Border_),
        };

        if (!already)
        {
            tile.AddHandler(Gestures.TappedEvent, (_, _) =>
            {
                if (!_pickedCatalog.Remove(site.Id))
                {
                    _pickedCatalog.Add(site.Id);
                }

                RebuildTiles();
            });
        }

        return tile;
    }

    private Control CustomSiteCard()
    {
        var address = new TextBox
        {
            Text = _customAddress,
            Watermark = Loc.T("custom_address_hint"),
            FontFamily = new FontFamily(UiTheme.MonoFont),
        };
        address.TextChanged += (_, _) => _customAddress = address.Text ?? "";

        var error = UiTheme.Text(_customError ?? "", 12.5, FontWeight.Medium, UiTheme.Danger);
        error.IsVisible = _customError is not null;

        PillButton add = UiTheme.Secondary(Loc.T("add_website"));
        void Add()
        {
            try
            {
                SiteAddress parsed = SiteUrl.ParseAddress(_customAddress);
                string label = parsed.Host + (SitePage.NormalizePath(parsed.Path) == "/" ? "" : SitePage.NormalizePath(parsed.Path));
                _pickedCustom.Add(EditableConfiguration.EditableSite.FromDocument(SiteMigration.FromUrl(label, _customAddress, includeSubpaths: true)));
                _customAddress = "";
                address.Text = "";
                error.IsVisible = false;
                _customError = null;
                RebuildPicked();
            }
            catch (ConfigurationException)
            {
                _customError = Loc.T("custom_invalid");
                error.Text = _customError;
                error.IsVisible = true;
            }
        }

        add.Click += (_, _) => Add();
        address.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Add();
            }
        };

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        row.Children.Add(address);
        add.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(add, 1);
        row.Children.Add(add);

        return UiTheme.Card(UiTheme.VStack(12,
            UiTheme.VStack(4, UiTheme.H2(Loc.T("any_other_website")), UiTheme.Muted(Loc.T("any_other_website_help"))),
            row,
            error));
    }

    private void RebuildPicked()
    {
        if (_pickedHost is null)
        {
            return;
        }

        var sites = _pickedCatalog.Select(id => EditableConfiguration.EditableSite.FromCatalog(SiteCatalog.Get(id)))
            .Concat(_pickedCustom)
            .ToList();

        var list = UiTheme.VStack(16,
            UiTheme.VStack(4, UiTheme.H2(Loc.T("coming_into_rule")), UiTheme.Muted(Loc.T("coming_into_rule_help"))));

        if (sites.Count == 0)
        {
            TextBlock empty = UiTheme.Muted(Loc.T("pick_a_website"));
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            list.Children.Add(new Border
            {
                Padding = new Thickness(12, 24),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1.5),
                Child = empty,
                [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
            });
        }

        foreach (EditableConfiguration.EditableSite site in sites)
        {
            PillButton remove = UiTheme.IconButton(Icons.Close, Loc.T("remove"));
            remove.Width = 32;
            remove.Height = 32;
            remove.MinHeight = 32;
            remove.BorderThickness = new Thickness(0);
            remove.Background = Brushes.Transparent;
            remove.Click += (_, _) =>
            {
                if (site.CatalogId is { } id && _pickedCatalog.Contains(id))
                {
                    _pickedCatalog.Remove(id);
                    RebuildTiles();
                }
                else
                {
                    _pickedCustom.Remove(site);
                    RebuildPicked();
                }
            };

            var head = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(remove, Dock.Right);
            head.Children.Add(remove);
            head.Children.Add(UiTheme.HStack(10, UiTheme.Monogram(site.CatalogId, site.DisplayLabel, 26), UiTheme.Strong(site.DisplayLabel)));

            var entry = UiTheme.VStack(8, head);
            foreach (EditableConfiguration.EditablePage page in site.Pages)
            {
                entry.Children.Add(VerdictLine(page.Name, page.Block));
            }

            if (site.BlockEverythingElse || site.Pages.Count == 0)
            {
                entry.Children.Add(VerdictLine(site.Pages.Count == 0 ? Loc.T("whole_site") : Loc.T("everything_else"), site.BlockEverythingElse));
            }

            list.Children.Add(entry);
            list.Children.Add(UiTheme.Divider());
        }

        _pickedHost.Child = UiTheme.Card(list);

        int count = sites.Count;
        if (_addButton is not null)
        {
            _addButton.Text = count switch
            {
                0 => Loc.T("add"),
                1 => Loc.T("add_one_website"),
                _ => Loc.T("add_n_websites", count),
            };
            _addButton.Enabled = count > 0;
        }
    }

    private static Control VerdictLine(string name, bool block)
    {
        Border chip = UiTheme.Chip(block ? Loc.T("block") : Loc.T("allow"), block ? UiTheme.BlockSoft : UiTheme.AllowSoft, block ? UiTheme.Danger : UiTheme.Accent);
        var row = new DockPanel { LastChildFill = true, Margin = new Thickness(36, 0, 0, 0) };
        DockPanel.SetDock(chip, Dock.Right);
        row.Children.Add(chip);
        row.Children.Add(UiTheme.Muted(name));
        return row;
    }

    private void CommitPickedSites()
    {
        if (_editingRule is not { } rule)
        {
            return;
        }

        var added = _pickedCatalog.Select(id => EditableConfiguration.EditableSite.FromCatalog(SiteCatalog.Get(id)))
            .Concat(_pickedCustom)
            .ToList();
        if (added.Count == 0)
        {
            return;
        }

        foreach (EditableConfiguration.EditableSite site in added)
        {
            rule.Sites.Add(site);
            _openSites.Add(site);
        }

        Navigate(Page.Editor);
    }
}

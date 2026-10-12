using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Add websites (full window) ----------

    private readonly List<string> _pickedCatalog = new();
    private readonly List<EditableConfiguration.EditableSite> _pickedCustom = new();
    private string _siteQuery = "";
    private string _customAddress = "";
    private string? _customError;

    private ContentControl? _tilesHost;
    private ContentControl? _pickedHost;
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

        var back = new PillButton(string.IsNullOrWhiteSpace(rule.Name) ? Loc.T("untitled") : rule.Name, ButtonKind.Ghost, Glyph.ArrowLeft);
        back.Click += (_, _) => Navigate(Page.Editor);
        var cancel = new PillButton(Loc.T("cancel"));
        cancel.Click += (_, _) => Navigate(Page.Editor);
        _addButton = new PillButton(Loc.T("add"), ButtonKind.Primary);
        _addButton.Click += (_, _) => CommitPickedSites();

        var search = new TextField(_siteQuery, Loc.T("search_websites"), TextFieldStyle.Large, leadingGlyph: Glyph.Search);
        search.Changed += text =>
        {
            _siteQuery = text;
            RebuildTiles();
        };

        _tilesHost = new ContentControl();
        _pickedHost = new ContentControl();
        RebuildTiles();

        StackPanel left = Layout.Column(18, search, _tilesHost, CustomSiteCard());
        return new FullPageLayout(
            new TopBar(back, new TextLine(Loc.T("add_websites"), TextStyle.Heading), Layout.Row(10, cancel, _addButton)),
            Layout.Columns(left, _pickedHost, 320, fixedRight: true));
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

        _tilesHost.Content = tiles.Count == 0
            ? new TextLine(Loc.T("no_matching_sites"), TextStyle.Small)
            : Layout.Tiles(tiles, 200, Space.S3);
        RebuildPicked();
    }

    private Control SiteTile(CatalogSite site, bool already)
    {
        string sub = already ? Loc.T("already_in_rule") : string.Join(", ", site.Pages.Select(p => p.Name));
        StackPanel content = Layout.Column(Space.S3,
            SiteLook.Monogram(site.Id, site.Name),
            Layout.Column(3, new TextLine(site.Name, TextStyle.Label), new TextLine(sub, TextStyle.Caption)));

        var tile = new PickTile(content, picked: _pickedCatalog.Contains(site.Id), locked: already);
        tile.Click += (_, _) =>
        {
            if (!_pickedCatalog.Remove(site.Id))
            {
                _pickedCatalog.Add(site.Id);
            }

            RebuildTiles();
        };
        return tile;
    }

    private Control CustomSiteCard()
    {
        var address = new TextField(_customAddress, Loc.T("custom_address_hint"), TextFieldStyle.Code);
        address.Changed += text => _customAddress = text;

        var error = new TextLine(_customError ?? "", TextStyle.CaptionStrong, TextTone.Danger) { IsVisible = _customError is not null };

        var add = new PillButton(Loc.T("add_website"));
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
                error.SetText(_customError);
                error.IsVisible = true;
            }
        }

        add.Click += (_, _) => Add();
        address.Submitted += Add;

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        row.Children.Add(address);
        add.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(add, 1);
        row.Children.Add(add);

        return new Card(Layout.Column(Space.S3,
            Layout.Column(Space.S1, new TextLine(Loc.T("any_other_website"), TextStyle.Subheading), new TextLine(Loc.T("any_other_website_help"), TextStyle.Small)),
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

        StackPanel list = Layout.Column(Space.S4,
            Layout.Column(Space.S1, new TextLine(Loc.T("coming_into_rule"), TextStyle.Subheading), new TextLine(Loc.T("coming_into_rule_help"), TextStyle.Small)));

        if (sites.Count == 0)
        {
            list.Children.Add(new Card(new TextLine(Loc.T("pick_a_website"), TextStyle.Small) { Centered = true }, CardStyle.Outline, CardPadding.Roomy));
        }

        foreach (EditableConfiguration.EditableSite site in sites)
        {
            var remove = new IconButton(Glyph.Close, Loc.T("remove"), IconButtonStyle.Bare);
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
            head.Children.Add(Layout.Row(10, SiteLook.Monogram(site.CatalogId, site.DisplayLabel, 26), new TextLine(site.DisplayLabel, TextStyle.Strong)));

            StackPanel entry = Layout.Column(Space.S2, head);
            foreach (EditableConfiguration.EditablePage page in site.Pages)
            {
                entry.Children.Add(VerdictLine(page.Name, page.Block));
            }

            if (site.BlockEverythingElse || site.Pages.Count == 0)
            {
                entry.Children.Add(VerdictLine(site.Pages.Count == 0 ? Loc.T("whole_site") : Loc.T("everything_else"), site.BlockEverythingElse));
            }

            list.Children.Add(entry);
            list.Children.Add(new Divider());
        }

        _pickedHost.Content = new Card(list);

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
        var chip = new Chip(block ? Loc.T("block") : Loc.T("allow"), block ? Tone.Danger : Tone.Accent);
        var row = new DockPanel { LastChildFill = true, Margin = new Thickness(36, 0, 0, 0) };
        DockPanel.SetDock(chip, Dock.Right);
        row.Children.Add(chip);
        row.Children.Add(new TextLine(name, TextStyle.Small));
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

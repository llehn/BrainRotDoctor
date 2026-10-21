using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Apps;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Add websites, apps and games (full window) ----------

    /// <summary>Steam games shown before "Show all": the rest are a search away.</summary>
    private const int GamesShown = 12;

    private readonly List<string> _pickedCatalog = new();
    private readonly List<EditableConfiguration.EditableSite> _pickedCustom = new();
    private readonly List<EditableConfiguration.EditableApp> _pickedApps = new();
    private string _siteQuery = "";
    private string _appQuery = "";
    private string _customAddress = "";
    private string? _customError;
    private string? _programError;
    private bool _addingApps;
    private bool _allGamesShown;

    private ContentControl? _tilesHost;
    private ContentControl? _pickedHost;
    private PillButton? _addButton;

    /// <summary>Opens the Add screen: on apps and games when the rule holds only those, else on websites.</summary>
    private void OpenAddSites()
    {
        _pickedCatalog.Clear();
        _pickedCustom.Clear();
        _pickedApps.Clear();
        _siteQuery = "";
        _appQuery = "";
        _customAddress = "";
        _customError = null;
        _programError = null;
        _allGamesShown = false;
        _addingApps = _editingRule is { Sites.Count: 0, Apps.Count: > 0 };
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

        var tabs = new Segmented(new Segment[] { new(Loc.T("websites")), new(Loc.T("apps_games")) }, _addingApps ? 1 : 0, stretch: false)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        tabs.Changed += i =>
        {
            _addingApps = i == 1;
            Rerender();
        };

        string query = _addingApps ? _appQuery : _siteQuery;
        var search = new TextField(query, Loc.T(_addingApps ? "search_apps" : "search_websites"), TextFieldStyle.Large, leadingGlyph: Glyph.Search);
        search.Changed += text =>
        {
            if (_addingApps)
            {
                _appQuery = text;
            }
            else
            {
                _siteQuery = text;
            }

            RebuildTiles();
        };

        _tilesHost = new ContentControl();
        _pickedHost = new ContentControl();
        RebuildTiles();

        StackPanel left = Layout.Column(18, tabs, search, _tilesHost, _addingApps ? OtherProgramCard() : CustomSiteCard());
        return new FullPageLayout(
            new TopBar(back, new TextLine(Loc.T("add"), TextStyle.Heading), Layout.Row(10, cancel, _addButton)),
            Layout.Columns(left, _pickedHost, 320, fixedRight: true));
    }

    private void RebuildTiles()
    {
        if (_tilesHost is null || _editingRule is not { } rule)
        {
            return;
        }

        if (_addingApps)
        {
            _tilesHost.Content = AppTiles(rule);
            RebuildPicked();
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

    // ----- Apps and games -----

    /// <summary>
    /// Steam first (all its games, Steam itself, then each installed game), then the
    /// programs open now or used today. A search narrows both.
    /// </summary>
    private Control AppTiles(EditableConfiguration.EditableRule rule)
    {
        string q = _appQuery.Trim();
        bool Fits(string name) => q.Length == 0 || name.Contains(q, StringComparison.OrdinalIgnoreCase);
        bool allGames = rule.Apps.Any(a => a.Kind == TargetAppKind.AllSteamGames);
        var sections = new StackPanel { Spacing = Space.S5 };

        if (SteamInstall.IsInstalled)
        {
            var steam = new List<Control>();
            foreach (EditableConfiguration.EditableApp entry in new[] { EditableConfiguration.EditableApp.AllSteamGames(), EditableConfiguration.EditableApp.For(new AppIdentity(AppKind.Steam, "Steam", "steam.exe")) })
            {
                if (Fits(AppLabel(entry)))
                {
                    steam.Add(AppTile(rule, entry, null));
                }
            }

            List<SteamGame> games = SteamInstall.Games.Where(g => Fits(g.Name)).ToList();
            bool cut = q.Length == 0 && !_allGamesShown && games.Count > GamesShown;
            foreach (SteamGame game in cut ? games.Take(GamesShown) : games)
            {
                var entry = new EditableConfiguration.EditableApp { Kind = TargetAppKind.SteamGame, Label = game.Name, Folder = game.Folder };
                steam.Add(AppTile(rule, entry, allGames ? Loc.T("covered_by_all") : null, hideCaption: true));
            }

            if (steam.Count > 0)
            {
                StackPanel section = Layout.Column(Space.S3, new TextLine("Steam", TextStyle.Overline), Layout.Tiles(steam, 200, Space.S3));
                if (cut)
                {
                    var all = new PillButton(Loc.T("show_all_n", games.Count), ButtonKind.Accent) { HorizontalAlignment = HorizontalAlignment.Left };
                    all.Click += (_, _) =>
                    {
                        _allGamesShown = true;
                        RebuildTiles();
                    };
                    section.Children.Add(all);
                }

                sections.Children.Add(section);
            }
        }

        var open = _status.Apps.Where(a => a.Identity.Kind == AppKind.Program).Select(a => a.Identity).ToList();
        HashSet<string> openKeys = open.Select(a => a.Key).ToHashSet(StringComparer.Ordinal);
        List<AppIdentity> programs = open
            .Concat(_status.Activity.Apps.Values.Select(t => t.App).OfType<AppIdentity>().Where(a => a.Kind == AppKind.Program))
            .DistinctBy(a => a.Key)
            .Concat(_pickedApps.Where(a => a.Kind == TargetAppKind.Program && a.Exe is not null)
                .Select(a => new AppIdentity(AppKind.Program, a.Label, a.Exe!, a.Description)))
            .DistinctBy(a => a.Key)
            .Where(a => Fits(a.Name))
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        StackPanel programSection = Layout.Column(Space.S3, new TextLine(Loc.T("programs"), TextStyle.Overline));
        if (programs.Count == 0)
        {
            programSection.Children.Add(new TextLine(q.Length == 0 ? Loc.T("no_programs") : Loc.T("no_matching_sites"), TextStyle.Small));
        }
        else
        {
            programSection.Children.Add(Layout.Tiles(
                programs.Select(a => AppTile(rule, EditableConfiguration.EditableApp.For(a), openKeys.Contains(a.Key) ? Loc.T("open_now") : null)).ToList(),
                200,
                Space.S3));
        }

        sections.Children.Add(programSection);
        return sections;
    }

    /// <summary>A pickable app; locked when the rule has it already, or has all Steam games for a game.</summary>
    private Control AppTile(EditableConfiguration.EditableRule rule, EditableConfiguration.EditableApp entry, string? note, bool hideCaption = false)
    {
        bool already = rule.Apps.Any(a => a.SameAs(entry));
        bool covered = entry.Kind == TargetAppKind.SteamGame && rule.Apps.Any(a => a.Kind == TargetAppKind.AllSteamGames);
        string? sub = already ? Loc.T("already_in_rule") : covered ? note : note ?? (hideCaption ? null : AppCaption(entry));
        StackPanel text = Layout.Column(3, new TextLine(AppLabel(entry), TextStyle.Label));
        if (sub is not null)
        {
            text.Children.Add(new TextLine(sub, TextStyle.Caption) { SingleLine = true });
        }

        var tile = new PickTile(Layout.Column(Space.S3, AppMark(entry), text), picked: _pickedApps.Any(a => a.SameAs(entry)), locked: already || covered);
        ToolTip.SetTip(tile, AppLabel(entry));
        tile.Click += (_, _) =>
        {
            if (_pickedApps.RemoveAll(a => a.SameAs(entry)) == 0)
            {
                _pickedApps.Add(entry);
            }

            RebuildTiles();
        };
        return tile;
    }

    /// <summary>A program that isn't listed: chosen by its .exe file.</summary>
    private Control OtherProgramCard()
    {
        var choose = new PillButton(Loc.T("choose_program"), ButtonKind.Secondary) { HorizontalAlignment = HorizontalAlignment.Left };
        choose.Click += async (_, _) =>
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Loc.T("choose_program"),
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType(Loc.T("program")) { Patterns = new[] { "*.exe" } } },
            });
            if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path)
            {
                return;
            }

            if (new WindowsAppObserver().Identify(path) is { } app)
            {
                _programError = null;
                EditableConfiguration.EditableApp entry = EditableConfiguration.EditableApp.For(app);
                if (!_pickedApps.Any(a => a.SameAs(entry)))
                {
                    _pickedApps.Add(entry);
                }
            }
            else
            {
                _programError = Loc.T("cant_limit_program");
            }

            Rerender();
        };

        StackPanel card = Layout.Column(Space.S3,
            Layout.Column(Space.S1, new TextLine(Loc.T("other_program"), TextStyle.Subheading), new TextLine(Loc.T("other_program_help"), TextStyle.Small)),
            choose);
        if (_programError is not null)
        {
            card.Children.Add(new TextLine(_programError, TextStyle.CaptionStrong, TextTone.Danger));
        }

        return new Card(card);
    }

    // ----- Websites -----

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

    // ----- Coming into the rule -----

    private void RebuildPicked()
    {
        if (_pickedHost is null)
        {
            return;
        }

        var sites = _pickedCatalog.Select(id => EditableConfiguration.EditableSite.FromCatalog(SiteCatalog.Get(id)))
            .Concat(_pickedCustom)
            .ToList();

        // The help is about websites' pages; apps come whole.
        StackPanel heading = Layout.Column(Space.S1, new TextLine(Loc.T("coming_into_rule"), TextStyle.Subheading));
        if (!_addingApps)
        {
            heading.Children.Add(new TextLine(Loc.T("coming_into_rule_help"), TextStyle.Small));
        }

        StackPanel list = Layout.Column(Space.S4, heading);

        if (sites.Count == 0 && _pickedApps.Count == 0)
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

        foreach (EditableConfiguration.EditableApp app in _pickedApps)
        {
            list.Children.Add(AppLine(app, () =>
            {
                _pickedApps.Remove(app);
                RebuildTiles();
            }, mark: 26));
            list.Children.Add(new Divider());
        }

        _pickedHost.Content = new Card(list);

        int count = sites.Count + _pickedApps.Count;
        if (_addButton is not null)
        {
            _addButton.Text = count == 0 ? Loc.T("add") : Loc.T("add_n", count);
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
        if (added.Count == 0 && _pickedApps.Count == 0)
        {
            return;
        }

        foreach (EditableConfiguration.EditableSite site in added)
        {
            rule.Sites.Add(site);
            _openSites.Add(site);
        }

        foreach (EditableConfiguration.EditableApp app in _pickedApps)
        {
            rule.Block(app);
        }

        Navigate(Page.Editor);
    }
}

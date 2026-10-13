# Foundations

The tokens everything is built from. Screens never use these values directly:
they reach them through a control's variant, `Tone` or `TextTone`. The values
live in `Tokens.cs`; this page mirrors them.

## Character

Calm and serious, not playful. Warm greys for surfaces, near-black ("ink") for
the one main action, and two meaning colours: **teal** for allowed / on /
positive and **orange-red** for blocked / destructive. Amber is reserved for
temporary states (paused). Colour always carries meaning; it is never decoration.

## Colour

Every colour is a *role* with a light and a dark value. The theme follows
Windows unless the user picks one.

### Surfaces

| Role | Light | Dark | Use |
|---|---|---|---|
| AppBg | `#F4F3F0` | `#17181B` | Window background; insets and wells inside cards |
| Sidebar | `#ECEBE7` | `#1C1D21` | The sidebar |
| Surface | `#FFFFFF` | `#232428` | Cards, inputs, secondary buttons |
| SurfaceAlt | `#F8F7F4` | `#1F2024` | Shaded rows inside a card |
| Track | `#ECEBE7` | `#2C2D33` | Segmented control track, meter track, neutral chips |
| Selected | `#FFFFFF` | `#3A3B42` | The raised segment of a segmented control |
| Border | `#E2E0DA` | `#313238` | Hairlines: card borders, dividers |
| BorderStrong | `#D6D3CC` | `#44464D` | Control outlines, hover borders, the off switch |

### Text

| Role | Light | Dark | `TextTone` |
|---|---|---|---|
| TextPrimary | `#16171B` | `#ECECEE` | `Primary`: content |
| TextSecondary | `#4F525A` | `#B4B6BD` | `Secondary`: help text, inactive labels |
| TextTertiary | `#6B6E76` | `#8E9098` | `Tertiary`: fine print, metadata, icons in fields |

### Ink (the inverse surface)

| Role | Light | Dark | Use |
|---|---|---|---|
| Ink | `#16171B` | `#ECECEE` | Primary buttons, selected tiles and days, ink cards |
| InkText | `#FFFFFF` | `#16171B` | `TextTone.OnInk` |
| InkTextMuted | `#C5C5C6` | `#4C4D50` | `TextTone.OnInkMuted`: supporting text on ink |
| InkTrack | `#333436` | `#CFCFD1` | Meter track on ink |

### Meaning

| Role | Light | Dark | `Tone` | Use |
|---|---|---|---|---|
| Accent | `#0B6B63` | `#2A9D8F` | `Accent` | Allowed, on, positive; light constructive actions |
| AccentSoft | `#E1F0EE` | `#173A36` | | Background of accent chips and callouts |
| AccentText | `#FFFFFF` | `#FFFFFF` | | Text on a filled accent or danger segment |
| Danger | `#B93D0E` | `#D2602F` | `Danger` | Blocked, destructive, errors |
| DangerSoft | `#FBE9E1` | `#45261A` | | Background of danger chips, banners, callouts |
| Success | `#0B6B63` | `#3DBBA9` | `Success` | Healthy state (status dot) |
| Warn | `#B07400` | `#E2A93B` | `Warn` | Temporary state marker (dot) |
| WarnSoft | `#FBF0D9` | `#3A2E14` | | Background of warn chips, banners, the resume button |
| WarnBorder | `#EBD3A0` | `#5C4920` | | Outline of warn banners and buttons |
| WarnText | `#5C3D00` | `#F2C76B` | `TextTone.Warn` | Text on warn surfaces |
| Knob | `#FFFFFF` | `#FFFFFF` | | The knob of a switch |
| Scrim | `#7316171B` | `#99000000` | | Dims the screen behind an `Overlay` |

### Maps

The one place colour is not meaning: tile colours only tell neighbouring
tiles of a `TileMap` apart. The clock fills do carry meaning, a ramp from
little time to a lot, getting stronger (darker in light, brighter in dark).

| Role | Light | Dark | Use |
|---|---|---|---|
| Tile1–Tile8 | pastels `#CFE3F3` `#F6D6C8` `#D9EBCF` `#EBD9F2` `#F7E7B9` `#CDEBE6` `#F3D3DE` `#DCDDF5` | deep muted `#22384A` `#4A3026` `#2B3F29` `#3D2D45` `#463C1F` `#1F403B` `#462A35` `#2E3050` | `MapTile` backgrounds |
| TileInset | `#80FFFFFF` | `#1FFFFFFF` | Nested tiles inside a tile |
| Clock1–Clock5 | `#8CCBC1` → `#063F3A` | `#2F6F66` → `#C2F2EA` | `ClockFill`: under ½ h, 1 h, 2 h, 3 h, more |
| ClockTrack | `#1A16171B` | `#2EFFFFFF` | The empty part of a clock |

`Tone.Neutral` maps to the greys, `Tone.Ink` to the ink pair.

**Contrast.** The three text roles meet WCAG AA (4.5:1) on Surface and AppBg in
both themes; check any new colour pair before adding it. Danger text in dark
mode is just under 4.5:1, so it's only used for short, semibold words.
Colour is never the only signal: chips carry words, verdicts carry
"Block"/"Allow".

## Type

One family, **Inter** (bundled), and **Cascadia Mono** (falling back to
Consolas) for literal values. Every piece of text is a `TextLine` with a `TextStyle`:

| Style | Size | Weight | Default colour | Use |
|---|---|---|---|---|
| Display | 60 | Semibold | Primary | One huge number per screen (the strict-mode countdown) |
| Title | 26 | Semibold | Primary | Page titles |
| Heading | 20 | Semibold | Primary | Top-bar titles, an emphatic statement in an ink card |
| Subheading | 16 | Semibold | Primary | Card and section titles |
| Label | 15 | Semibold | Primary | Names on tiles, the app name |
| Body | 14 | Regular | Primary | Running text |
| Strong | 14 | Semibold | Primary | Names in lists and rows |
| Small | 13 | Regular | Secondary | Help text under a title |
| Caption | 12.5 | Regular | Tertiary | Fine print, metadata |
| CaptionStrong | 12.5 | Semibold | Secondary | A short status next to content; field labels |
| Overline | 11.5 | Semibold, upper case, +0.7 tracking | Tertiary | Section labels inside cards |
| Code | 12.5 | Regular, mono | Primary | Addresses, paths, query conditions |

Mixed emphasis inside one line uses phrases: `TextLine.Rich(style, tone, new Phrase("5 of 5 min", Strong: true), new Phrase(" used"))`.

## Spacing

A 4-pixel grid. Margins, paddings and gaps use `Space`:

| Token | px | Typical use |
|---|---|---|
| `Space.S0` | 2 | Title to caption inside a compact row |
| `Space.S1` | 4 | Title to help text; icon to label in tight places |
| `Space.S2` | 8 | Items in a row of chips or buttons |
| `Space.S3` | 12 | Elements inside a card section |
| `Space.S4` | 16 | Between cards in a column; card padding (compact) |
| `Space.S5` | 20 | Between sections in a card; default card padding |
| `Space.S6` | 24 | Between page blocks; gap between the two panes; roomy card padding |
| `Space.S8` | 32 | Page padding; spacious card padding |
| `Space.S10` | 40 | Page side padding next to the sidebar |

## Shape

| Radius | px | Use |
|---|---|---|
| Small | 8 | Chips, menu items, segments |
| Control | 10 | Buttons, segmented tracks, list rows |
| Inset | 12 | Insets, wells, banners, callouts, choice tiles |
| Card | 14 | Cards, pick tiles |
| Feature | 16 | Ink cards |

Fully round: switches, dots, round toggles, status chips.

## Elevation

Flat by default: hierarchy comes from surface colour and hairlines, not shadows.
Two small shadows exist: **Raised** (the selected segment) and **Knob** (switch knob).

## Icons

Lucide line icons on a 24-unit grid, 2-unit round stroke, coloured by
`TextTone`. Sizes: 14 (inside small controls), 16 (buttons, rows), 18 (default,
navigation), 22–28 (empty states). The set is `Glyph`; names say what the icon
shows (`List`, `Gear`), not what it's for. Add new ones from Lucide only.

## Layout

- **Two panes stay side by side.** There is no stacked or mobile layout. The
  main window's minimum size (1000 × 600) is chosen so every two-pane screen fits.
- **Sidebar screens** (`SidebarLayout`): 236-wide sidebar, content padded 40/32.
- **Full-window screens** (`FullPageLayout`): a `TopBar`, then content padded 32
  and capped at 1280 wide.
- **Tiles** reflow by count per row (`Layout.Tiles`); that is a grid, not a
  responsive breakpoint.
- One `Primary` button per screen or card. Destructive actions confirm in place
  (`Danger` → `DangerSolid`), never in a dialog.
- Free text only where text is truly free (names, addresses, search). Numbers
  use `Stepper`, times `TimeField`, choices `Segmented`, `RadioPill` or `Dropdown`.

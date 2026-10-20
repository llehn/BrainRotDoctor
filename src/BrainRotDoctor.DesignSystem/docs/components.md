# Components

Every public type of the design system. A test fails if one is missing here.
See the [gallery](gallery-light.png) for each in its variants and states.

Conventions shared by all controls:

- Interactive controls raise their own events (`Click`, `Changed`, `Toggled`,
  `Removed`, `Submitted`). A tap on a control never also triggers the control
  underneath it (e.g. the switch on a clickable card).
- Controls that rebuild on change (`RoundToggle`, `RadioPill`) report the
  *requested* state; the screen updates its model and rebuilds.
- Controls with live state (`Chip`, `Dot`, `Meter`, `StatusPanel`, `TextLine`)
  have `Set…` methods so screens update them in place on every status tick.

---

## Setup

### `DesignTheme`
Wires the system into an app. `builder.UseDesignSystem()` registers the fonts;
`DesignTheme.Install(app)` adds the Fluent base theme (accent pinned to our
teal), the palette and global control tweaks. `TextCulture` is the culture for
upper-casing overlines; set it when the UI language changes.

### `AppWindow`
Base class of every window: app background and the sans font. Screens' windows
derive from it; a test fails for any app window that doesn't.

## Tokens (see [foundations](foundations.md))

### `Space`
The spacing scale, `S0`–`S10`. The only values for margins, paddings and gaps.

### `Fonts`
`Sans` (Inter) and `Mono` (Cascadia Mono). Used inside the system only.

### `Glyph`
The icon set, as path data for `Icon`.

### `Swatch`
Brand colours for a `Monogram` (`Background`, `Foreground` hex). The one place
the app passes colours in, because a brand's colour is data, not styling.
`Swatch.Neutral` for anything without a brand.

## Text and icons

### `TextLine`
All text. `new TextLine(text, TextStyle, TextTone?)`; the tone defaults per style.
- `SingleLine`: one line, ending in "…".
- `Centered`: centres lines and block.
- `SetText`, `SetTone`, `SetPhrases` for live updates.
- `TextLine.Rich(style, tone, phrases…)` for mixed emphasis.

**Don't** set a font size or weight: pick a `TextStyle`. If none fits, add one.

### `Phrase`
A run of a rich `TextLine`: text, and whether it's strong.

### `Icon`
A line icon: `new Icon(Glyph.Lock, size, TextTone)`. `SetTone` for live updates.

## Buttons

### `PillButton`
A rounded button with a label and optional leading icon.

| `ButtonKind` | Looks | Use |
|---|---|---|
| Primary | Filled ink | The one main action of a screen or card |
| Secondary | Outlined | Other actions |
| Ghost | Text, secondary colour | "Back", low-key actions |
| Danger | Text, danger colour | A destructive action offered quietly |
| DangerSolid | Filled danger | The same action, being confirmed |
| Accent | Text, accent colour | Light constructive actions inside content ("Add a page") |
| Warn | Soft amber, outlined | Leaving a temporary state ("Resume") |

`ButtonSize`: `Small` (28, inline next to a label), `Regular` (40), `Large`
(48, the final action of a flow). States: hover (fades slightly), `Enabled =
false` (greyed, still shows its tooltip). `Stretch` fills the width. `Text` can
change in place (e.g. "Add 2 websites").

**Don't** put two Primary buttons side by side.

### `IconButton`
Square icon-only button; always has a tooltip. `IconButtonStyle.Outlined`
(40×40, a standalone action in a row, `selected` outlines it in ink while its
panel is open) or `Bare` (28×28, removing or dismissing the thing it sits on).

### `Pressable`
Makes any content clickable without changing its look (the header of an
expandable card). Buttons inside keep their own clicks.

## Selection

### `Segmented`
Mutually exclusive options on a grey track; the selected one is raised. Give a
`Segment` a `Tone` to fill it when selected (`Danger` "Block" / `Accent`
"Allow"). `stretch` fills the width; `height` 36 for use inside rows. Use for
2–4 short options. More options → `Dropdown`; options that wrap → `RadioPill`.

### `Segment`
One option of a `Segmented`: text and optional fill tone.

### `OnOffSwitch`
Switches one thing on or off, effective immediately. Accent when on, grey when
off; `Enabled = false` when it can't change (e.g. locked). Raises `Toggled`.

**Don't** use it for a choice that only applies after "Save": use `CheckOption`.

### `RadioPill`
One choice among several longer options that wrap onto lines. Selected: ink
outline, semibold.

### `RoundToggle`
A small round toggle with one or two letters, for picking members of a short
set (weekdays). Selected: filled ink. Always has a tooltip with the full name.

### `CheckOption`
A check box with optional content, for agreeing to something or choosing an
item for a later action. `onInk: true` on ink surfaces.

### `CheckRow`
An outlined row with a check box, title and caption; the whole row toggles.
For choosing items from a list.

### `ChoiceTile`
One of a grid of choices with a title and a caption (e.g. a duration). The
selected tile is filled ink.

### `PickTile`
A tile that can be added to a selection, with a round check in its corner.
`picked`: ink outline and filled check. `locked`: already chosen elsewhere —
greyed, grey check, not clickable.

## Input

### `TextField`
Free text only (names, addresses, search). `TextFieldStyle`: `Regular`,
`Large` (the main search of a screen), `Code` (mono, for literal values),
`Title` (frameless, heading size: an editable title in a `TopBar`). Options: a
leading `Glyph` or a literal `prefix` (e.g. the host before a path). Raises
`Changed` on every edit and `Submitted` on Enter.

**Don't** use it for numbers, times or choices.

### `Dropdown`
A choice from a long list (more than fit a `Segmented`, e.g. languages).

### `Stepper`
A whole number changed with − and +, never typed. Holding a button repeats and
speeds up (×5 after 8 steps, ×10 after 20), so long ranges stay quick. Raises
`Changed` after each step; screens update their model without rebuilding it.

### `TimeField`
A 24-hour time, picked from an hour and a minute column (5-minute steps),
never typed. Raises `Changed` once, when the picker closes with a new time.

## Surfaces

### `Card`
The container for a group of related content.

| `CardStyle` | Looks | Use |
|---|---|---|
| Surface | White / raised grey, hairline border | The default |
| Inset | Page background, no border | A summary or result inside a card |
| Well | Page background, outlined | A nested editing area inside a card |
| Outline | Outline only | An empty state waiting for content |
| Ink | Filled ink | The single most important block of a screen; content uses OnInk tones |

`CardPadding`: `None` (rows run edge to edge), `Compact` 16, `Regular` 20,
`Roomy` 24, `Spacious` 32. Subscribing to `Click` makes the whole card
clickable (hand cursor, stronger border on hover).

### `Band`
A full-width row inside a `Card` with `CardPadding.None`; rows are separated by
`Divider`s. `shaded` sets one apart (a summary row, or the row matching a test).

### `Divider`
A hairline between rows or sections.

### `Banner`
A page-level message: icon, message, optional action at the end. `Tone.Warn`
for a temporary state (paused), `Tone.Danger` for errors. Build the message
with `Banner.Message(tone, phrases…)`.

### `Callout`
An outcome inside a card: a dot, a title in the tone's colour, an explanation.
`Danger` (blocked), `Accent` (allowed), `Neutral` (doesn't apply).

### `PlaceholderTile`
A dashed tile inviting to create something new, last in a grid of tiles.

## Status

### `Chip`
A short status in a pill, coloured by `Tone`. `SetState(text, tone)` in place.

### `Dot`
A small solid circle marking a state, coloured by `Tone`; `Tone.Ink` is the
ink-surface foreground.

### `Meter`
How much of something is used: a thin bar filled to a fraction. `Accent` while
there's room, `Danger` when spent; `onInk` for ink cards. `SetFraction`, `SetTone`.

### `Monogram`
A rounded square with one or two letters standing in for a logo, in a
`Swatch`. `Monogram.InitialsOf(name)` derives letters from a name.

### `InfoChip`
A compact label with a leading mark (a small monogram) and an optional muted
detail: what a card contains, at a glance.

### `SuggestionChip`
A clickable literal value offered as an example to try.

### `RemovableChip`
A literal value in a list, removed with its ×. Raises `Removed`.

### `StatusPanel`
The overall state of the app at a glance: dot, title, detail. `inverted` (ink)
for the most serious state.

## Maps of time

### `ClockFill`
An amount of time as clock faces: one circle per hour, the last one filling
like a pie. The fill gets stronger with the total (under half an hour, under
an hour, under two, under three, more), so a lot of time stands out without
reading a number. At most `MaxCircles` (12) circles. `SetTime`, `SetSize`.

**Don't** print the exact time next to it: put it in a tooltip, if anywhere.

### `TileMap`
Tiles filling all of their space, each with an area in proportion to its
weight (`Add(tile, weight)`, heaviest first): how a whole divides up, at a
glance. `Gap` between tiles (8 by default). Give it a fixed size or a parent
that doesn't scroll; it fills whatever it gets.

### `MapTile`
A tile of a `TileMap`: a coloured block with a title and a `ClockFill`, and
optionally a `TileMap` of smaller tiles inside. Title and clocks grow with the
tile; what doesn't fit is left out (the inner tiles first). `colour` picks one
of the tile colours, which only tell neighbours apart. Top-level tiles are
clickable (`Click`); `nested` ones sit inside another tile, lighter, with a
literal (mono) title. A `muted` tile is plain grey with an outline and not
clickable: an amount that stands apart from the rest (time not told apart by site).

### `Overlay`
A sheet over a dimmed screen: one thing looked at closely, with its actions
(pass a `Card`). Capped at 680 wide and scrolls when tall. A tap beside the
sheet or Escape raises `Dismissed`; the screen then rebuilds without it.

## Navigation and layout

### `NavItem`
An entry of the sidebar: icon, label, optional badge. Raised when current.

### `PopupMenu`
A menu dropping down from a button: `AddHeading`, `AddItem(label, hint, pick)`,
`AddDivider`, `AddNote`; `ShowAt(anchor)`. Picking an item closes it.
`AddContent` holds anything else (choices, a field, a button); close it with `Hide`.

### `TopBar`
The bar of a full-window screen: a way back (`Ghost` button with
`Glyph.ArrowLeft`), the title (`TextLine` Heading or a `Title` `TextField`), and
the actions on the right.

### `SidebarLayout`
The main layout: sidebar (navigation on top, `StatusPanel` at the bottom) and
the scrolling content. `scrolls: false` makes the content exactly the window's
height instead, for a screen that fills it (a `TileMap`).

### `FullPageLayout`
A full-window screen: `TopBar` and a scrolling body capped at 1280 wide.

### `PageHeader`
Page title, optional subtitle, and the page's actions on the right.

### `Layout`
Arrangement helpers: `Row` (side by side, centred), `Column`, `Columns` (two
panes, always side by side, one of fixed width), `Tiles` (equal tiles, count
per row follows the width), `Inactive` (fades content that's present but not
in effect, e.g. a switched-off item).

## Lists

### `InfoRow`
A read-only row on a sunken background: icon, title, caption and a status at
the end (e.g. a locked item and its live state).

### `Group`
A titled card of rows separated by dividers (a settings section).
`Group.Row(content)` pads free content as a row.

### `FormRow`
A row of a `Group`: title and help on the left, the control on the right.

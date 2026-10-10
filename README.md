<p align="center"><img src="docs/assets/icon-512.png" width="160" alt="BrainRotDoctor: a pink brain with spiral eyes and a worm peeking out"></p>

# BrainRotDoctor

**[Website](https://llehn.github.io/BrainRotDoctor/) · [Download for Windows](https://github.com/llehn/BrainRotDoctor/releases/latest/download/BrainRotDoctor.exe)**

BrainRotDoctor is a Windows application for limiting Instagram
doom-scrolling while retaining access to useful surfaces such as Direct
Messages and Stories.

## Status

The browser-independent **core product model** is implemented and tested
(`src/BrainRotDoctor.Core`). It is organized around the user's mental model
(ADR-007, modelled on AppBlock's "Zeitpläne"): a **rule** is *what* to block plus
*when*. Each rule combines:

- **What:** a list of sites picked from a built-in **catalog** (Instagram Reels,
  Instagram feed, YouTube Shorts, TikTok, Facebook Reels, X, Reddit, …) by name,
  or added as a **custom** site (label + URL). Catalog picks are canonical
  (matching resolved from the catalog, shown by label only); custom sites store a
  URL with an "include subpaths" toggle. The same site may appear in several
  rules.
- **When:** a single combined condition —
  - an **allowance** of *N minutes per hour*, or **block completely** (no
    allowance);
  - active **all day** or within a **from–to** window (which may wrap past
    midnight);
  - on selected **days of the week**.

A rule blocks its sites whenever it is active and either set to block completely
or out of allowance for the current hour. Several rules may target the same site;
the site is blocked if any active rule blocks it.

The Windows enforcement app (`src/BrainRotDoctor.App`) is a modern **Avalonia**
desktop app (light/dark theming) that observes selected tabs in supported
browser windows through Windows UI Automation, accounts time against the rules,
and closes the selected tab when a rule blocks it — without stealing focus or
moving the window (ADR-010, ADR-011). It has been live-tested locally against
Firefox, Chrome, and Edge. A blocked tab does not vanish without warning: a short
animated scene plays in the screen corner (a doctor pulls a worm out of a brain)
and the tab closes the moment the worm pops out, about three seconds in (ADR-012).

The app starts with paired primary/watchdog roles by default. If either role is
killed, the other restarts it. Normal startup also registers the app in the
current user's Windows Run key. Strict mode can be activated from the UI for a
flexible duration (any number of minutes, hours, or days) behind a double opt-in;
while active, the app uses the locked configuration snapshot captured at
activation and ignores later config changes until the deadline passes.

The UI is **localized into 31 languages** (the 24 official EU languages plus
Russian, Ukrainian, Bosnian, Serbian (Cyrillic), Norwegian Bokmål, Turkish, and
Catalan). It follows the Windows display language by default — including the
one-click installer — and can be overridden in Settings (Automatic + a picker).
English (US) is the canonical fallback; day names and number formatting follow
the active culture. A test verifies every language has every key with matching
placeholders. (Translations are machine-assisted; have a native speaker review
before release.)

It ships as a single self-contained **`BrainRotDoctor.exe`** that doubles as a
one-click, no-admin installer (ADR-008): running the downloaded exe offers to
install it per-user under `%LOCALAPPDATA%\Programs`, register autostart, and add
an "Apps & features" entry. Uninstalling from Windows Settings runs the app's own
code and **refuses while strict mode is active** — manual file deletion still
works, but the easy three-click uninstall does not.

### Build and test

The solution targets .NET 10 (LTS) and the core has no OS dependency.

```sh
dotnet build
dotnet test
```

The rule set is configurable without recompiling; see
[config/default-config.json](config/default-config.json).

### Run the app

```sh
dotnet run --project src/BrainRotDoctor.App
```

The app loads `config/default-config.json` when run from the repository, starts
the paired watchdog process, and registers current-user startup. You can use
another ruleset and an optional diagnostic log:

```sh
dotnet run --project src/BrainRotDoctor.App -- --config path\to\config.json --log path\to\brainrotdoctor.log
```

For temporary tests that must not leave a watchdog, startup entry, or trigger the
auto-updater:

```sh
dotnet run --project src/BrainRotDoctor.App -- --no-install-prompt --no-watchdog --no-startup --no-update
```

Debug builds can show the worm scene on its own: `--scene-preview [count]` plays
it in the screen corner, and `--scene-frames out.png [--times 0.5,2.0,…]` renders
frames at the given seconds (twice real size) for side-by-side comparison with
the design. `--scene-clip <folder>` writes every frame (30 a second, transparent)
for the website's animation, and `--ui-snapshots <folder>` renders every app
screen with sample rules (the website's screenshots come from it).

The product icon is the worm scene's hypnotised brain, drawn at runtime by the
scene itself. The exe's own icon file is generated from it; regenerate it after
changing the scene's look:

```sh
BrainRotDoctor.exe --dump-icon src\BrainRotDoctor.App\Assets\BrainRotDoctor.ico
```

### Releases and silent auto-update

Installed builds update themselves silently (ADR-009). Every few hours the app
checks the latest [GitHub Release](https://github.com/llehn/BrainRotDoctor/releases),
verifies a signed `update.json` manifest and the new exe's SHA-256, and — only for
a strictly newer version (never a downgrade or reinstall) — stages the binary and
swaps it in, coordinating with the watchdog so the new build takes over without a
window. There is no update UI by design; applied versions are recorded to
`%LOCALAPPDATA%\BrainRotDoctor\update\history.log`.

Releases are continuous: every push to `master` is tested, and if green, built,
signed, and published automatically (version `1.0.<commit-count>`, always
increasing). A failing test means no release. There is nothing to tag or click —
push to `master` and installed apps update themselves within hours. See
[tools/release/README.md](tools/release/README.md) for the one-time signing-key
setup.

To produce the single-file distributable locally (one downloadable exe, no runtime
needed on the target):

```sh
dotnet publish src/BrainRotDoctor.App -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -o publish
```

Compression keeps it a single double-click exe (~71&nbsp;MB) that self-extracts at
launch; installed size is unaffected.

The resulting `publish\BrainRotDoctor.exe` is what a user downloads and
double-clicks. It self-installs (`--install`), and the registered uninstall runs
`--uninstall` (which refuses during strict mode).

Closing the window leaves enforcement running in the tray. The UI is navigation,
not a single dense screen: **Home** is a grid of rule cards (each showing its
condition summary, sites, and live status); clicking a card opens a **focused
edit screen** for just that rule (the grid is replaced, not crowded alongside).
Strict mode and settings are their own screens; the theme choice
(system / light / dark) lives in Settings rather than the chrome.

Editing matches the user's mental model: create a rule, choose its sites by
label + URL, and set when it applies. There are no hosts, path prefixes, regexes,
or ids to manage — the URL is parsed for you, with an "include subpaths" toggle
per site.

Saving is explicit: the edit screen validates through the same core loader used
at startup, persists to the active config file, reloads enforcement, and returns
to the grid; Cancel discards. While strict mode is active it becomes the landing
screen and the existing rules are locked, using the config snapshot captured at
activation.

## Architecture Decision Records

Architecture Decision Records (ADRs) preserve not only the selected design but
also rejected alternatives and their rationale. Later work must not reverse an
accepted decision silently. A change requires a new ADR that explicitly
supersedes the earlier record.

### ADR-001: Strict Mode Prioritizes Commitment Enforcement

- **Status:** Accepted
- **Date:** 2026-06-05

#### Context

The product exists because an ordinary browser blocker or extension can be
disabled within seconds. The user may have local administrator access, but an
impulsive decision made during an active commitment must not provide a simple
way to stop, disable, reconfigure, or remove the blocker.

This is the central product requirement, not optional hardening to add after
the core application works.

The application is intended eventually for public distribution, but
distribution conventions must not silently weaken strict mode.

#### Decision

BrainRotDoctor will provide an optional strict mode with these rules:

- While a strict-mode commitment is active, the product provides no emergency
  bypass, recovery shortcut, delayed escape, or ordinary uninstall path.
- Changes that weaken enforcement cannot take effect during the active
  commitment.
- Enforcement must restore itself after ordinary crashes, process
  termination, logout, and reboot.
- The architecture must actively investigate resistance to administrator-level
  stop, disable, tampering, replacement, startup removal, and uninstall
  attempts.
- During an active commitment, configuration changes that weaken enforcement
  are rejected, not queued for later application.
- Configuration changes that strengthen enforcement may be allowed during an
  active commitment if they do not introduce a bypass.
- Strict mode supports multiple planned end-condition modes, including fixed
  duration, recurring schedule, and password-based disabling.
- "Until reinstall/wipe" is not a product mode.
- Non-strict mode may support normal disabling, reconfiguration, and complete
  uninstall.
- Before activation, the UI must explain strict mode's consequences clearly.
  That disclosure does not create a bypass after activation.

The exact self-protection mechanism is not decided by this ADR. Candidate
designs, including mutually supervising processes and Windows service
controls, must be prototyped and tested. The initial design needs enough
friction to defeat the expected impulsive bypass behavior; it does not need
maximum hardening before real use demonstrates that more is necessary.

#### Rejected Alternatives

**Always provide a documented recovery and removal path**

Rejected because any recovery path available to the same user during strict
mode is also an immediate bypass. That directly contradicts the reason for the
product.

**Emergency unlock or emergency disable**

Rejected because the software cannot determine whether a claimed emergency is
genuine or an impulsive attempt to evade a working commitment.

**Delayed escape available after strict mode starts**

Rejected as a general recovery mechanism. A strict-mode commitment is governed
by the end conditions fixed before activation; it must not acquire a new
escape path after activation.

**Queue weakening configuration changes for after the commitment ends**

Rejected. During an active commitment, weakening changes are rejected rather
than queued. Queuing creates additional state and product behavior that is not
needed for the goal.

**Strict mode until reinstall or Windows wipe**

Rejected as a product mode. The product should offer deliberate strict-mode
end conditions such as fixed duration, recurring schedule, and password-based
disabling. Reinstalling or wiping Windows is not part of the intended product
workflow.

**Safe-mode recovery documented as a product feature**

Rejected. Safe Mode, offline modification, external media, and similar
administrator techniques are threats to evaluate and harden against, not
intentional recovery features.

**A deliberately broken or fake uninstaller as the complete design**

Rejected as insufficient, not because strict mode must remain easy to remove.
A fake uninstaller alone does not provide robust self-protection and can be
bypassed through services, processes, files, registry entries, or replacement
installation. The installer and self-protection design must enforce the active
commitment coherently. Outside strict mode, uninstall should be complete.

**Accept administrator removal as unavoidable and stop hardening there**

Rejected. Administrator-level bypasses are design threats and testing targets.
The project must reduce them enough to meet the practical friction goal instead
of treating them as pre-approved behavior. If actual use reveals that a bypass
is being used, hardening that path becomes new product work.

**Publish known bypass instructions**

Rejected because supplying circumvention steps directly reduces the friction
the product is designed to create. Internal testing may record bypass details,
but public product documentation must not provide a guide for disabling active
strict mode.

#### Failure Scenarios Considered

The following scenarios were considered as arguments for an emergency recovery
path and rejected as such:

- Blocking all of a browser instead of only prohibited Instagram surfaces.
- Affecting unrelated applications, websites, browser windows, or tabs.
- Continuous crashes or restart loops.
- Windows instability or interference with login.
- Excessive CPU, memory, disk, or startup impact.
- Incompatibility after Windows or browser updates.
- Incorrect persistence beyond the configured commitment end.
- Removing the application before selling or transferring the computer.

These are production-quality, architecture, release, and testing obligations.
They do not justify a strict-mode bypass. Device transfer is outside the
product scope; wiping the Windows installation is an acceptable preparation
for transfer.

This decision accepts residual malfunction risk rather than deliberately
weakening strict mode. Testing cannot prove the absence of every defect, so the
release process must be conservative and the enforcement surface narrowly
bounded.

#### Consequences

- Strict mode carries a stronger operational risk than conventional desktop
  software, and activation must communicate that fact.
- Test coverage must include clean virtual machines, browser and Windows
  updates, long-running tests, crash loops, false-positive prevention, and
  adversarial removal attempts.
- Staged releases and compatibility qualification are required before broad
  distribution.
- Installer, updater, configuration, supervisor, and monitor behavior must all
  preserve an active commitment.
- A malfunction during strict mode is not solved by an intentionally provided
  emergency bypass.
- Self-protection hardening is incremental. The initial release targets enough
  friction for the observed use case; bypasses actually used in practice drive
  further hardening.
- Public documentation explains strict-mode consequences without publishing
  circumvention instructions.

### ADR-002: Cross-Browser Native UI Automation

- **Status:** Accepted
- **Date:** 2026-06-05

#### Context

The original concept focused on Firefox, but the product requirement is to
support browsers generally on Windows. Browser extensions can be disabled
quickly and therefore do not satisfy the enforcement requirement.

#### Decision

- Native Windows UI Automation is the primary browser-observation mechanism.
- Browser-specific behavior is isolated behind adapters.
- Supported browsers and versions are determined by prototypes and
  compatibility tests.
- Browser extensions are not an enforcement dependency.

#### Rejected Alternatives

**Firefox-only support**

Rejected because the intended product must work across supported Windows
browsers.

**Browser extension as the authoritative enforcement layer**

Rejected because the user can disable an extension too quickly. It recreates
the weakness that BrainRotDoctor is intended to solve.

**Browser extension backed by a native component**

Rejected as the primary architecture because disabling the extension would
still disable browser detection. Native enforcement must remain effective
without an extension.

#### Consequences

- Every supported browser requires accessibility-tree research, an adapter,
  compatibility fixtures, and update testing.
- UI Automation changes are a major compatibility risk and require staged
  qualification.
- Cross-browser support increases the system-test matrix and release cost.

### ADR-003: Least Privilege Is Not a Product Requirement

- **Status:** Accepted
- **Date:** 2026-06-05

#### Context

An earlier plan introduced least privilege as a product principle even though
it was not present in the product requirements. Strict-mode enforcement may
require privileges or Windows integration that cannot be selected in advance.

#### Decision

Required functionality has priority over security tradeoffs. BrainRotDoctor
must perform its intended blocking and commitment-enforcement behavior; a
secure design that cannot perform that behavior provides no product value and
is rejected.

Security remains required, but its role is to protect the functioning system.
Security controls must be designed within the constraints imposed by the core
functional requirements. When a proposed control prevents or materially
weakens a core requirement, the response is to find another security design,
accept and document the residual risk if necessary, or reject the control. The
functional requirement is not silently removed to satisfy the control.

Accordingly, least privilege is not a product requirement and must not
constrain the architecture before self-protection mechanisms are evaluated.
Components may use the privileges required to implement reliable enforcement.
The selected privilege model must still be explicit, reviewed, tested, and
protected against avoidable Windows security vulnerabilities.

#### Rejected Alternative

**Mandate least privilege as a product principle**

Rejected because it was an invented requirement and could prematurely rule out
effective strict-mode designs.

**Choose a secure design that weakens or prevents required enforcement**

Rejected because non-functioning secure software does not solve the product
problem. Security does not justify removing the behavior the application
exists to provide.

**Treat security as optional because functionality has priority**

Rejected. The priority ordering resolves conflicts; it does not remove the
requirement to make the functioning implementation secure.

#### Consequences

- Privilege level is an engineering decision driven by enforcement needs.
- Architecture reviews must evaluate functionality first, then secure the
  viable functional design.
- Security findings may require redesign, mitigation, or explicit residual-risk
  acceptance, but cannot silently override core product requirements.
- Security review remains mandatory, especially for elevated services,
  installers, IPC, file permissions, and update mechanisms.

### ADR-004: Doom-Scrolling Surfaces Use Recurring Budgets

- **Status:** Accepted
- **Date:** 2026-06-06

#### Context

The product is intended to prevent doom-scrolling behavior, not to block whole
websites. A site can contain both useful functionality and addictive surfaces.
For example, the same service may provide messages, account pages, specific
linked content, feeds, reels, shorts, and endless recommendations.

Viewing one short video or reel is not automatically doom-scrolling. The
problem is getting pulled into the repeated scrolling loop.

#### Decision

BrainRotDoctor blocks configured doom-scrolling surfaces using recurring time
budgets.

- URL/page rules identify configured doom-scrolling surfaces.
- Budget groups define allowed time and reset interval.
- Rules and budgets have an N-to-M relationship: one rule may consume multiple
  budgets, and one budget may be shared by multiple rules.
- Time counts only while a configured budget-consuming surface is active.
- Once a budget is exhausted, matching surfaces assigned to that budget are
  blocked, for example by closing the tab.
- The broader service remains usable unless another configured rule matches
  the current page.
- The ruleset must be configurable without recompiling or redistributing the
  application.

Example: YouTube Shorts, Instagram Reels, Facebook Reels, and TikTok may share
a `short-form-video` budget with a default such as `2 minutes every 1 hour`.
When that budget is exhausted, those surfaces are blocked until reset. Normal
YouTube pages, Instagram DMs, account pages, and specific linked content
remain usable unless explicitly configured otherwise.

When a budget group is exhausted:

- Every on-screen supported browser window is checked (ADR-010).
- If a window's selected tab/current page matches a rule assigned to the
  exhausted budget, that selected tab is closed.
- The whole browser window is not closed unless the browser itself closes the
  window because the selected tab was its last tab.
- If the same surface is reopened before the budget resets, it is closed again.
- If multiple exhausted budget groups apply to the same selected tab, closing
  it once is sufficient.
- A non-selected matching tab is not closed until it becomes the selected
  tab/current page of a browser window.

#### Rejected Alternatives

**Block entire services**

Rejected because it would prevent valuable social and media functionality that
the product is supposed to preserve.

**Apply the budget to all use of a service**

Rejected because the behavior to limit is doom-scrolling, not every interaction
with a website.

**Always block short-form content with no budget**

Rejected because a single short video or reel sent by another person can be
intentional social interaction. The target is the addictive loop, not the media
format by itself.

**Hard-code the list of supported sites**

Rejected because new doom-scrolling surfaces can appear after release. The
ruleset must be configurable.

#### Consequences

- The core data model must represent rules, budget groups, and their N-to-M
  assignments.
- The monitor must evaluate the current page against configured rules rather
  than treating a whole domain as blocked.
- Default configuration matters, but it is not the product boundary.
- Tests must cover shared budgets, separate budgets, reset intervals, and
  service pages that should remain unaffected.
- Tests must cover selected-tab closure, repeated closure before reset, and
  non-selected matching tabs becoming selected after exhaustion.

### ADR-005: Selected Tabs of Open Windows Consume Budget

- **Status:** Accepted; amended by ADR-010 (only on-screen windows count)
- **Date:** 2026-06-06

#### Context

The product targets the active doom-scrolling surface. A browser window has one
selected tab/current page. A non-selected tab is not the current page of that
window and is not the surface being watched or scrolled.

Fancy activity detection can create bypasses, false assumptions, and
implementation complexity.

#### Decision

Budget accounting is based on the selected tab/current page of each open
supported browser window:

- Every open supported browser window is monitored.
- For each window, only that window's selected tab/current page is evaluated.
- A non-selected tab does not consume budget.
- A selected tab in a background browser window does consume budget.
- Time is counted once per real elapsed interval for each affected budget
  group.
- Time is not multiplied by the number of matching windows.
- If multiple budget groups have selected tabs/current pages open at the same
  time, each affected budget group consumes the same elapsed interval.
- System sleep and screen lock may pause accounting because the user cannot
  continue doom-scrolling in those states.
- System idle is not used to pause accounting.
- In case of doubt, prefer blocking over adding complex detection. Early
  iterations should keep the model simple and harden it before adding nuanced
  activity detection.

#### Rejected Alternatives

**Currently focused tab only**

Rejected because selected tabs in other open browser windows are also current
pages of those windows.

**Currently focused browser window only**

Rejected because background browser windows should still count.

**All tabs in all windows**

Rejected because a non-selected tab is not the current page of its browser
window and is not the doom-scrolling surface being watched or scrolled.
Counting it would punish having a URL parked in a tab, which is not the
behavior the product targets.

**Multiply budget usage by number of matching windows**

Rejected because doom-scrolling is one user's behavior over real elapsed time,
not a per-window resource meter.

**Pause for system idle**

Rejected for the initial model because idle detection adds complexity and can
be wrong. The guiding rule is to prefer blocking in uncertain cases.

#### Consequences

- The monitor must track the selected tab/current page of every open supported
  browser window, not only the focused window.
- Tests must cover non-selected tabs, background windows, multiple matching
  windows, and multiple simultaneous budget groups.
- Users may lose budget while a matching selected tab is open in a background
  window. This is accepted because that tab is still the current page of that
  window.

### ADR-006: Core Model Implementation

- **Status:** Accepted
- **Date:** 2026-06-20

#### Context

The first implementation step (plan §5.2 and the First Milestone) is the
browser-independent core: rules, budget groups, time accounting, and the
blocking decision. ADR-004 and ADR-005 fix the behavior; this ADR records the
concrete decisions made while implementing it in C# (`BrainRotDoctor.Core`,
targeting .NET 8). It does not change any prior decision.

#### Decision

- **Language/runtime.** The core library targets `net10.0` (the current LTS) and
  has no Windows or UI Automation dependency, so it stays deterministic and
  testable on any platform. The eventual enforcement layer (UI Automation,
  service, self-protection) builds on top of it.
- **Pure, clock-injected engine.** `BudgetEngine.Tick(windows, now)` is the only
  entry point. The caller supplies the browser snapshot and the current instant
  every tick; the engine owns no clock and no OS state, which makes the whole
  model reproducible in tests.
- **Selected-tab-only input.** The engine consumes a list of
  `BrowserWindowState { WindowId, Url }` — one entry per open window, its
  selected tab/current page only (ADR-005). Non-selected tabs are simply not
  represented.
- **Tumbling budget windows.** A budget resets on fixed tumbling windows of
  length `ResetInterval` aligned to an `Anchor` (default Unix epoch, UTC).
  Tumbling rather than rolling windows keep accounting deterministic and cheap,
  matching the product's preference for the simplest model that works. Time that
  straddles a window boundary is only charged to the window it actually falls
  in, so a fresh allowance is never over-charged.
- **Charge once per affected budget.** Each tick computes the union of budgets
  consumed across all matching selected tabs and charges the elapsed interval to
  each affected budget exactly once, never multiplied by the number of windows
  (ADR-005).
- **Sleep/lock via a gap clamp, not power detection.** An inter-tick gap larger
  than `MaxAccountedGap` (default 5s, suited to ~1s polling) is clamped, so a
  sleeping or locked machine does not drain the budget. This realizes "system
  sleep and screen lock may pause accounting" with no explicit power or idle
  detection, honoring the "prefer the simple model" guidance.
- **Configurable rule set.** Rules and budgets load from JSON
  (`ConfigurationLoader`, sample at `config/default-config.json`) so new
  doom-scrolling surfaces can be added without recompiling (ADR-004). A built-in
  `DefaultConfiguration` provides a starting point and the shipped JSON is
  guarded by a test.

#### Rejected Alternatives

- **Rolling/sliding budget windows.** Rejected for the first model: more state
  and complexity than tumbling windows, with no clear product benefit yet.
- **Explicit idle/power-state detection to pause accounting.** Rejected per
  ADR-005; the gap clamp covers the real case (sleep/lock) without extra
  detection that can be wrong or create bypasses.
- **An engine that reads the wall clock itself.** Rejected because it would make
  the core non-deterministic and hard to test; the clock is injected per tick.

#### Consequences

- The core has thorough unit coverage (URL matching, tumbling windows,
  configuration validation, the loader, and the full accounting/closing
  behavior including shared budgets, multi-window non-multiplication, reset,
  reopen-before-reset, and the sleep clamp).
- Persistence of budget state across restarts is not yet implemented; it is
  needed before strict mode and will be a later step. The runtime state is
  deliberately small and serializable to make that straightforward.
- The browser observation layer must produce `BrowserWindowState` snapshots; it
  is the next workstream (plan §5.3).

### ADR-007: Rule Model (What + When), Navigation UI, and Avalonia

- **Status:** Accepted
- **Date:** 2026-06-21
- **Supersedes:** the configuration vocabulary and rule/budget cardinality of
  ADR-004, and the Windows Forms UI of the first enforcement app.

#### Context

The first configuration UI exposed the internal model directly — rule id, name,
host, path prefixes, path regex, budget id/name/allowance/reset, and a per-rule
budget checklist. That leaks implementation detail and does not match how a user
thinks. The product owner uses AppBlock (Android) and wants its model: the
primary entity is a **Zeitplan/rule** — *what* to block plus *when* — where the
"when" is a combined condition (an allowance, a time-of-day window, and selected
weekdays). The owner also rejected a single dense screen and a master-detail
split (no reason to see other rules while editing one), and rejected a
harsh black/white "dark" theme and a prominent theme switch.

#### Decision

- **One entity: `Rule` = what + when.** A rule owns a list of sites and a single
  combined condition. This replaces ADR-004's separate budget-groups and rules
  and their N-to-M assignment. Sites are not exclusively owned: the same URL may
  appear in several rules, and it is blocked if any active rule blocks it.
- **The "when" condition.** Each rule has: an optional **allowance** of *N
  minutes per hour* (null ⇒ *block completely*); an **active window** (all day,
  or a local `from`–`to` range that may wrap past midnight); and a set of
  **days**. The allowance refills on each clock hour. "Per hour" is deliberately
  the only granularity — a daily allowance is psychologically weaker (spend it
  once and you are done), so it was left out.
- **URL-first sites.** The user types a URL; `SiteUrl.ToPattern` compiles it into
  a `UrlPattern`. A bare host matches the whole site; a path matches that path
  and, with "include subpaths", everything beneath it; a root address with
  subpaths off matches only the front page (so Instagram DMs stay reachable).
  Hosts, prefixes, regexes, and ids are never shown.
- **Navigation UI, not one screen.** Home is a grid of rule cards; selecting one
  replaces the grid with a focused edit screen for that rule alone. Settings and
  strict mode are their own screens. Saving is explicit (PC convention); Cancel
  discards.
- **Strict mode placement.** When strict mode is active it is the landing screen
  and existing rules are locked (the detailed strict-mode UX is deferred).
- **Theme.** A soft, layered light/dark palette (theme dictionaries), not raw
  Fluent defaults. The default follows the OS; the choice lives in Settings, not
  in the chrome (a theme switch is needed only rarely).
- **UI framework.** Rebuilt in Avalonia, replacing Windows Forms. `UseWPF`
  remains enabled solely to reference the UI Automation client assemblies; no
  WPF windows are created.

#### Rejected Alternatives

- **Expose the raw rule/budget fields.** Rejected: it surfaces internal detail
  and was the core complaint.
- **Separate "allowance" and "schedule" entities the user picks between.**
  Rejected: AppBlock's model is one rule with a combined condition; forcing the
  user to choose a machinery type up front inverts that.
- **Per-day allowance.** Rejected for now as weaker against impulsive use; may be
  added later.
- **Master-detail in one window / a single dense scrolling screen.** Rejected:
  there is no value in seeing other rules while editing one, and the owner should
  never have to scroll for a handful of rules.
- **A prominent theme toggle in the header.** Rejected: rarely needed; it belongs
  behind Settings. Default follows the OS.
- **Stay on Windows Forms / move to WPF.** Rejected: a modern, themeable UI was a
  hard requirement.

#### Consequences

- The JSON schema is `rules[]` with `name`, optional `allowanceMinutes`,
  `allDay`/`from`/`to`, `days`, and `sites[]` (`label`, `url`, `includeSubpaths`).
  The shipped `config/default-config.json` and the editor round-trip use it; the
  editor writes camelCase. Old-schema files do not load and fall back to the
  built-in defaults.
- Core and app tests were rewritten for the rule model, with new coverage for
  windowed/wrapping active times, block-completely vs allowance, hourly refill,
  and URL→pattern compilation.
- Persisting rule state across restarts is still future work (as in ADR-006), as
  is the detailed strict-mode screen.

### ADR-008: Per-User Self-Installer with Strict-Mode-Aware Uninstall

- **Status:** Accepted
- **Date:** 2026-06-21

#### Context

The app needs an easy distribution path for non-technical users: download one
file, double-click, done — without administrator rights or a runtime prerequisite
(SmartScreen warnings are expected and accepted). It also needs to extend the
strict-mode friction (ADR-001) to uninstallation: while a commitment is active,
the ordinary "three-click" uninstall from Windows Settings must not work, though
deliberate manual removal of files remains possible (that is acceptable friction,
not a bypass guarantee).

#### Decision

- **One self-contained exe that is also the installer.** The product is published
  as a single-file, self-contained `BrainRotDoctor.exe` (no .NET runtime needed
  on the target). Run from outside the install location it shows a one-click
  "Install BrainRotDoctor?" window; run from the install location it is the app.
- **Per-user, no admin.** Installs to `%LOCALAPPDATA%\Programs\BrainRotDoctor`,
  writes the `HKCU\…\Run` autostart value, and registers an
  `HKCU\…\Uninstall\BrainRotDoctor` entry so it appears in Apps & features. All
  HKCU + LocalAppData, so no elevation is required.
- **Uninstall is our code.** The registered `UninstallString` re-invokes the exe
  with `--uninstall`. That path checks strict mode first: if active it shows a
  message and exits non-zero without removing anything (so Windows still lists the
  app). Otherwise it stops the running instances, removes the registry entries and
  app data, and schedules deletion of the install directory after exit (a small
  retrying batch file, because a single-file exe stays briefly locked after it
  exits).
- **No third-party installer toolchain.** Keeping install/uninstall in the app
  avoids an external dependency (e.g. Inno Setup) and lets the strict-mode check
  live in the same code as the rest of the product.

#### Rejected Alternatives

- **MSIX / Microsoft Store packaging.** Rejected: uninstall is OS-managed and
  cannot be refused, which defeats the strict-mode requirement; it also pushes
  toward signing/elevation flows.
- **Inno Setup / NSIS setup.exe.** Workable (per-user, and uninstall can be
  aborted from installer script), but adds a build-time toolchain and splits the
  strict-mode logic into installer script. Deferred unless a richer setup UI is
  needed.
- **Per-machine install (Program Files).** Rejected: requires admin, which the
  product explicitly should not.

#### Consequences

- The strict-mode-aware uninstall increases real friction without claiming to be
  unbypassable, consistent with ADR-001.
- Install/uninstall logic is covered by unit tests (against a temporary directory
  and registry subkey) and was verified end-to-end with the published exe:
  install writes files + autostart + Apps & features entry; uninstall is refused
  (exit 1, entry intact) while a strict-mode marker is present and succeeds
  (entry + autostart removed, files self-deleted) once it is cleared.
- The distributable is large (~150 MB) because it is self-contained including
  WPF (referenced only for the UI Automation client) and Avalonia; slimming this
  is possible future work.

### ADR-009: Silent, Signed Auto-Update from GitHub Releases

- **Status:** Accepted
- **Date:** 2026-06-23

#### Context

Because new doom-scrolling surfaces appear over time and enforcement bugs must be
fixable, installed builds need to update without the user's involvement. A
self-protection tool that can rewrite its own binary is also an attack surface
against the user themselves (a tampered or downgraded build is a strict-mode
bypass), and the existing watchdog (ADR-008 lineage) respawns the primary within
~650 ms, so the running, file-locked exe cannot simply be replaced.

#### Decision

- **GitHub Releases as the channel.** Each release publishes `BrainRotDoctor.exe`
  plus a small `update.json` manifest (version, asset name, SHA-256, size) and an
  `update.json.sig`. Releases are continuous — every push to `master` is tested,
  then (if green) built, hashed, manifest-written, signed, and published, with a
  monotonic `1.0.<commit-count>` version. The manual `publish/` flow is retired.
- **Signed manifest + hashed binary.** The manifest is signed with an ECDSA P-256
  key (built-in `System.Security.Cryptography`, no new dependency) whose public
  half is embedded in the app and private half is a CI secret. The app trusts a
  build only if the signature verifies and the downloaded exe's SHA-256 matches
  the signed manifest. Updates are **forward-only** (strictly greater version),
  blocking downgrade/replay.
- **Completely silent, always on.** A background timer checks every few hours;
  failures are swallowed and retried. Updates apply even during strict mode
  (they are forward fixes, not a user escape). No user-facing UI; applied
  versions are logged locally for a possible future "what's new" view.
- **Watchdog-aware swap with recovery.** The verified binary is staged, then runs
  itself in an `--role apply-update` mode that raises a short-lived, freshness-
  bounded marker. The primary and watchdog see it, schedule a safety-net relaunch,
  and exit; the applier backs up the old exe, copies the new one in, re-verifies
  its hash (rolling back on failure), then relaunches. The safety net guarantees
  protection returns even if the applier is killed mid-swap.

#### Rejected Alternatives

- **Velopack/Squirrel.** Mature, but imposes its own install layout and a large
  dependency that conflicts with the bespoke per-user installer and watchdog
  (ADR-008); a small custom updater fits the existing model better.
- **Notify-and-prompt or manual "check for updates".** Rejected: a user could
  decline a fix, and most never click. Silence matches the product's intent.
- **HTTPS/version check without signing.** Rejected as too weak for a tool that
  rewrites its own binary; signing prevents a tampered build from being applied.

#### Consequences

- The updater is self-protection-conscious: the stand-down requires a fresh,
  authentic marker rather than a signal anyone could raise, and a hard-killed
  applier is recovered by the safety net or next-login autostart (a brief, bounded
  outage rather than an easy permanent bypass).
- Self-protection hardening remains incremental: the marker mechanism is friction,
  not a guarantee against a determined local user (consistent with ADR-001/003).
- Verifier, manifest parsing, and the no-downgrade/staging decision are unit
  tested with an ephemeral key; the CI signer and embedded public key are
  cross-checked to use the same DER/SHA-256 format.

### ADR-010: Only On-Screen Browser Windows Count

- **Status:** Accepted
- **Date:** 2026-10-08
- **Amends:** ADR-004 (closing), ADR-005 (accounting)

#### Context

ADR-005 counts the selected tab of *every* open browser window. In practice that
included minimized windows and windows on other virtual desktops. This was
inconsistent with ADR-005's own reasoning: a non-selected tab does not count
because it is not the surface being watched, yet a minimized window — equally
unwatchable — did count.

It also made closing disruptive. To close a tab in such a window the app had to
bring it back first: restoring a minimized window also un-snapped snapped windows
(Windows Snap layouts), and activating a window on another virtual desktop
switched the user to that desktop.

#### Decision

A browser window takes part in accounting and closing only while it is on the
user's current screen:

- **Counts:** shown, not minimized, and on the current virtual desktop — whether
  focused, in the background, snapped, or covered by other windows.
- **Does not count:** minimized windows and windows on another virtual desktop.
  Their selected tab neither consumes budget nor is closed. When the window comes
  back on screen it counts again; if its budget is exhausted it is closed on the
  next check.
- Closing never restores, moves, or re-sizes a window. It only acts on windows
  already on screen, so snapped/maximized layouts and the current desktop are
  left untouched.

All other ADR-005 rules (selected tab only, background windows count, time not
multiplied by windows, prefer blocking over complex detection) are unchanged.

#### Rejected Alternatives

**Keep counting minimized/other-desktop windows, and only restore when needed**

Rejected because it keeps the inconsistency with non-selected tabs and still has
to restore or switch desktops to close, which disrupts the user's layout.

**Count only windows that are actually unobstructed**

Rejected because occlusion detection is fragile and complex; ADR-005 prefers the
simple model and blocking in doubt. A covered window is one click away and still
counts.

#### Consequences

- Minimizing a window or moving it to another desktop pauses its consumption.
  Accepted: it cannot be scrolled there, and audio from a background tab is
  already not counted either.
- Closing is less intrusive: no un-snapping, no desktop switching.
- A window minimized between observation and closing is skipped and re-evaluated
  on the next check.

### ADR-011: Close Tabs Without Stealing Focus

- **Status:** Accepted
- **Date:** 2026-10-08

#### Context

The app closed a tab by bringing the browser window to the front and typing a
global Ctrl+W. Windows' foreground lock refuses that request from a background
program whenever another app is in front, so for any browser window that was not
already in front, Ctrl+W landed in whatever the user was using (closing their
current tab or editor file). Forcing the window forward instead would steal the
user's focus mid-typing, which is unacceptable.

Live tests on Firefox, Chrome, and Edge with background windows showed:

- Posting plain key messages to the browser window does nothing: browsers read
  modifier keys from their own keyboard state, not from the message.
- Posting Ctrl+W while the browser thread's keyboard state marks Ctrl as held
  closes the tab in all three, with no focus change and no window movement.
- Pressing the tab's own close button through UI Automation also works in all
  three, but every browser then brings its window to the front by itself; focus
  can be handed back within ~20–75 ms.

#### Decision

- **Primary:** post Ctrl+W directly to the target browser window while its thread
  sees Ctrl as held. Ctrl is held only until the window reacts (title change or
  window closed), capped at 250 ms, and only the faked bits are released.
- **Fallback:** if a keyboard attempt showed no effect and the same window still
  shows a blocked page on the next check, press the selected tab's close button
  via UI Automation (matched by class, not localized name) and immediately hand
  focus back to the window the user was using.
- No global keystrokes are ever sent, so nothing can land in another app.

#### Rejected Alternatives

**Bring the window forward, then type Ctrl+W (previous behavior)**

Rejected: fails under the foreground lock and misdirects Ctrl+W into the user's
app; forcing it through would steal focus.

**Close button only**

Rejected as the primary path because every press makes the browser grab focus,
even if only for a moment.

**Posting plain key messages**

Rejected: browsers ignore the Ctrl modifier, so it does nothing.

#### Consequences

- Closing a tab in a background window no longer interrupts the user.
- The primary path relies on how browsers read modifier state; it is verified on
  current Firefox, Chrome, and Edge, and the close-button fallback covers a
  future browser that stops honoring it.
- For the brief hold, a key the user types into that same browser could be read
  as a Ctrl shortcut. The hold ends as soon as the tab closes to keep this rare.

### ADR-012: The Tab Closes on the Worm Scene's Pop

- **Status:** Accepted
- **Date:** 2026-10-10

#### Context

A blocked tab used to close instantly, followed by a text notification. The
approved design replaces that with a four-second scene in the bottom-right corner
of the screen: an infested brain rises, a doctor pulls the worm out, and the tab
closes exactly when the worm pops out (3.0 s). The seconds before the pop are the
user's last warning (a warning sound at the start is planned). The scene must look
like the approved design and stay sharp at every Windows zoom level and on every
monitor.

#### Decision

- **Delayed close, same rules.** When a rule blocks the selected tab, the scene
  starts instead of the close. On the pop the app closes that window's selected
  tab only if it still shows a blocked page (any page of a blocking rule, so
  swiping to the next short still counts). If the user switched away, nothing is
  closed — never a different tab. Which tabs are closed is unchanged; only the
  moment moves.
- **One scene at a time.** Windows blocked in the same check share one scene and
  close on its pop. While a scene plays, new blocks wait; a blocked page still
  open afterwards starts the next scene.
- **Safety nets.** If the scene cannot be shown, the tabs close at once; if it
  never reports its pop, they close after five seconds.
- **Drawn live as flat 2D shapes.** The design was prototyped with a 3D library,
  but its view and light never move, so every part is drawn directly as flat
  outlines and tone steps, computed from the design's own geometry, colours and
  timing. The brain's fold pattern is regenerated from the design's seeds.
  Frames are compared with the design's frames at the same moments.
- **Painted on the processor, shown as a layered window.** Each frame is painted
  at exactly the screen's pixels (a few milliseconds) and handed to Windows as a
  layered, click-through, never-activating overlay, at 60 frames a second.
- **Actors and scripts.** Brain, worm and doctor are actors with their own moves;
  a script decides who does what when. Future endings (the user gives up the tab,
  or dodges to another tab) are new scripts reusing the same actors.

#### Rejected Alternatives

**A recorded animation**

Rejected: a recording is sharp only at the size it was made; at 200% zoom or on a
4K monitor it is blurry, and recording for the largest size makes the app much
bigger.

**Running the design page in an embedded browser**

Rejected: starts a hidden browser on every block (memory, delay before the first
frame) and makes a see-through, focus-free overlay fiddly.

**An ordinary Avalonia window**

Rejected after measurement: on a machine with an extra virtual display adapter,
the default graphics path redrew such a window only about four times a second,
and the graphics card was slow at filling the scene's many fine outlines.

#### Consequences

- The user sees the block coming and has about three seconds before the tab goes.
- The scene has no words, so the closed site's name is no longer shown anywhere
  (the user was just on it).
- The pop sound plays through Windows and follows the system volume and mute.

## Contributing, License, and Security

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) for build,
test, and coding-standard guidance.

Licensed under the [MIT License](LICENSE).

To report a security issue, follow [SECURITY.md](SECURITY.md) (please report
privately rather than in a public issue).

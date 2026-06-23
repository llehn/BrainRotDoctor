# BrainRotBlocker

BrainRotBlocker is a Windows application for limiting Instagram
doom-scrolling while retaining access to useful surfaces such as Direct
Messages and Stories.

## Status

The browser-independent **core product model** is implemented and tested
(`src/BrainRotBlocker.Core`). It is organized around the user's mental model
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

The Windows enforcement app (`src/BrainRotBlocker.App`) is a modern **Avalonia**
desktop app (light/dark theming) that observes selected tabs in supported
browser windows through Windows UI Automation, accounts time against the rules,
and closes the selected tab with Ctrl+W when a rule blocks it. It has been
live-tested locally against Firefox, Chrome, and Edge.

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

It ships as a single self-contained **`BrainRotBlocker.exe`** that doubles as a
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
dotnet run --project src/BrainRotBlocker.App
```

The app loads `config/default-config.json` when run from the repository, starts
the paired watchdog process, and registers current-user startup. You can use
another ruleset and an optional diagnostic log:

```sh
dotnet run --project src/BrainRotBlocker.App -- --config path\to\config.json --log path\to\brainrotblocker.log
```

For temporary tests that must not leave a watchdog or startup entry behind:

```sh
dotnet run --project src/BrainRotBlocker.App -- --no-install-prompt --no-watchdog --no-startup
```

To produce the single-file distributable (one downloadable exe, no runtime needed
on the target):

```sh
dotnet publish src/BrainRotBlocker.App -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -o publish
```

Compression keeps it a single double-click exe (~71&nbsp;MB) that self-extracts at
launch; installed size is unaffected.

The resulting `publish\BrainRotBlocker.exe` is what a user downloads and
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

BrainRotBlocker will provide an optional strict mode with these rules:

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
the weakness that BrainRotBlocker is intended to solve.

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

Required functionality has priority over security tradeoffs. BrainRotBlocker
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

BrainRotBlocker blocks configured doom-scrolling surfaces using recurring time
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

- Every open supported browser window is checked.
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

- **Status:** Accepted
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
concrete decisions made while implementing it in C# (`BrainRotBlocker.Core`,
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
  as a single-file, self-contained `BrainRotBlocker.exe` (no .NET runtime needed
  on the target). Run from outside the install location it shows a one-click
  "Install BrainRotBlocker?" window; run from the install location it is the app.
- **Per-user, no admin.** Installs to `%LOCALAPPDATA%\Programs\BrainRotBlocker`,
  writes the `HKCU\…\Run` autostart value, and registers an
  `HKCU\…\Uninstall\BrainRotBlocker` entry so it appears in Apps & features. All
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

## Contributing, License, and Security

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) for build,
test, and coding-standard guidance, and the [Code of Conduct](CODE_OF_CONDUCT.md).

Licensed under the [MIT License](LICENSE).

To report a security issue, follow [SECURITY.md](SECURITY.md) (please report
privately rather than in a public issue).

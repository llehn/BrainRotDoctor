# BrainRotBlocker

BrainRotBlocker is a planned Windows application for limiting Instagram
doom-scrolling while retaining access to useful surfaces such as Direct
Messages and Stories.

The project is currently in the design phase. See [idea.md](idea.md) for the
original concept and [plan.md](plan.md) for the production roadmap.

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

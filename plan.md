# BrainRotBlocker Production Plan

## 1. Product Goal

Help the user avoid compulsive doom-scrolling while preserving intentional,
useful access to social and media services.

Doom-scrolling means losing time and attention to repeated, low-intention
consumption loops such as algorithmic feeds, endless recommendations, reels,
shorts, or other infinite-scroll experiences.

The product succeeds when the user can still use valuable parts of a service,
such as communication, specific content, account access, and deliberate social
interaction, but cannot easily fall into or remain stuck in the addictive
scrolling loop.

The product fails if avoiding the restriction is easy enough to do impulsively.

## 2. Product Principles

1. **Commitment enforcement.** When strict mode is enabled, BrainRotBlocker
   must strongly resist being stopped, disabled, reconfigured, or removed. It
   must not provide an emergency bypass, recovery shortcut, delayed escape, or
   ordinary uninstall path that defeats the active commitment.
2. **Functionality before security tradeoffs.** The application must first
   perform its required blocking and commitment-enforcement behavior. Security
   must protect a functioning product, not prevent it from functioning. A
   security control that defeats a core requirement is not an acceptable
   design, even if it reduces risk.
3. **Production quality protects the commitment.** Malfunctions are addressed
   through conservative architecture, narrow targeting, extensive automated
   and system testing, staged releases, and high-quality implementation. They
   are not addressed by adding a strict-mode bypass.
4. **Cross-browser enforcement.** The product targets all supported Windows
   browsers through native Windows UI Automation. Browser extensions are not
   an enforcement dependency because they can be disabled too easily.
5. **Local-first privacy.** Browsing events and budget state remain on the
   device. No analytics or telemetry are enabled by default.

## 3. Product Behavior Model

BrainRotBlocker does not primarily block websites. It blocks configured
doom-scrolling surfaces.

A doom-scrolling surface is any configured URL or page pattern that represents
the addictive consumption loop: YouTube Shorts, Instagram Reels, Facebook
Reels, TikTok feeds, Instagram's home feed, or similar future surfaces.
Viewing one short video, reel, or linked post is not automatically
doom-scrolling. The behavior to prevent is getting pulled into the repeated
scrolling loop.

The product uses recurring time budgets for configured doom-scrolling
surfaces. A default configuration could be `2 minutes every 1 hour`, but the
model must support different budgets and reset intervals.

Behavior example:

1. The user opens a configured doom-scrolling surface, such as YouTube Shorts.
2. The browser monitor matches the current URL against the configured rules.
3. The matching rule is assigned to one or more budget groups.
4. While the relevant budget has time remaining, the user can view the surface.
5. Time counts while at least one selected tab/current page in any supported
   browser window matches a configured rule.
6. Once the relevant budget is exhausted, matching doom-scrolling surfaces are
   blocked, for example by closing the tab.
7. When the interval resets, the budget becomes available again.

The broader service is not blocked merely because one of its surfaces is
budgeted. If YouTube Shorts is exhausted, normal YouTube pages remain usable
unless they match a configured doom-scrolling rule. If Instagram Reels is
exhausted, Instagram DMs remain usable. If Instagram's home feed is configured
as a doom-scrolling surface, that feed counts against its assigned budget, but
Instagram as a whole is not treated as blocked.

The core relationship is:

- URL/page rules identify doom-scrolling surfaces.
- Budget groups define allowed time and reset interval.
- Rules and budgets have an N-to-M relationship: one rule may consume multiple
  budgets, and one budget may be shared by multiple rules.

This allows rules such as:

- YouTube Shorts, Instagram Reels, Facebook Reels, and TikTok all share one
  `short-form-video` budget.
- Instagram home feed and other algorithmic feeds share one `feeds` budget.
- A specific high-risk surface has its own stricter budget.

The ruleset must be configurable without recompiling or redistributing the
application, because new doom-scrolling surfaces can appear after release.

Time accounting is based on the selected tab/current page of each open
supported browser window:

- Every open supported browser window is monitored.
- For each window, only that window's selected tab/current page is evaluated.
- A non-selected tab does not consume budget.
- A selected tab in a background browser window does consume budget.
- Time is counted once per real elapsed interval for each affected budget
  group. It is not multiplied by the number of matching windows.
- If selected tabs/current pages from multiple budget groups are open at the
  same time, each affected budget group consumes the same elapsed interval.
- System sleep and screen lock may pause accounting because the user cannot
  keep doom-scrolling in those states.
- System idle detection is not used to pause accounting.
- In case of doubt, prefer blocking over adding complex activity detection.
  Early iterations should avoid fancy detection and harden the simple behavior
  first.

The reason is product behavior: a non-selected tab is not the current page of
its browser window and is not the doom-scrolling surface being watched or
scrolled. Counting it would punish having a URL parked in a tab, which is not
the behavior the product targets.

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

## 4. Architecture Hypotheses

The architecture is not finalized. This section records current hypotheses
that must be validated or replaced during implementation.

### 4.1 Browser Observation

Windows UI Automation is the first hypothesis for observing browser state
across supported Windows browsers.

The browser observer should:

- Discover open supported browser windows.
- Read each window's selected tab/current page URL.
- Detect navigation, selected-tab changes, browser restarts, and browser UI
  reconstruction.
- Match selected tab/current page URLs against configured doom-scrolling
  rules.
- Close selected tabs when exhausted budgets apply.

Browser extensions are not an enforcement dependency.

### 4.2 Native Enforcement

The enforcement system is native Windows software responsible for rules,
budgets, persistence, tab closing, strict-mode behavior, and self-protection.

The exact supervision model is undecided. Candidate models include:

- mutually supervising background processes,
- Windows Service,
- service plus worker process,
- scheduled task or startup entry,
- combinations of the above.

The selected model must satisfy the product goal: enough friction that avoiding
the restriction is not easy to do impulsively.

### 4.3 Browser Adapter Boundary

Browser-specific behavior should be isolated behind adapters so support can be
added, removed, or fixed without changing the product model.

### 4.4 Strict-Mode Self-Protection

Strict mode must prevent ordinary disable, reconfiguration, and uninstall paths
during an active commitment. The exact mechanism is an implementation decision.

## 5. Roadmap

The roadmap is hierarchical. Each workstream should produce enough evidence to
justify the next layer of detail before implementation choices are locked in.

### 5.1 Repository and Decision Hygiene

Set up the project so future work is not lost between sessions:

- Initialize version control.
- Keep `idea.md`, `plan.md`, and `README.md` as source documents.
- Maintain ADRs for product and architecture decisions.
- Add repository hygiene only when needed by the next implementation step.

### 5.2 Core Product Model

Build the browser-independent behavior first:

- Configurable URL/page rules.
- Budget groups with reset intervals.
- N-to-M rule-to-budget assignments.
- Selected-tab-per-window time accounting.
- Exhausted-budget blocking decisions.
- Strict-mode configuration rules.

This layer should be deterministic and heavily tested because every later
component depends on it.

### 5.3 Browser Observation

Prove that supported browsers can be observed reliably enough for the product:

- Discover open supported browser windows.
- Read each window's selected tab/current page.
- Detect navigation, selected-tab changes, browser restarts, and browser UI
  changes.
- Keep browser-specific behavior isolated behind adapters.

The output of this workstream is the supported browser matrix and the first
adapter implementation.

### 5.4 Enforcement

Connect observation to the core model:

- Match selected tab/current page URLs against configured rules.
- Consume the relevant budget groups.
- Close selected tabs when exhausted budgets apply.
- Re-close matching surfaces reopened before reset.
- Preserve unconfigured service functionality.

### 5.5 Supervision and Strict Mode

Implement enough self-protection to satisfy the practical goal:

- Start enforcement automatically.
- Restore enforcement after ordinary process termination, logout, reboot, and
  crashes.
- Reject weakening configuration changes during active strict mode.
- Prevent ordinary disable and uninstall paths during active strict mode.
- Harden further only when actual use shows the current friction is
  insufficient.

### 5.6 User Interface

Provide the minimum UI needed to use the product correctly:

- Show current rules, budgets, and remaining time.
- Configure rules and budget groups outside active strict mode.
- Activate strict mode with clear consequences.
- Show whether enforcement is active.

UI details are deferred until the model and enforcement behavior are stable.

### 5.7 Installation and Distribution

Make the product installable only after the enforcement model works:

- Install and start the enforcement components.
- Preserve active strict-mode commitments through repair and upgrade paths.
- Allow complete uninstall outside strict mode.
- Refuse uninstall during active strict mode.
- Prepare public distribution only after private use validates the product.

### 5.8 Quality and Release Readiness

Quality work is part of every implementation step, not a final cleanup phase:

- Test the core model extensively.
- Test browser adapters against supported browser versions.
- Test process supervision and strict-mode bypass attempts.
- Test install, upgrade, repair, uninstall outside strict mode, and uninstall
  refusal during strict mode.
- Verify performance is acceptable and unrelated browsing is not slowed down.
- Keep privacy local by default.
- Do not publish circumvention instructions.

## 6. First Milestone

The first milestone should prove the product concept with the fewest moving
parts:

1. Initialize the repository and preserve the ADRs.
2. Implement the core rules and budget model.
3. Implement one browser adapter for one supported browser.
4. Detect the selected tab/current page of every open window for that browser.
5. Apply a small default ruleset with a recurring budget.
6. Close matching selected tabs when the budget is exhausted.
7. Add enough tests to trust the behavior before adding strict-mode hardening.

This milestone does not need public distribution, polished UI, package-manager
support, or maximum self-protection.

## 7. Deferred Detail

The following details should not be decided in this plan:

- Exact repository folder structure.
- Exact project names.
- Exact CI workflow layout.
- Exact installer technology.
- Exact browser adapter internals.
- Exact UI screens.
- Exact release packaging.
- Exhaustive test-case lists.

Those decisions should be made when the relevant implementation step begins,
using the product model and ADRs as constraints.

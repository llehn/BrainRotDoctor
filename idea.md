# Instagram Reels Blocker — Design Document

## Problem

Doom-scrolling Instagram Reels on Windows desktop (Firefox) is killing daily productivity. The current browser extension that hides the feed/reels gets disabled in 3–4 clicks — not enough friction.

**Constraints:**
- Windows desktop, Firefox, local admin
- Need to keep: Instagram DMs and Stories
- Need to block: Reels and feed doom-scrolling
- Phone is already solved via App Block strict mode — that model works

---

## What Works and Why (the phone model)

App Block on Android works because of two properties:
1. **Allowance-based** — not a full block, so no hard bounce-back effect
2. **Can't be undone in the moment** — strict mode with device admin

The goal is to replicate both properties on Windows.

---

## Approach

A small custom Windows application running as two mutually-watching background processes, implementing:

- A **daily time budget** for `instagram.com` (e.g. 10–15 min/day)
- **Active URL monitoring** of Firefox to detect reels/feed usage
- **Tab closing** when reels are detected or budget is exhausted
- **Friction-based self-protection** — hard enough to bypass that an impulsive brain gives up

---

## Technical Design (High Level)

### Detection
- Use the **Windows UIAutomation API** to poll Firefox's address bar every ~500ms
- Read the active tab URL from Firefox's accessibility tree — no network interception needed, works regardless of HTTPS/VPN
- Match URLs against a block list (e.g. `instagram.com/reels/`, home feed patterns)
- Optional: Firefox extension as a secondary detection layer for pre-load accuracy

### Blocking Action
- On URL match or budget exhaustion: close the offending tab via Win32 (`WM_CLOSE` or UIAutomation)
- Track cumulative time on `instagram.com` in a local file; enforce daily budget

### Self-Protection (the friction layer)
Two processes, mutually watching each other:
- **Process A**: does the actual URL monitoring and blocking
- **Process B**: watches A, restarts it immediately if it dies
- **Process A** also watches B — mutual watchdog with ~100–200ms polling interval

This means killing either process sequentially causes an immediate restart before you can reach the second. Since Process Explorer does not support multi-select kill, both processes cannot be terminated simultaneously through normal UI tools.

Both processes registered in **autorun** (registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` or equivalent) so a reboot does not escape the block — it requires deliberately disabling autorun first, then rebooting. That is a multi-step deliberate action, not an impulsive one.

### Installation / Uninstallation
- No functional uninstaller — the uninstall path either errors out or does nothing
- Manual removal requires: stop both processes, delete files, clean registry — several deliberate steps
- Config file held with an exclusive lock while processes are running

### What is NOT in scope
- Bulletproof against a determined, deliberate bypass — that is not the goal
- Kernel driver / Protected Process Light — unnecessary complexity
- Blocking Instagram entirely — DMs and Stories must remain accessible within the daily budget

---

## Bypass Paths (known, accepted)

| Method | Effort |
|---|---|
| Disable autorun entries + reboot | Deliberate, multi-step, requires reboot |
| Kill both processes via command line simultaneously | Requires knowing process names and scripting it |
| Boot from external drive / safe mode | High effort, clearly deliberate |

All bypass paths require deliberate multi-step action. None are compatible with an impulsive moment.
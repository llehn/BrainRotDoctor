# Security Policy

## Supported versions

BrainRotBlocker is pre-1.0 and under active development. Security fixes are
applied to the latest `main` only.

## Reporting a vulnerability

Please report security issues **privately**, not in public issues or pull
requests.

- Preferred: open a [GitHub private security advisory](https://github.com/llehn/BrainRotBlocker/security/advisories/new).
- Alternatively, email **levlehn@gmail.com** with the subject line
  `BrainRotBlocker security`.

Include the affected version/commit, a description of the issue, reproduction
steps, and the impact. You will normally get an acknowledgement within a few
days. Please give a reasonable amount of time for a fix before public
disclosure.

## Scope and threat model

BrainRotBlocker is a local-first Windows application. It has no server
component, performs no network requests, and contains no analytics or
telemetry. Configuration and runtime state are stored under the current user's
`%LOCALAPPDATA%`. It runs per-user, without administrator privileges.

Security reports of particular interest:

- Privilege escalation, arbitrary code execution, or path/registry handling
  flaws in the installer/uninstaller (`Installer`, `Program.RunUninstall`,
  `ScheduleDirectoryDeletion`).
- Unsafe handling of configuration files or strict-mode state.
- Vulnerabilities in the native interop (`NativeMethods`, UI Automation
  observation, tab closing).

### A note on strict-mode "bypasses"

By design (see ADR-001 in the [README](README.md)), strict mode resists being
disabled, reconfigured, or uninstalled while a commitment is active. The
project deliberately **does not** publish step-by-step circumvention
instructions, because doing so would defeat the product's purpose.

A *bypass that an impulsive user can perform in the moment* is a product
weakness we want to know about — please report it privately as above. A
deliberate, multi-step administrator-level removal (documented as accepted in
ADR-001) is a known, accepted limitation rather than a vulnerability.

## Known issues

- `Tmds.DBus.Protocol` (a Linux-only transitive dependency of
  `Avalonia.Desktop`) carries advisory **NU1903**. It is never loaded on
  Windows, which is the only supported platform, so the warning is suppressed
  in `BrainRotBlocker.App.csproj`. It will be removed when an updated Avalonia
  bumps the transitive version.

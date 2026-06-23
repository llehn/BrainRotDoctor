# Contributing to BrainRotBlocker

Thanks for your interest in contributing! This document explains how to build,
test, and propose changes.

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). By
participating you agree to uphold it.

## Prerequisites

- **.NET 10 SDK** (LTS). Check with `dotnet --version`.
- **Windows 10/11** for the enforcement app (`BrainRotBlocker.App` targets
  `net10.0-windows` and uses Windows UI Automation). The core library
  (`BrainRotBlocker.Core`) is platform-independent and builds/tests anywhere.

## Build and test

```sh
dotnet build
dotnet test
```

CI runs the same `dotnet build` / `dotnet test` on Windows for every pull
request (see `.github/workflows/ci.yml`). Please make sure both pass locally
first.

### Running the app from a checkout

```sh
dotnet run --project src/BrainRotBlocker.App
```

For temporary tests that must not leave a watchdog or a startup entry behind:

```sh
dotnet run --project src/BrainRotBlocker.App -- --no-install-prompt --no-watchdog --no-startup
```

Your edits in the app are saved to your per-user config
(`%LOCALAPPDATA%\BrainRotBlocker\config.json`), **not** to the shipped
`config/default-config.json`. The shipped file is only a read-only seed used on
first run. To iterate on the shipped default itself, edit that file directly
and run with `--config config/default-config.json`.

## Coding standards

These are enforced by the build (`Directory.Build.props`), so a PR that ignores
them will not compile:

- **Warnings are errors** (`TreatWarningsAsErrors`). Keep the build clean.
- **Nullable reference types** are enabled. No new nullable warnings.
- **Implicit usings** are enabled.
- Public API in `BrainRotBlocker.Core` is documented with XML comments;
  per-member comments are not required everywhere (CS1591 is relaxed).

Match the surrounding style: small focused types, clock/dependency injection in
the core so logic stays deterministic and testable, and no OS dependencies in
`BrainRotBlocker.Core`.

## Tests

- Add or update tests for any behavior change. The core model in particular is
  expected to stay heavily covered (URL matching, budget windows, configuration
  loading, accounting).
- The shipped `config/default-config.json` is guarded by
  `ShippedConfigTests`; if you change it, keep that test passing.
- New runtime/app behavior should be covered where it can be made testable
  (see, e.g., `StartupConfigResolverTests`).

## Architecture Decision Records (ADRs)

Significant product or architecture decisions are recorded as ADRs in the
[README](README.md). An accepted ADR must not be reversed silently: if your
change contradicts one, add a **new** ADR that explicitly supersedes the
earlier record and explains the rationale, including rejected alternatives.

Please open an issue to discuss anything that touches strict mode,
self-protection, the budget/accounting model, or the installer before writing a
large change — those areas are governed by the ADRs (ADR-001 through ADR-008) in the README.

## Pull requests

1. Fork and create a topic branch.
2. Keep changes focused; one logical change per PR.
3. Ensure `dotnet build` and `dotnet test` pass.
4. Describe **what** changed and **why**, and reference any related issue or
   ADR.
5. Do not include build output (`bin/`, `obj/`, `publish/`) — it is gitignored.

## Localization

The UI ships in 31 languages (`src/BrainRotBlocker.App/Ui/Strings.cs`).
`LocalizationTests` verifies every language has every key with matching
placeholders, so when you add or rename a string key, update **all** languages
(machine-assisted translations are acceptable; a native-speaker review is
welcome). English (US) is the canonical fallback.

## Security

Please report vulnerabilities privately as described in
[SECURITY.md](SECURITY.md) rather than in a public issue or PR. In particular,
do not publish strict-mode circumvention instructions (see ADR-001).

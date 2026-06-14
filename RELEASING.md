# Releasing

All releases are built and published automatically by GitHub Actions when a version tag is pushed.
Use `release.ps1` to tag — never tag manually.

## Commands

```powershell
# Bug fix (x.x.X)
pwsh -File release.ps1 -Patch

# New feature (x.X.0)
pwsh -File release.ps1

# Breaking / significant functionality change (X.0.0)
pwsh -File release.ps1 -Major
```

The workflow builds a self-contained `win-x64` publish, zips it, and attaches it to a GitHub Release.
Track progress at: https://github.com/MrSanityCheck/QuickLaunch/actions

## Installing / updating on another PC

```powershell
irm https://raw.githubusercontent.com/MrSanityCheck/QuickLaunch/master/install.ps1 | iex
```

Extracts to `%LOCALAPPDATA%\QuickLaunch\` and wires up a startup shortcut. Re-run to update.

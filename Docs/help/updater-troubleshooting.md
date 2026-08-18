# Updater Troubleshooting

This document collects symptoms, root causes and fixes for problems encountered with the `msTools.Updater` integration.

## 1. `NoUpdate` / `NoNewerUpdateAvailable` although no update source is configured

**Date:** 2026-08-18

**Observed symptom**

`Rezepte.Updater.TestHost` (or `Rezepte.Web`) logs:

```text
Outcome = NoUpdate
State = Idle
Code = NoNewerUpdateAvailable
Message = No newer update is available.
Error =
```

**Root cause**

Neither `ApplicationUpdates:RepositoryOwner` / `ApplicationUpdates:RepositoryName` nor `ApplicationUpdates:LocalSourceDirectory` was configured. `msTools.Updater` then falls back to the default `AutoUpdateLocalFolderSource`, which does not find a valid `update.json` manifest. The result is a silent `NoUpdate` that can be mistaken for a successful pipeline.

**Fix / Checklist**

- If using GitHub: set `RepositoryOwner`, `RepositoryName` and `ManifestAssetName`.
- If using a local folder: set `LocalSourceDirectory` to a directory that contains a valid `update.json` manifest and the update package.
- Do not leave both options empty.

## 2. Open — template for next finding

**Date:**

**Observed symptom**

**Root cause**

**Fix / Checklist**

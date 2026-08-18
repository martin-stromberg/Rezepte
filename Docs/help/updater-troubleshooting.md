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

## 2. `InstallationStarted` but nothing happens and `update.lock` stays

**Date:** 2026-08-18

**Observed symptom**

`Rezepte.Updater.TestHost` (or `Rezepte.Web`) starts the installation and logs:

```text
Outcome = Success
State = Installing
Code = InstallationStarted
Message = Installation started.
Error =
```

A `update.lock` file is created, the package is downloaded to `pending/` and a PowerShell script is generated, but the application files are not copied. The lock file persists and no further output appears.

**Root cause**

`msTools.Updater`’s default `IAutoUpdateProcessRunner` starts the generated PowerShell script as a detached process. It does not capture stdout/stderr. If the script hangs (missing IIS permissions, missing `WebAdministration`/`IISAdministration` module, app pool cannot be stopped, etc.) or fails, the error is not reported by `msTools.Updater`. The lock is also not released because the installation is still considered in progress.

**Fix / Checklist**

- Run the generated `.ps1` script manually in an **administrative** PowerShell to see the actual error.
- Alternatively, use `Rezepte.Updater.TestHost` with the included `LoggingAutoUpdateProcessRunner`. It starts `powershell -File <script>` synchronously and prints the script output.
- Ensure the account can stop and start the configured IIS app pool (or Windows service / executable).
- Ensure the `WebAdministration` or `IISAdministration` PowerShell module is installed.
- For IIS, `StopHostAfterScriptStart` is not relevant. The PowerShell script must stop/start the app pool itself.

## 3. `install` returns `No update package is ready to install` after a previous `run` was interrupted

**Date:** 2026-08-18

**Observed symptom**

After a previous `run` created `update.lock`, downloaded the zip into `pending/` and generated the PowerShell script, the user runs `install` again and gets:

```text
No update package is ready to install.
```

The package file is still present in `pending/`, but `msTools.Updater` does not recognize it.

**Root cause**

`install`/`InstallAsync` installs the update package that was discovered or downloaded in the **same process**. `msTools.Updater` does not reconstruct the `AutoUpdatePackageDescriptor` from the `pending/` zip when a new `dotnet run` starts. After the test host exits, the in-memory state is gone.

**Fix / Checklist**

- Execute the full workflow in a single process: `dotnet run -- run`.
- Before re-running, delete the stale `update.lock` from the `DownloadPath` directory (default `updates/`).
- Alternatively, use `dotnet run -- check` and `dotnet run -- download` and `dotnet run -- install` in the same long-running host session, which is not possible with the console test host.
- For `Rezepte.Updater.TestHost`, prefer `run` over `install`.

## 4. Open — template for next finding

**Date:**

**Observed symptom**

**Root cause**

**Fix / Checklist**

# Validation — 2026-11-03

## Executed in the managed Debian 13 / Linux x64 cloud instance

* Official .NET SDK 8.0.427 installed in `/workspace/.dotnet` (runtime 8.0.33).
* Actual upstream repositories cloned and selected source/license files inspected; full commit IDs recorded in RESEARCH.md.
* `dotnet restore Soultrap.sln --locked-mode`: passed. Repeated after publish to verify that runtime identifiers remain consistent. A first attempt exposed NU1004; RuntimeIdentifiers is now explicitly win-x64 and lockfiles regenerated.
* `dotnet build Soultrap.sln -c Release --no-restore -m:2`: passed, **0 warnings, 0 errors**, including WPF XAML compilation.
* `dotnet test Soultrap.Tests -c Release --no-build`: **42 passed, 0 failed, 0 skipped**. Config save/load and corruption preservation, preset schema/import/export/delete, path traversal and symlink refusal, cursor layout compatibility, apply/hash/restore, restore ordering and conflicts, cross-version refusal, backup tampering, deletion restrictions, PNG dimensions, flag types, version comparison, missing releases, update digest/host validation, valid and corrupted HTTP downloads, ZIP traversal and version-matched mod import.
* `dotnet publish Soultrap.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/publish`: passed. Produces `Soultrap.exe` plus Windows Desktop/.NET runtime and dependencies. PE x64 Windows GUI executable verified with the local `file` utility. This does not execute it.
* Automated HTTP tests use mocked handlers. No live published Soultrap release was tested.

Current-run TRX: `Soultrap.Tests/TestResults/cloud.trx` (generated, not committed). ZIP: `artifacts/Soultrap-0.1.0-win-x64.zip`; checksums: `artifacts/SHA256SUMS.txt` (generated, not committed). The archive includes all required dependencies, original assets and license notices.

## Windows checks not executed here

WPF visual inspection, contrast under each theme, tray/autostart, Windows credentials/DPAPI enrollment, Windows Hello (not implemented), real Roblox discovery/signature/launch, in-game cursor effects, installer compile/run/uninstall, and real release download/install were **not tested**. Inno Setup 6 and Windows are unavailable on the cloud host. Windows GitHub Actions is configured to build and test, compile the installer, upload artifacts and create a draft prerelease only after both jobs succeed. Local validation does not establish that those remote jobs succeeded. No public production release is claimed.

Manual acceptance on Windows 11: build/install; run with Roblox missing and present; launch official Roblox; close it; apply each original cursor to a disposable client and check visuals; restore and compare hashes; update Roblox and confirm old journal refusal; test custom PNG re-centering; test ZIP rejection; change/reload each theme and config; exercise corrupt config preservation; authenticate/cancel/wrong-account owner login; verify tray and Run key; uninstall without deleting backup data. Do not promote the release before these checks.

## Constraints and outstanding functionality

Classic UI is an interactive local study, not real historical CoreGui. FastFlags are validated/editable locally and not applied because the authoritative current allowlist could not be verified (DevForum access returned CONNECT 403). Modern defaults can only be restored from actual Soultrap backups. Roblox installation and client updates use the official website/installer, not a Soultrap-managed downloader. Updates are never automatically executed without a pinned publisher identity. English only. Passwordless/Hello owner authentication is not supported.

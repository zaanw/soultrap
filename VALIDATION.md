# Validation — 2026-11-03

## 0.3.1 — menu and in-app EXE updates

Application commit `a010987`, [CI 41667558056](https://github.com/zaanw/soultrap/actions/runs/41667558056): Windows, Linux, stable release publication and live public-feed verification all succeeded. Release: https://github.com/zaanw/soultrap/releases/tag/v0.3.1 . Direct installer: https://github.com/zaanw/soultrap/releases/download/v0.3.1/Soultrap-0.3.1-Setup-x64.exe .

* Local Release build: zero warnings/errors; 77 tests passed, none failed/skipped. Windows/Linux unit jobs passed the same suite.
* Windows GUI smoke opened the grouped/custom-title-bar menu, all sections and all themes in each startup mode, with the existing pixel/foreground checks enabled.
* Installer integration installed the previous public 0.3.0, upgraded to 0.3.1, waited for the restarted main window and verified unchanged settings/backup bytes. Test uninstall retained user data. The preceding 0.3.0 build had also verified the transition from 0.2.1.
* The live Windows job used the actual updater code against the public Releases API, downloaded the published EXE and rechecked hash/PE. It did not execute that downloaded file. Installer execution was tested separately with the locally built CI candidate.
* Explicit update confirmation, Windows download-zone marking, fixed repository/version/URL validation, bounded/cancellable downloads and locked hash verification are implemented. A publisher certificate is still absent; this does not remove Windows trust prompts or provide unattended publisher-pinned updates.

This validates the launcher/update workflow, not live Roblox gameplay or every possible user machine. See UPDATING.md for bootstrap and trust boundaries.

## 0.2.1 — rendered theme regression

Application commit `26f0e4f`, [Windows CI 41661487829](https://github.com/zaanw/soultrap/actions/runs/41661487829): Windows build, unit tests, GUI smoke, publish and installer packaging succeeded. GUI checks render the shared window root, translate page/footer/scroll coordinates into that frame and compare pixels against the active palette; body text and primary-button foreground inheritance are checked in every theme. Existing 56 unit/integration tests remain unchanged. This fixes the white dark-mode page/error area shown in user feedback; it does not change Roblox detection or claim gameplay testing.

## 0.2.0 — redesigned menu and startup flow

Application commit `c2ce2db` verified in [GitHub Actions 41657532965](https://github.com/zaanw/soultrap/actions/runs/41657532965): Windows, Linux and release-draft jobs all succeeded. Windows artifacts are available as **Soultrap-Windows-x64**, artifact ID `13241240111`.

* Local Release build: zero errors/warnings; self-contained win-x64 publish passed.
* Unit/integration tests: **56 passed, 0 failed, 0 skipped** locally; Windows and Linux test jobs succeeded. New coverage includes preference persistence, old-config compatibility, upstream import mapping/atomic validation, source-font-vs-launcher-zoom distinction, safe process priority and log cleanup boundaries.
* Windows GUI smoke: real WPF process opens startup window and all twelve navigation pages, filters sidebar sections, follows studio navigation and reapplies all eight themes. Run with Fluent splash, no splash, and Classic splash; all modes passed. Startup/Home/Settings/upstream-inventory PNG captures are uploaded with the Windows artifact.
* Inno Setup 0.2.0 installer and portable ZIP compiled/packaged/uploaded successfully. No installer interaction or live Roblox gameplay test was performed.
* The initial new startup test exposed a frozen SolidColorBrush animation error; animations are now attached before publishing brushes to resource consumers. The regression check remains enabled.

The 501-property upstream inventory is not a claim of full upstream feature parity. See SETTINGS-COMPATIBILITY.md for the working subset and explicitly unimplemented integrations. Signing still needs a real trusted publisher certificate; current CI builds remain unsigned. Screenshots were generated automatically; there was no manual Windows UI or live Roblox acceptance session.

## Historical 0.1.0 validation

## Executed in the managed Debian 13 / Linux x64 cloud instance

* Official .NET SDK 8.0.427 installed in `/workspace/.dotnet` (runtime 8.0.33).
* Actual upstream repositories cloned and selected source/license files inspected; full commit IDs recorded in RESEARCH.md.
* `dotnet restore Soultrap.sln --locked-mode`: passed. Repeated after publish to verify that runtime identifiers remain consistent. A first attempt exposed NU1004; RuntimeIdentifiers is now explicitly win-x64 and lockfiles regenerated.
* `dotnet build Soultrap.sln -c Release --no-restore -m:2`: passed, **0 warnings, 0 errors**, including WPF XAML compilation.
* `dotnet test Soultrap.Tests -c Release --no-build`: **42 passed, 0 failed, 0 skipped**. Config save/load and corruption preservation, preset schema/import/export/delete, path traversal and symlink refusal, cursor layout compatibility, apply/hash/restore, restore ordering and conflicts, cross-version refusal, backup tampering, deletion restrictions, PNG dimensions, flag types, version comparison, missing releases, update digest/host validation, valid and corrupted HTTP downloads, ZIP traversal and version-matched mod import.
* `dotnet publish Soultrap.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/publish`: passed. Produces `Soultrap.exe` plus Windows Desktop/.NET runtime and dependencies. PE x64 Windows GUI executable verified with the local `file` utility. This does not execute it.
* Automated HTTP tests use mocked handlers. No live published Soultrap release was tested.

Current-run TRX: `Soultrap.Tests/TestResults/cloud.trx` (generated, not committed). ZIP: `artifacts/Soultrap-0.1.0-win-x64.zip`; checksums: `artifacts/SHA256SUMS.txt` (generated, not committed). The archive includes all required dependencies, original assets and license notices.

## Windows CI — verified remotely

GitHub Actions run [41642598611](https://github.com/zaanw/soultrap/actions/runs/41642598611), application code commit `5230c1e443d801a83eba63cf83b68e2d1cfb044d`:

* `windows`: completed successfully. Locked restore, Release build, xUnit run, self-contained publish, Inno Setup installer compilation and artifact uploads each have success status.
* `core-linux`: completed successfully.
* `release-draft`: completed successfully, including downloading the Windows artifacts and the create/retain draft prerelease step. The draft is accessible to repository maintainers under Releases; no public release publication is claimed.
* Uploaded artifact **Soultrap-Windows-x64** contains portable ZIP, SHA-256 checksums and `installer/Soultrap-0.1.0-Setup-x64.exe`. The artifact listing reports 117 MB.

The first run failed in Linux because `--locked-mode` was passed to `dotnet test`; the workflow now performs explicit locked restore before testing. The corrected run above passed. Node 20 action deprecation notices are warnings, not build failures.

## Manual Windows checks still outstanding

WPF visual inspection, contrast under each theme, tray/autostart, Windows credentials/DPAPI enrollment, Windows Hello (not implemented), real Roblox discovery/signature/launch, in-game cursor effects, installer run/uninstall, and real release download/install were **not tested**. Inno Setup 6 and Windows are unavailable on the cloud host. Windows CI compilation/test/packaging succeeded as recorded above. It does not validate interaction with Windows GUI or Roblox. No public production release is claimed.

Manual acceptance on Windows 11: build/install; run with Roblox missing and present; launch official Roblox; close it; apply each original cursor to a disposable client and check visuals; restore and compare hashes; update Roblox and confirm old journal refusal; test custom PNG re-centering; test ZIP rejection; change/reload each theme and config; exercise corrupt config preservation; authenticate/cancel/wrong-account owner login; verify tray and Run key; uninstall without deleting backup data. Do not promote the release before these checks.

## Constraints and outstanding functionality

Classic UI is an interactive local study, not real historical CoreGui. FastFlags are validated/editable locally and not applied because the authoritative current allowlist could not be verified (DevForum access returned CONNECT 403). Modern defaults can only be restored from actual Soultrap backups. Roblox installation and client updates use the official website/installer, not a Soultrap-managed downloader. Updates are never automatically executed without a pinned publisher identity. English only. Passwordless/Hello owner authentication is not supported.

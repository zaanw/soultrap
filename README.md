# SOULTRAP — Ultimate Aero

An independent Windows 11 WPF Roblox customization workbench, with compact Windows 7-inspired chrome. **Version 0.1.0 builds and passes automated tests in Windows and Linux CI; manual Windows GUI / Roblox acceptance is still outstanding.** Soultrap is not affiliated with Roblox.

## Build and run

On Windows 11 x64, install .NET SDK **8.0.427** (only developers need the SDK):

```powershell
dotnet restore Soultrap.sln --locked-mode
dotnet build Soultrap.sln -c Release --no-restore
dotnet test Soultrap.Tests -c Release --no-build
dotnet run --project Soultrap.App -c Release
dotnet publish Soultrap.App -c Release -r win-x64 --self-contained true -o artifacts/publish
```

Run `artifacts/publish/Soultrap.exe`. Keep all published files together. End users of the self-contained folder need neither Visual Studio nor the .NET runtime. “Portable” means a relocatable application folder; user data remains in `%LOCALAPPDATA%\Soultrap`.

For a per-user Windows installer, install Inno Setup 6 and compile `Soultrap.Installer/Soultrap.iss` after publishing. GitHub Actions builds/tests on Windows and Linux and uploads the ZIP and installer as workflow artifacts. A pushed commit is not evidence that the remote workflow passed. After successful Windows and Linux jobs, a main-branch push creates a draft prerelease with the packaged binaries if v0.1.0 does not already exist. No public release is auto-published and no executable is silently signed.

## Use

1. Install Roblox from its official website; use **Install / Update Roblox** to open it. Soultrap does not download or maintain its own Roblox distribution.
2. **Refresh Client** and select the exact version. Standard Roblox/Bloxstrap/Fishstrap/Voidstrap local version directories are scanned. Configure another directory in Settings if needed.
3. **Validate Client** verifies Authenticode trust, Roblox Corporation publisher, and expected cursor layout. **Play Roblox** requests `RobloxPlayerBeta.exe --app`; account/game joining can also use a `roblox-player:` URI through Windows. Launch tickets are session-only and omitted from saved/exported settings and logs.
4. Close Roblox. Choose a cursor and Apply. Original bytes are backed up first. File/hash verification proves the write, not that the current Roblox renderer uses that cursor.
5. Restore in Diagnostics, newest backup first. Backups from other client versions cannot be applied. Restore all recorded changes with Restore Defaults. If no backup exists Soultrap cannot infer original bytes; repair through the official installer.
6. Cursor Studio imports/decodes PNG, resizes it and centres the chosen click point. Export saves PNG, local custom cursor and editor metadata. Roblox does not consume arbitrary OS hotspot metadata.
7. Presets support local JSON creation, edit/rename, duplicate, import, export, delete and cursor application. Built-ins cannot be overwritten or deleted. Classic preview selections do not alter in-game menus.
8. Choose one of six themes; save settings to apply accent, background, text, opacity, animations and sounds. Autostart is a current-user Run key; minimizing to tray is optional. Theme Studio opens these settings.
9. Developer Console requires the **current Windows account password**, freshly verified by Windows. First authentication enrolls a DPAPI-protected local owner marker. It does not grant administrator/Roblox privileges. Windows Hello and passwordless accounts are unsupported.

## Compatible mods

A ZIP contains `mod.json` and one or two PNGs, with an exact client version, for example:

```json
{
  "Schema": 1,
  "Name": "My original cursor",
  "RequiredVersion": "version-EXACT-INSTALLED-ID",
  "Files": {
    "content/textures/Cursors/KeyboardMouse/ArrowCursor.png": "cursor.png",
    "content/textures/Cursors/KeyboardMouse/ArrowFarCursor.png": "cursor.png"
  }
}
```

Only these two destinations are accepted. Extra entries, traversal, links, executable content, unknown targets and oversized archives are rejected. Inspect lists files; Install applies the inspected package. Remove Imported Package clears the in-memory package; disabling/restoring reverses its applied files through the journal. There is no download marketplace or executable mod support.

## Honest compatibility limits

* Historical menu/HUD/CoreGui replacement is unavailable. Classic Roblox is an interactive **local preview only**; it is not a pixel-exact recreation.
* FastFlags search, categories, scalar validation and JSON import/export work locally. Application is deliberately disabled: no authoritative current allowlist was verified. Nonempty flags block preset application. Arbitrary flags are not promised to work.
* Client installation/updates are delegated to the official Roblox installer, which the user launches. There is no channel switcher, old-client downgrade or built-in online client version comparison.
* Soultrap update metadata/download code requires a real GitHub release and asset digest. No release is assumed to exist. SHA-256 alone does not establish publisher identity; downloaded files are never auto-executed. Certificate pinning, signed release distribution and automatic installation remain future work.
* English interface only. No Windows Hello, game chat integration or Roblox account privileges. Feature switches control local diagnostic/editor notes only.
* Actual Windows GUI, tray, credentials, startup, publisher validation, Roblox launch and visible cursor behavior must be manually tested on Windows 11. Linux unit tests cannot establish those outcomes.

See [RESEARCH.md](RESEARCH.md), [ARCHITECTURE.md](ARCHITECTURE.md), [VALIDATION.md](VALIDATION.md), and [CHANGELOG.md](CHANGELOG.md). All bundled cursor graphics are original geometric artwork, not Roblox assets.

## SmartScreen / Unknown publisher

Current CI downloads are unsigned. A trusted publisher certificate is required for Authenticode signing; a code change cannot guarantee suppression of SmartScreen's reputation warning. [SIGNING.md](SIGNING.md) describes the external certificate requirement and the Windows script that signs and verifies the app, installer and uninstaller. Do not treat a normal unsigned CI build as a signed release.

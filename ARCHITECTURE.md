# Architecture

* `Soultrap.Core`: typed settings, preset schema, feature catalog, conservative FastFlag validation and compatibility models. No Windows dependency.
* `Soultrap.Infrastructure`: atomic JSON files, discovery, Authenticode verification, journaled cursor writes, mod ZIP validation, GitHub releases and bounded downloads. No upstream code is copied.
* `Soultrap.App`: WPF, constructor injection through Microsoft.Extensions.DependencyInjection, observable view model, async commands, native Windows credential prompt, DPAPI profile, tray, registry autostart, Aero resource dictionary and original PNG cursor studio.
* `Soultrap.Tests`: xUnit filesystem, configuration, package and HTTP integration checks with isolated temporary directories and mocked HTTP.
* `Soultrap.Installer`: per-user Inno Setup 6 script. Does not require administrator access or a separately installed .NET runtime.

The UI and application composition remain in one project; a separate Soultrap.UI assembly would add a deployment unit without a current independent consumer. All long network/file operations await completion. Process-wide command serialization prevents overlapping UI writes. Mutation checks require Roblox closed. Discovery accepts standard Win32 versions or an explicit user path; Store clients, portable third-party custom layouts and non-Windows clients are not assumed compatible.

## Transaction model

Before each write: check client version path, all allowed targets, PNG header and dimensions, and reject reparse points in the target ancestry. Store original bytes and their SHA-256, plus expected applied hashes, in a new GUID backup directory. Persist journal before client writes. Verify after each write. Rollback validates every target and original hash before starting restoration, rejects newer/foreign edits and requires the exact original installation version. A journal left by an interrupted apply can restore files whose bytes match either planned output or original. Restore journals newest first. No silent reapplication after a Roblox update.

A journal supports recovery but is not a cross-filesystem atomic transaction: OS termination during restore can leave a partial restore. Retry is possible because both original and applied hashes are accepted. There is no claim of protection against a malicious process running as the same Windows user or a concurrent filesystem link-swap attack. Close Roblox and other client modifiers during mutation. Offline snapshots do not capture running processes.

## Owner authentication

Native Windows credential prompt, LogonUser and exact current-SID match, with a DPAPI CurrentUser-protected enrollment marker. No hardcoded password and no automatic owner access by username. Each console opening requires authentication. Windows Hello/PIN and passwordless Microsoft-account authentication are not implemented. This is a local application lock, not an administrator elevation or a Roblox privilege. No network owner service exists.

## Update trust

HTTPS GitHub release API, semantic numeric versions, restricted project asset URL, bounded download and mandatory SHA-256 digest. Metadata hash establishes integrity relative to GitHub, not an independently authenticated Soultrap publisher. Without a pinned signing certificate Soultrap never starts an update executable. Installer builds are unsigned unless a maintainer adds signing separately. No unattended installer is provided.

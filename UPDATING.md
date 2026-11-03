# In-app updates and release trust

## User flow

The first install of 0.3.1 is a direct per-user Setup EXE from GitHub Releases. A previously installed 0.2.x cannot acquire a feature absent from its binary, so that bootstrap happens once. Subsequent updates use **Updates → Check → Download and install** inside Soultrap. The home page also exposes these actions. No ZIP extraction or file copying is needed.

Downloads are asynchronous and cancellable. After successful verification Soultrap asks for explicit confirmation with the version, official repository, installation directory and publisher limitation. The current app remains usable if download/verification is cancelled or fails. Before launch, the updater confirms write access to the existing application folder and rechecks the installer while holding a read handle that denies replacement/write. If Windows starts the installer, Soultrap exits. The per-user Inno Setup upgrade restores the executable files in the same directory and restarts the updated application. Installer failures are handled by Inno Setup; merely starting its process is not reported as a completed installation.

Settings, cursor backups and presets live outside the application folder and are retained. Save pending edits before updating. No real-time process priority, arbitrary mod execution, application elevation, Defender exclusions or root CA installation are introduced.

## Verification boundary

* The update API is fixed to `https://api.github.com/repos/zaanw/soultrap/releases/latest`.
* Draft/prerelease/foreign releases, nonnumeric version tags, equal/older versions, duplicate/wrong-version installer assets, unexpected URLs, missing digests, size violations and incomplete/corrupt downloads are rejected.
* Only `Soultrap-{version}-Setup-x64.exe` under the exact release tag is selected. Redirects must remain HTTPS on GitHub's known release-content hosts.
* SHA-256 must match GitHub's asset metadata. The cached file is checked again before launch; a Windows PE executable is required. This is content integrity relative to GitHub, not an independent Authenticode identity check.
* Windows Internet-zone metadata is written for the downloaded installer. SmartScreen and other Windows protections remain effective. Unsigned publishers can still trigger Windows prompts.
* No downloaded installer is started without the explicit confirmation path. A missing publisher certificate is not concealed and does not enable unattended execution.

This trusts the configured GitHub repository, GitHub HTTPS/API metadata, and its maintainer/release credentials. Compromise of that publisher account/feed is outside the protection of a hash received from the same source. A trusted signing service/certificate and publisher pinning would provide additional protection. See SIGNING.md. No certificate or signing key is fabricated or committed.

## Release pipeline and validation

Version identifiers in the app, installer, asset names and workflow must agree and increase for a new release. Existing published versions are immutable in this workflow. Stable publication follows successful core tests, Windows GUI/theme tests and an actual installer-upgrade check. That check installs a previous version on a disposable Windows runner, upgrades to the candidate with the restart flag, waits for the new main window, verifies unchanged configuration/backup hashes and confirms uninstall retains data.

After publication a separate Windows job uses the application updater code against the real public API, downloads the actual release EXE and verifies it without execution. The initial bootstrap used the previously validated 0.2.1 Actions artifact as a baseline; future jobs use the latest stable public release.

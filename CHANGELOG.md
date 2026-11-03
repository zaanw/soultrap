# Changelog

## 0.3.1 — 2026-11-03

Voidstrap-style grouped navigation, vector icons and active marker, integrated draggable title bar/window controls, compact Roblox home and persistent footer actions. Flat themed settings sections replace native beveled group boxes.

Confirmed in-app EXE updates: stable release API, exact version/asset/repository validation, bounded/cancellable download with progress, mandatory SHA-256, PE checks, locked final verification and writable-folder check. Windows Internet-zone metadata retained. Installer updates the same directory, restarts Soultrap and preserves saved settings/backups. Public releases are published only after Windows/Linux, GUI and installer-upgrade checks; a live job verifies the public feed/download. Publisher identity is not pinned; confirmation and possible Windows warnings remain.

77 local tests passed. Initial 0.3.0 CI already validated upgrade from 0.2.1, restart, data preservation and the public feed; final 0.3.1 uses that public build as its upgrade baseline.

## 0.2.1 — 2026-11-03

Fix the white client area and error footer in dark mode. Explicit Window styles for derived windows, theme backgrounds on page/scroll surfaces, themed scrollbars/progress tracks, inherited body/button foregrounds and readable combo arrows. Windows GUI regression now samples actual rendered page/footer pixels and checks body/button text in all eight palettes. CI run 41661487829 passed Windows build/tests, all splash/navigation/theme checks, publish and installer packaging. No Roblox detection behavior changed.

## 0.2.0 — 2026-11-03

Voidstrap-inspired sidebar and search, adapted MIT startup layouts (Fluent and Classic), real initialization progress, eight working palettes, launcher behavior/rendering/editor preferences, local process monitoring, Windows shortcuts and safe log maintenance. Inventory of 501 upstream configuration declarations with supported-subset import and honest compatibility statuses. Existing Soultrap themes/configs remain readable. Expanded tests and Windows startup/tab/navigation checks; CI captures UI screenshots.

No claim of full feature parity with upstream integrations. Publisher signing still requires a trusted certificate; normal CI binaries remain unsigned.

## 0.1.0 — 2026-11-03

Initial independent implementation: Aero WPF workbench, six themes, Win32 client discovery and verified process-launch path, journaled cursor customization and restore, original era-inspired cursor PNGs, cursor studio, local Classic UI preview, validated presets, restricted PNG ZIP mods, conservative local FastFlags editor, settings/tray/autostart, Windows-credential owner console, structured JSONL logs, GitHub release download integrity checks, xUnit tests and Windows CI packaging.

Windows runtime acceptance, current Roblox visual compatibility and installer execution remain unverified. See VALIDATION.md for actual executed checks.

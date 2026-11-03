# Settings and UI adaptation — Soultrap 0.2.0

Reference snapshots are the commits recorded in RESEARCH.md. Re-inspected:

* Bloxstrap `Bloxstrap/Models/Persistable/Settings.cs`: 23 settable properties.
* Fishstrap `Bloxstrap/Models/Persistable/Settings.cs`: 45 settable properties.
* Voidstrap `src/Voidstrap.App/Models/Persistable/AppSettings.cs`: 433 settable property declarations (including declarations in nested settings types).
* Voidstrap legacy WPF `UI/Elements/Settings/MainWindow.xaml`, `Bootstrapper/FluentDialog.xaml`, and `ClassicFluentDialog.xaml`.

`Assets/UpstreamSettings.json` records all **501 property declarations** with project, name, original type, implementation status and Soultrap destination. This is a snapshot of configuration declarations, not a claim that every upstream option currently works in Roblox or that every upstream subsystem was audited. All inventory rows can be searched in **Upstream Settings**. Unsupported rows have no active toggle.

## Working adaptations

| Source option / idea | Soultrap behavior |
|---|---|
| All three: CheckForUpdates | Optional Soultrap update check after the main window opens; does not block startup splash |
| All three: ConfirmLaunches | Confirmation before direct launch or passing a launch URI to Windows |
| Bloxstrap / Fishstrap: WPFSoftwareRender | WPF software rendering compatibility mode |
| All three: BootstrapperTitle | Custom Soultrap startup-window title, with validated length |
| Voidstrap: DisableSplashScreen | Inverse of Show startup loading screen; suppresses splash on next startup |
| Voidstrap: UiZoomPercent | Whole-workspace scale, 0.85–1.50 (converted from percent) |
| Voidstrap: EditorFontSize / EditorWordWrap | Actual JSON-editor typography and line wrapping |
| Voidstrap: CustomAccentColor | Validated custom #RRGGBB accent |
| Bloxstrap / Fishstrap: EnableActivityTracking | Simplified local process-status monitor; no game/session parsing or Discord publishing |
| Voidstrap: SelectedCpuPriority | Validated Normal / AboveNormal / BelowNormal priority on the process Soultrap directly starts; not imported automatically because upstream uses different values |
| Fishstrap / Voidstrap: cleaner | Explicit log-only maintenance, retention 1–365 days, protected current log, no traversal or junction following; does not purge client downloads/caches |
| All three: appearance/navigation | Sidebar, filtered section navigation, persistent footer save action, dark/light palettes; six prior palettes retained |
| All three: shortcuts | Create shortcuts to the published Soultrap.exe on desktop / Start menu, without replacing an existing shortcut |

Additional working Soultrap settings: always-on-top, sidebar width, behavior after successful launch (keep open / minimize / close), tray, autostart, UI sounds/animations, opacity, client path and session-only launch URI.

**Import supported upstream settings** reads the original JSON, validates a copy of the local configuration, applies supported keys and reports unmapped property names in Diagnostics. Unknown cookie/token/account values are not displayed or applied. Original upstream files are never modified. Source-specific numeric enums and nested integration configurations are not guessed. `CustomFontScale` is Roblox font-resource scaling, not launcher zoom, and is explicitly not imported. Old Soultrap schema-1 settings retain their selected theme and receive defaults for new options.

## Explicitly not ported

Discord RPC, Spotify, matchmaking, account/cookie access, webview extensions, Studio integrations, download pipeline/channel control, arbitrary program integrations, injected graphics hooks, audio hooks and experimental input manipulation require integrations Soultrap does not have. Their configuration properties remain reference entries. They do not become working features just because a checkbox is added. This version does not recreate those services.

Arbitrary Roblox FastFlags remain local-edit-only without a current verified allowlist. Historic CoreGui is still preview-only. No new claims are made about in-game client compatibility.

## UI and license provenance

The startup window **adapts the MIT-licensed layout** of Voidstrap's FluentDialog (500×280, centred icon/message/detail/progress) and ClassicFluentDialog (icon and message arranged horizontally). Standard WPF controls replace upstream-specific WPF UI dependencies; Soultrap uses its own icon and real local startup progress. There is no Roblox download progress claim or artificial startup delay.

The main menu follows Voidstrap's sidebar/search/content/footer arrangement. It is adapted to Soultrap's independent MVVM services; the complete Voidstrap application and its brand assets are not bundled. Original copyright and MIT permission notices are preserved in `legal/VOIDSTRAP-LICENSE.txt`, `BLOXSTRAP-LICENSE.txt`, and `FISHSTRAP-LICENSE.txt`. Third-party cursor packs, fonts and Roblox art have not been copied.

Windows CI opens each page with Fluent splash, no splash, and Classic splash, checks binding errors, exercises navigation filtering and studio navigation, and captures rendered startup/Home/Settings/catalog PNGs in `artifacts/screenshots`. These tests do not establish Roblox gameplay functionality.

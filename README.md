# UltraShield

**UltraShield is an accessible desktop application for "digital self-defense"** —
part of the Ultra suite (which also includes Ultra Video Editor, Ultra Audio
Editor, and Ultra Studio), but a separate repo, since its purpose is security,
not creative work.

It isn't about one specific actor (e.g. the Lazarus group) but the general
threats every developer/user faces today: trojanized open-source packages,
fake job/recruiter scams, phishing, malicious files. The goal is for it to be
equally useful and accessible to blind and sighted users.

## What the app does

Three modules, one shell:

1. **Education** — short, concrete pages about real, current threats (fake
   job offers, trojanized npm/PyPI packages, fake crypto platforms,
   deepfake/fake IT workers, malicious extensions), each with a "what to do"
   tip.

2. **Scanner** — four modes:
   - npm package check (existence + heuristics: age, version count, repo field)
   - PyPI package check (same)
   - Single file check (SHA-256 hash against MalwareBazaar / VirusTotal)
   - **Full folder scan** — a recursive, on-demand scan of an entire
     folder/drive (like Malwarebytes' "Scan now" button — **not** real-time
     protection), with a progress bar, a local results cache (30 days, so it
     doesn't re-query the API for the same file), automatic respect for
     VirusTotal's rate limit, and **quarantine** (a flagged file is moved,
     not deleted — it can be restored)

3. **Checklist** — a security hygiene list (2FA, dependency audit, backups,
   etc.) with progress saved between runs.

## Why it's not a "real antivirus"

On purpose. Real-time protection (watching every file live, blocking before
execution) requires a kernel-level driver and Microsoft certification — a
completely different, much bigger undertaking. UltraShield does what
Malwarebytes' on-demand scan does: it checks when you run it, not constantly
in the background.

## Tech stack

WPF / .NET 8, the same approach as Ultra Video Editor — native WPF controls
have full UIA/Automation support for JAWS with zero extra work, which is why
TabControl/CheckBox/RadioButton weren't retemplated in a way that would hide
their automation pattern.

## Visual identity

- **Colors**: an ink-slate background (`#161B22`) with a muted bronze/gold
  accent (`#C9A24B`) — deliberately avoiding both the "cream + terracotta"
  and "black + neon" default looks. Verdicts (Clean/Suspicious/Malicious)
  have their own colors, but the verdict text is always present — color is
  never the sole carrier of information.
- **Typography**: Segoe UI (the Windows system font — always present, and
  exactly what JAWS users expect from a native app), with a clear weight
  scale (Semibold for headers, Regular for body, smaller/greyer for captions).
- Everything is defined in `Styles/Colors.xaml` and `Styles/Theme.xaml` as
  implicit styles — they apply automatically to every Button/TextBox/etc. in
  the app with no per-screen setup.
- The keyboard focus ring is deliberately visible (`BrushFocus`, blue, high
  contrast) — not cosmetic, part of the accessible "quality floor."

## Structure

```
UltraShield/
├── UltraShield.sln
├── LICENSE
└── UltraShield/
    ├── App.xaml(.cs)              — startup, exception handler, loads the theme
    ├── MainWindow.xaml(.cs)       — shell: header (shield + wordmark) + TabControl
    ├── AboutWindow.xaml(.cs)      — About dialog
    ├── Styles/
    │   ├── Colors.xaml            — color token system
    │   └── Theme.xaml             — typography + implicit control styles
    ├── Core/
    │   ├── ViewModelBase.cs
    │   ├── RelayCommand.cs
    │   └── Converters/
    │       └── VerdictToBrushConverter.cs
    ├── Modules/
    │   ├── Education/
    │   ├── Scanner/                — 4 modes + FindingItemViewModel for the folder-scan list
    │   └── Checklist/
    └── Services/
        ├── NpmPackageService.cs / PyPiPackageService.cs
        ├── MalwareBazaarService.cs / VirusTotalService.cs
        ├── FolderScannerService.cs / HashResultCache.cs
        ├── QuarantineService.cs
        ├── FileHasher.cs
        ├── AppSettingsService.cs / ChecklistPersistenceService.cs
        └── KnownMaliciousPackages.cs
```

## How to get a VirusTotal API key (free)

The VirusTotal check is optional — the MalwareBazaar part of the Scanner
works with no key at all. But for a second opinion from ~70 antivirus
engines, you need your own free key:

1. Go to **virustotal.com** and click **Sign up** (top right) — create an
   account (email + password, or via Google/GitHub).
2. After signing in, click your profile/avatar in the top-right corner →
   **API key**.
3. Copy the displayed key (a long string of letters and numbers).
4. In UltraShield, open the Scanner tab → expand the **"VirusTotal API key
   (optional)"** panel at the bottom → paste the key → **Save key**.
5. The key is stored only locally, in
   `%LocalAppData%\UltraShield\settings.json` on your machine — UltraShield
   never sends it anywhere except directly to VirusTotal's own API.

Note: a free (public) VirusTotal account has a limit of roughly 4
requests/minute and 500/day. For a single file check that's more than
enough; during a **Full folder scan**, the app automatically throttles its
calls to respect that limit (you'll see this as a slightly slower scan when
VirusTotal is enabled — that's expected, not a bug).

## What's done (functional, not just a skeleton)
- Scanner: real calls to the npm/PyPI registries, a local seed list of known
  malicious packages, SHA-256 hash + MalwareBazaar/VirusTotal, recursive
  folder scan with caching and quarantine
- Education: researched content (August 2026) on five threat types
- Checklist: state persistence
- Full visual identity (colors, typography, implicit styles, branded header)
- About dialog with credit

## Next steps (not done yet)
- `KnownMaliciousPackages` is a small manual seed — it should be connected to
  a real feed (Sonatype OSS Index or OSV.dev) for real coverage
- The VirusTotal key is stored as plain JSON — before a real release it's
  worth protecting with DPAPI (the `ProtectedData` class)
- Folder scan has no whitelist UI (the skip list is only hardcoded: .git,
  node_modules, bin, obj)
- No unit tests
- The app name is currently "UltraShield" — a working name, can change
- Installer script and GitHub Actions release workflow (same pattern as
  Video Editor, tag e.g. shield-v*)
- Firewall module (Outpost Firewall-style: per-app connection control,
  live connection visibility, prompt-on-new-connection) — planned, not
  started yet
- I couldn't compile it locally (no .NET SDK/Windows in this environment) —
  try a build in Visual Studio and let me know what (if anything) breaks

## Author

Created by **Demir Ajvazi**, part of the Ultra suite of accessible software.

## License

GPL-3.0 — see the [LICENSE](./LICENSE) file.

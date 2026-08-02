; ══════════════════════════════════════════════════════════════════════════
;  UltraShield.iss
;  Inno Setup installer for UltraShield
;
;  Much simpler than the Video/Audio Editor installers: UltraShield has no
;  external runtime dependencies to download (no Ollama, FFmpeg, Python,
;  etc.) - it's a plain WPF/.NET 8 app. This installer just copies the
;  already-built application and creates shortcuts.
;
;  BUILD-TIME DEPENDENCIES (on your machine, not the end user's):
;    - Inno Setup 6.x: https://jrsoftware.org/isinfo.php
;
;  BEFORE COMPILING, ADJUST (if needed):
;    - SetupIconFile, if you have an .ico (waiting on the logo)
;    - #define MyAppSourceDir below - verify it matches your current build
;      output folder.
;
;  CI/GitHub Actions: MyAppVersion and MyAppSourceDir can be overridden
;  from outside via "ISCC /DMyAppVersion=... /DMyAppSourceDir=...
;  UltraShield.iss" without touching this file - the local default below
;  stays untouched for manual testing.
; ══════════════════════════════════════════════════════════════════════════

#define MyAppName "UltraShield"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "Demir Ajvazi"
#define MyAppURL "https://github.com/demirajvazi10-max/UltraShield"
#define MyAppExeName "UltraShield.exe"

; Folder where your build output lives (.exe + DLLs). For a real release,
; build in the Release configuration first (Build > Rebuild), not Debug -
; Debug is unoptimized and carries extra .pdb symbols nobody outside of you
; needs. Adjust this path to match your actual checkout location.
#ifndef MyAppSourceDir
  #define MyAppSourceDir "C:\Users\Ajvazi\source\repos\UltraShield\UltraShield\bin\Release\net8.0-windows"
#endif

[Setup]
; Unique GUID, generated for this app specifically - do not reuse the
; Video/Audio Editor GUIDs, or Windows will treat this as the same
; application as one of those.
AppId={{737F5B7C-DE86-4830-9726-D2100108DABB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputBaseFilename=UltraShieldSetup-{#MyAppVersion}
OutputDir=Output
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
; SetupIconFile=app.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
; UltraShield's UI is English-only for now (no LanguageManager/Lang.cs yet,
; unlike the Video/Audio Editor) - so the installer only needs one language.
; If localization gets added later, mirror the German/Serbian setup from
; UltraVideoEditor.iss / UltraAudioEditor.iss here.

[CustomMessages]
english.DesktopIconGroup=Additional icons:
english.DesktopIconTaskName=Create a desktop icon

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIconTaskName}"; GroupDescription: "{cm:DesktopIconGroup}"

[Files]
; *.pdb                    - debug symbols, not needed by end users.
; runtimes\ios*/linux*/osx*/android*
;                           - non-Windows native binaries some NuGet
;                             packages pull in by default; this app only
;                             ever runs on Windows.
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "*.pdb,runtimes\ios*,runtimes\linux*,runtimes\osx*,runtimes\android*"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

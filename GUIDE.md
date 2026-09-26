# Wiggly Snake — Build, Run & Sign Guide

Everything below assumes you've cloned the repo and are on the `app` branch:

```bash
git clone <your-fork-or-this-repo-url> Snake-CSharp
cd Snake-CSharp
git checkout app
```

The MAUI project lives in `SnakeGame.Maui/`. The original Blazor web game
(`SnakeGame.csproj` at the repo root) is untouched — this is a separate,
additional app, not a replacement.

---

## 0. One-time machine setup

```bash
# .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
dotnet --version        # confirm it shows a 10.x version

# MAUI workload (run once; re-run after major .NET/VS updates)
dotnet workload install maui

# Restore
cd Snake-CSharp
dotnet restore SnakeGame.Maui/SnakeGame.Maui.csproj
```

Platform extras:
- **Android**: nothing extra needed the first time — `dotnet build -f net10.0-android`
  auto-downloads the Android SDK/NDK components it needs. Alternatively install
  Android Studio and let it manage the SDK.
- **iOS / macOS**: **must be on a Mac.** Install Xcode from the App Store, then
  `xcode-select --install`. Sign in with your Apple ID in Xcode → Settings →
  Accounts (a free, ordinary Apple ID is enough for everything in §3 below).
- **Windows**: nothing extra — the Windows App SDK restores via NuGet.

You can open `SnakeGame.sln` in Visual Studio 2022 (with the ".NET Multi-platform
App UI development" workload) instead of using the CLI for any step below.

---

## 1. Run it locally (fastest way to see it working)

```bash
cd SnakeGame.Maui

# Windows machine, Windows app window:
dotnet build -t:Run -f net10.0-windows10.0.19041.0

# Mac, as a desktop app (Mac Catalyst):
dotnet build -t:Run -f net10.0-maccatalyst

# Android emulator or plugged-in Android device (any OS):
dotnet build -t:Run -f net10.0-android

# iOS Simulator (Mac only):
dotnet build -t:Run -f net10.0-ios
```

---

## 2. Android — distributable, signed `.apk`

You do **not** need any account for this — signing your own APK is free and
doesn't require Google's involvement at all (only *publishing to the Play
Store* needs Google's one-time $25 developer fee, and that's optional).

```bash
cd SnakeGame.Maui

# Create your signing key once. Keep this file and password safe forever —
# you need the exact same key for every future update of the same app.
keytool -genkeypair -v -keystore wigglysnake.keystore -alias wigglysnake \
  -keyalg RSA -keysize 2048 -validity 10000

# Publish a signed, installable release APK:
dotnet publish -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=wigglysnake.keystore \
  -p:AndroidSigningKeyAlias=wigglysnake \
  -p:AndroidSigningKeyPass=<your-password> \
  -p:AndroidSigningStorePass=<your-password>

# Output:
#   bin/Release/net10.0-android/publish/com.rjporosh.wigglysnake-Signed.apk
```

Copy that `.apk` to any Android phone, tablet, or Android TV box and install
it directly (enable "Install unknown apps" for your file manager/browser once,
in Android Settings → Security). It's a real, signed, standalone distributable
— shareable via email, USB, cloud drive, or your own website.

For the Play Store instead, add `-p:AndroidPackageFormat=aab` to get an
`.aab` bundle, and upload it in the Play Console (this is where the optional
$25 one-time Google account fee applies — only for *Play Store listing*, not
for building or sideloading).

---

## 3. iPhone / iPad — **without** a paid Apple Developer account

Apple's rule (not this project's limitation): **a completely free Apple ID
lets you build, sign, and run the app on your own physical iPhone/iPad**, but
Apple does not allow a "distributable .ipa you can just hand to anyone" without
a paid Apple Developer Program membership ($99/yr) — that restriction is
enforced by iOS itself (every app must be signed by a certificate Apple
recognizes, and free-tier certificates only work on devices you've personally
registered in Xcode). There is no way around this from the code side; it's
identical for every iOS app, not specific to this one.

**What you *can* do for free — install straight onto your own iPhone/iPad:**

1. On a Mac, open `SnakeGame.sln` in Visual Studio 2022 or Xcode via
   `open SnakeGame.Maui/Platforms/iOS`.
2. Plug your iPhone/iPad into the Mac with a cable, unlock it, and tap
   "Trust This Computer" if prompted.
3. In Xcode/Visual Studio, sign in with your (free) Apple ID under
   Settings → Accounts, select your device as the run target, and hit Run —
   Xcode automatically creates a free personal-team signing certificate and
   installs the app straight onto your device.
   CLI equivalent:
   ```bash
   dotnet build -t:Run -f net10.0-ios -p:_DeviceName=<your-device-name>
   ```
4. The app now lives on your Home Screen like any other app. The one
   catch with the free tier: iOS requires you to re-launch it from Xcode (or
   re-trust it in Settings → General → VPN & Device Management) roughly every
   7 days, since free personal-team certificates expire weekly. A paid
   Developer Program membership removes that 7-day limit and is what's
   required to produce a shareable `.ipa` file for other people's devices,
   TestFlight, or the App Store.

**If you do get a paid Apple Developer account later**, building an
installable/shareable `.ipa` is:

```bash
dotnet publish -f net10.0-ios -c Release \
  -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Distribution: <you>" \
  -p:CodesignProvision="<distribution provisioning profile name>"
# → bin/Release/net10.0-ios/ios-arm64/publish/WigglySnake.ipa
```

---

## 4. Mac desktop/laptop — `.app` (and `.pkg`/`.dmg` if you want an installer)

Also free — a Mac Catalyst app you build and sign with your own free Apple
ID runs on your own Mac with no restrictions (the 7-day re-sign rule from §3
is an iOS-only thing; Mac apps signed this way keep working).

```bash
cd SnakeGame.Maui

# Just run it locally:
dotnet build -t:Run -f net10.0-maccatalyst

# Build a distributable .app + .pkg installer:
dotnet publish -f net10.0-maccatalyst -c Release \
  -p:CreatePackage=true -p:EnableCodeSigning=true \
  -p:CodesignKey="Apple Development: <you>"
# → bin/Release/net10.0-maccatalyst/maccatalyst-x64/publish/WigglySnake-1.0.0.0.pkg

# Turn the .app into a drag-and-drop .dmg (hdiutil ships with macOS):
APP="bin/Release/net10.0-maccatalyst/maccatalyst-x64/publish/WigglySnake.app"
hdiutil create -volname "Wiggly Snake" -srcfolder "$APP" -ov -format UDZO WigglySnake.dmg
```

Sharing a `.pkg`/`.dmg` signed only with a free "Apple Development" identity
with *other people's* Macs will trigger Gatekeeper warnings (right-click →
Open bypasses it once). Removing that warning for wide distribution needs the
same paid Developer ID as notarization — again an Apple platform rule, not
this project.

---

## 5. Windows desktop/laptop — `.exe`

No account, no signing required to run it on your own machine or share with
others (Windows shows an "Unknown publisher" SmartScreen prompt for unsigned
exes — click "More info" → "Run anyway" once; a paid code-signing certificate
removes that prompt but isn't required to run the app).

```bash
cd SnakeGame.Maui
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -r win10-x64 --self-contained
# → bin/Release/net10.0-windows10.0.19041.0/win10-x64/publish/WigglySnake.exe
#   (zip the whole publish/ folder — the .exe needs its sibling .dlls)
```

---

## 6. Responsiveness — phone / tablet / TV coverage (mandatory targets)

All handled by one shared layout (`MainPage.xaml` + `GameBoardView`) that
squares itself to whatever space it's given — no separate phone/tablet/TV
code paths to maintain:

- **Android phone & tablet**: works out of the box; the manifest doesn't lock
  orientation, so portrait and landscape both just re-center the same layout.
- **Android TV**: `AndroidManifest.xml` already declares
  `android.software.leanback` support, and every on-screen control is a
  regular focusable `Button`, so a TV remote's D-pad can navigate and press
  the on-screen arrows/pause/play button. (A remote-native input mapping —
  so physical D-pad *presses* move the snake directly, the way arrow keys do
  on Windows — is the one polish item flagged in `ai-handover.md`.)
- **iPad / iPhone**: `Info.plist` allows portrait + all landscape orientations
  on iPhone, and all four orientations on iPad; same shared layout.
- **Apple TV (tvOS) / Apple Watch (watchOS) / Wear OS**: **not supported —
  and this is a hard platform limitation, not something left unfinished.**
  .NET MAUI has no tvOS, watchOS, or Wear OS target at all; those platforms
  need entirely separate native projects (Swift/SwiftUI for tvOS/watchOS,
  Kotlin/Compose for Wear OS) that share no code with a MAUI app. Since you
  marked phone/tablet/TV as the mandatory set and Android TV support is
  included, this doesn't block anything mandatory — it's called out here so
  it's not mistaken for a bug.

---

## 7. If a build fails

This project was authored in a sandbox with **no .NET SDK, no Android/Apple/
Windows toolchains, and no network access to nuget.org**, so nothing above
could be compiled or run before reaching you — you are the first real build.
The code follows standard, documented MAUI conventions, but MAUI/.NET 10's
exact API surface can shift between preview builds. If `dotnet build`/
`publish` errors out, it is most likely one of:
- a workload not installed (`dotnet workload install maui` again), or
- a small API rename between the .NET 10 version this was written against
  and the one you have installed.

See `ai-handover.md` for exactly what to check first and how to hand the
error back to an AI assistant (or a person) to fix without re-doing any of
the finished work.

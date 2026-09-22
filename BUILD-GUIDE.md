# Wiggly Snake — MAUI build & distribution guide

This guide walks you from a clean checkout of the `app` branch to a signed,
distributable file for each platform: **.apk** (Android), **.ipa** (iOS),
**.pkg** / **.dmg** (macOS via Mac Catalyst), and **.exe** / **.msix**
(Windows).

> **Read this first — where each package can be built**
> Apple's tools (Xcode, `codesign`, `xcrun`) only run on macOS, and Apple only
> lets you produce a signed .ipa/.pkg/.dmg from a Mac with an Apple ID (free
> for testing on your own device; a paid Apple Developer Program membership,
> $99/yr, for TestFlight/App Store distribution). The Android and Windows
> outputs can be built on Windows, macOS or Linux. There is no way around the
> Apple requirement — it's an Apple platform policy, not a project limitation.

| Target        | File     | Must be built on         | Needs a paid account? |
|---------------|----------|---------------------------|------------------------|
| Android       | `.apk`   | Windows / macOS / Linux   | No (Play Store: yes, $25 one-time) |
| Android (bundle) | `.aab` | Windows / macOS / Linux | No (required for Play Store) |
| iOS           | `.ipa`   | **macOS only**             | Free tier: install on your own device. Distribution/TestFlight: Apple Developer Program |
| Mac Catalyst  | `.pkg`/`.dmg` | **macOS only**        | Free tier: local/Gatekeeper-bypass use. Notarized distribution: Apple Developer Program |
| Windows       | `.exe`   | Windows (or `-r win10-x64` from any OS, unsigned) | No |
| Windows (store) | `.msix` | Windows                 | No for sideload; Microsoft Store: free dev account |

---

## 0. One-time setup

```bash
# 1. .NET 10 SDK
#    https://dotnet.microsoft.com/download/dotnet/10.0

# 2. Install the MAUI workload (run once, and again after every major .NET update)
dotnet workload install maui

# 3. Platform-specific extras
#    Android : Android SDK + a JDK — installed automatically the first time
#              you build for -android, or via Visual Studio's Mobile
#              Development workload / Android Studio's SDK Manager.
#    iOS/Mac : Xcode from the Mac App Store (macOS host only), then:
#              xcode-select --install
#    Windows : "Windows App SDK" is restored automatically via NuGet; on a
#              non-Windows host you can still build the *library* for
#              net10.0-windows but only a Windows machine can produce a
#              runnable .exe.

# 4. Restore
git clone <your-fork-url> Snake-CSharp
cd Snake-CSharp
git checkout app
dotnet restore SnakeGame.Maui/SnakeGame.Maui.csproj
```

Open `SnakeGame.sln` in Visual Studio 2022 (17.11+, with the ".NET Multi-platform
App UI development" workload) or Visual Studio Code with the MAUI extension if
you'd rather use an IDE than the CLI for the steps below.

---

## 1. Android — `.apk`

```bash
cd SnakeGame.Maui

# Debug build, deploy straight to a plugged-in device/emulator:
dotnet build -t:Run -f net10.0-android

# Release, unsigned/debug-signed APK (good enough to sideload for testing):
dotnet publish -f net10.0-android -c Release

# The .apk lands in:
#   bin/Release/net10.0-android/publish/com.rjporosh.wigglysnake-Signed.apk
```

### Signing a release APK properly (for real distribution)

```bash
# Generate a keystore once, keep it safe — you need the SAME one for every update:
keytool -genkeypair -v -keystore wigglysnake.keystore -alias wigglysnake \
  -keyalg RSA -keysize 2048 -validity 10000

# Then publish with signing properties:
dotnet publish -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=wigglysnake.keystore \
  -p:AndroidSigningKeyAlias=wigglysnake \
  -p:AndroidSigningKeyPass=<your-password> \
  -p:AndroidSigningStorePass=<your-password>
```

For the Play Store, publish an `.aab` instead (add `-p:AndroidPackageFormat=aab`)
and upload it to the Play Console.

---

## 2. iOS — `.ipa`  *(macOS + Xcode required)*

```bash
# One-time: register your Apple ID in Xcode → Settings → Accounts.

cd SnakeGame.Maui

# Build & run on the Simulator (no signing needed):
dotnet build -t:Run -f net10.0-ios -p:_DeviceName=:v2:udid=<simulator-udid>

# Archive for a real device / distribution:
dotnet publish -f net10.0-ios -c Release \
  -p:ArchiveOnBuild=true \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Development: <you>" \
  -p:CodesignProvision="<provisioning profile name>"

# The .ipa is written under:
#   bin/Release/net10.0-ios/ios-arm64/publish/WigglySnake.ipa
```

If you don't yet have a provisioning profile, the simplest path is: open
`SnakeGame.sln` in Visual Studio for Mac/VS 2022 (Mac build host) or run
`dotnet build -t:Run -f net10.0-ios` with a device plugged in — Xcode/VS will
offer to create a free personal-team signing identity for you automatically.

For App Store/TestFlight distribution you need an Apple Developer Program
membership, an **App Store** provisioning profile/distribution certificate,
and to upload the `.ipa` with Transporter or `xcrun altool`.

---

## 3. Mac Catalyst — `.pkg` / `.dmg`  *(macOS + Xcode required)*

```bash
cd SnakeGame.Maui

dotnet publish -f net10.0-maccatalyst -c Release \
  -p:CreatePackage=true \
  -p:EnableCodeSigning=true \
  -p:CodesignKey="Apple Development: <you>"

# This produces a signed .app bundle and, with CreatePackage=true, a .pkg
# installer under:
#   bin/Release/net10.0-maccatalyst/maccatalyst-x64/publish/WigglySnake-1.0.0.0.pkg
```

To also produce a drag-and-drop `.dmg` from the `.app` bundle:

```bash
# hdiutil ships with macOS — no extra tools needed.
APP="bin/Release/net10.0-maccatalyst/maccatalyst-x64/publish/WigglySnake.app"
hdiutil create -volname "Wiggly Snake" -srcfolder "$APP" \
  -ov -format UDZO WigglySnake.dmg
```

For notarized distribution outside the Mac App Store, run
`xcrun notarytool submit WigglySnake.dmg --keychain-profile "<profile>" --wait`
after signing with a **Developer ID Application** certificate (requires the
paid Apple Developer Program).

---

## 4. Windows — `.exe` / `.msix`

```bash
cd SnakeGame.Maui

# Plain, portable, unsigned .exe (WindowsPackageType is already "None" in
# the csproj, so this is the default):
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -r win10-x64 --self-contained

# Output:
#   bin/Release/net10.0-windows10.0.19041.0/win10-x64/publish/WigglySnake.exe
#   (plus its dependent .dlls in the same folder — zip the folder to share it)
```

### Packaged `.msix` (Microsoft Store or sideload with auto-update)

```bash
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -r win10-x64 \
  -p:WindowsPackageType=MSIX -p:GenerateAppxPackageOnBuild=true
```

An unsigned `.msix` can be sideloaded on a dev machine after enabling
"Developer Mode" in Windows Settings and either installing your own
self-signed test certificate or right-clicking → Properties → Digital
Signatures on the package. For the Microsoft Store, associate the app with a
Store listing in Visual Studio (Project → Publish → Associate App with the
Store) — no separate certificate needed, Microsoft signs it for you.

---

## 5. Building every platform from one CI run

`.github/workflows/maui-build.yml` (added on this branch) builds the Android
APK and the Windows EXE on every push to `app` using GitHub-hosted `windows`
and `ubuntu` runners (no Mac required for those two), and additionally builds
the iOS and Mac Catalyst archives on a `macos` runner — GitHub-hosted macOS
runners come with Xcode preinstalled. Unsigned artifacts are uploaded for
every push; add your signing secrets (`ANDROID_KEYSTORE_BASE64`,
`APPLE_CERTIFICATE_BASE64`, `APPLE_PROVISIONING_PROFILE_BASE64`, etc., as
repository secrets) to produce signed, installable output straight from CI.

---

## 6. Quick sanity check without any platform SDK

```bash
dotnet build SnakeGame.Maui/SnakeGame.Maui.csproj -f net10.0-android
```

If this fails immediately with a workload error, re-run step 0.2
(`dotnet workload install maui`) — that's almost always the cause.

---

## What this repo does *not* include

This project was authored and committed from an environment with **no .NET
SDK, no Android/Apple/Windows toolchains, and no network access to
nuget.org**, so none of the above commands could be run or verified here —
the code follows standard, documented .NET MAUI project conventions, but you
are the first one to actually compile it. If a build error comes up, it is
most likely a small, fixable API-surface mismatch (MAUI/.NET 10 moves fast) —
open an issue or share the error and it can be patched.

# AI Handover — Wiggly Snake MAUI port

Read this before touching anything. It tells you exactly what's done, what's
unverified, and the precise next command to run. Branch: `app`. Do not repeat
work already listed as done — this file exists so you don't have to re-derive
the state from scratch.

## TL;DR for the next agent

```bash
git checkout app
dotnet workload install maui
dotnet restore SnakeGame.Maui/SnakeGame.Maui.csproj
dotnet build SnakeGame.Maui/SnakeGame.Maui.csproj -f net10.0-android
```

Run that build first. It is the fastest way to surface the first real
compiler error (this environment could not run `dotnet` at all — see
"What was never verified" below). Fix errors one at a time, commit each fix
separately with a `fix(maui): ...` message (see commit style in `git log`),
and do **not** rewrite or reorder the existing commits on `app` — they're
kept as a clean, reviewable history of how the port was built.

## What is complete and committed (branch `app`, 9 commits on top of `master`)

| Commit | What it did |
|---|---|
| `chore(maui): scaffold...` | `SnakeGame.Maui.csproj` (net10.0, android/ios/maccatalyst/windows), `MauiProgram`, `App`, `AppShell` |
| `feat(engine): port core Snake game rules...` | `GameEngine.cs` — pure C#, no UI dependency, 1:1 logic port of `Pages/Home.razor`'s `@code` block |
| `feat(services): add high-score persistence and generated-tone audio` | `Services/HighScoreService.cs` (MAUI `Preferences`), `Services/AudioService.cs` (platform tone/beep APIs, no bundled audio files) |
| `feat(ui): add GraphicsView board renderer...` | `Controls/GameBoardView.cs` — draws the board/snake/food to match `wwwroot/css/app.css` exactly |
| `feat(ui): build responsive MainPage...` | `MainPage.xaml(.cs)` — header, scoreboard, overlays, D-pad, swipe/tap gestures, Dispatcher-timer game loop, Windows keyboard hook |
| `feat(platforms): add Android, iOS, Mac Catalyst and Windows heads` | Per-platform entry points/manifests |
| `chore: ignore MAUI build output...` | `.gitignore` additions for MAUI build artifacts |
| `docs: add solution file, multi-platform build guide and CI workflow` | `SnakeGame.sln`, `BUILD-GUIDE.md` (superseded by the more complete `GUIDE.md` at repo root — keep both or fold one into the other, your call), `.github/workflows/maui-build.yml` |
| `fix(maui): correct XAML comment syntax and Mac Catalyst entitlements wiring` | Fixed invalid `<!-- ---- -->` XML comments in `MainPage.xaml` (double-dashes are illegal inside XML comments — this would have failed the XAML compiler), wired `CodesignEntitlements`, removed a dead `MauiImage` line |

Also present but **not yet committed as part of the history above** (added in
this session after the handover request): `GUIDE.md` (build/run/sign guide
including no-developer-account paths for iOS), this file. Commit these before
doing anything else — see "Immediate next command" below.

## What was never verified (be skeptical of it first)

This entire project was written in a sandbox with:
- **no `dotnet` CLI / .NET SDK at all** (`which dotnet` → not found)
- **no Android SDK, no Xcode, no Windows SDK**
- **no network access to `nuget.org`** (only npm/PyPI/crates/GitHub domains
  were reachable) — so `dotnet workload install maui` itself could not even
  be run here.

Concretely, **nothing in `SnakeGame.Maui/` has been compiled, restored, or
run, ever.** Every file was hand-written against known MAUI/.NET conventions
and manually checked for:
- brace/paren balance (scripted check, passed)
- XML well-formedness of every `.xaml`/`.plist`/`.xml`/`.appxmanifest`/`.svg`
  (scripted check — caught and fixed the double-dash comment bug above)

...but never a real compile. Treat every `.cs`/`.xaml` file as "logically
reviewed, never built." The most likely failure points, roughly in order of
likelihood, if you hit a build error:

1. **`Border.StrokeShape="RoundRectangle 16"` string syntax** in
   `MainPage.xaml` — this shorthand is correct for recent MAUI versions but
   double-check against whatever MAUI version `dotnet workload install maui`
   actually pulls in for .NET 10.
2. **`Element.Dispatcher` / `Dispatcher.CreateTimer()`** usage in
   `MainPage.xaml.cs` — standard MAUI API, but confirm the namespace resolves
   without an extra `using Microsoft.Maui.Dispatching;` if the compiler
   complains.
3. **Windows keyboard hook** (`#if WINDOWS` block at the bottom of
   `MainPage.xaml.cs`) — casts `Handler.PlatformView` to
   `Microsoft.UI.Xaml.FrameworkElement`. If MAUI's Windows handler wraps the
   page differently in your installed version, this cast may need adjusting
   (e.g. to `Microsoft.UI.Xaml.Controls.Page` or similar) — it's isolated
   inside one `#if WINDOWS` block so it can't break other platforms even if
   wrong.
4. **`net10.0-windows10.0.19041.0` TFM string** — if .NET 10's actual MAUI
   Windows TFM differs slightly (e.g. a different SDK version number) by the
   time you're building, update it in `SnakeGame.Maui.csproj` and in
   `GUIDE.md`/`BUILD-GUIDE.md`'s Windows commands (they're duplicated in a
   few places — search-and-replace across the repo).
5. **Icon/splash SVG generation** — `Resources/AppIcon/appicon.svg`,
   `appiconfg.svg`, `Resources/Splash/splash.svg` are hand-authored vector
   files (no emoji fonts, no external assets). MAUI's icon generator
   (`resvg`-based) should handle them fine, but if it errors on any SVG
   feature, simplify the offending `<path>`/gradient rather than debugging
   the generator itself — the exact pixel art doesn't matter, only that it
   builds.

## What was intentionally left simpler (not a bug, don't "fix" without asking)

- **No custom "Fredoka" font bundled** (no internet access to fetch the
  `.ttf` here). System default font is used instead. See
  `SnakeGame.Maui/README.md` for how to add it back if wanted.
- **No bundled audio files** — sound uses each OS's built-in
  tone/beep primitive (`Services/AudioService.cs`). This was a deliberate
  choice to avoid needing any new binary assets or NuGet audio packages, and
  mirrors the original's "generated Web Audio, no files" approach.
- **Keyboard input is wired for Windows only** (see `#if WINDOWS` block in
  `MainPage.xaml.cs`). Android TV remotes and iOS/Mac Catalyst hardware
  keyboards currently rely on the on-screen D-pad buttons receiving focus and
  D-pad/remote/trackpad clicks — functional, but not as polished as a native
  key/controller mapping. Flagged as a nice-to-have, not required for the
  phone/tablet/TV mandatory targets.
- **watchOS / Wear OS / tvOS are out of scope** — .NET MAUI has no target for
  any of them. This is an upstream platform limitation, not something to
  "finish" here (see `GUIDE.md` §6 for the full explanation to give the
  user if asked again).

## Immediate next command (do this first, in order)

```bash
# 1. Commit the two guide files added in this session, if not already committed:
git add GUIDE.md ai-handover.md
git commit -m "docs: add GUIDE.md and ai-handover.md for build/sign steps and session handoff"

# 2. Then attempt the first real compile:
dotnet workload install maui
dotnet restore SnakeGame.Maui/SnakeGame.Maui.csproj
dotnet build SnakeGame.Maui/SnakeGame.Maui.csproj -f net10.0-android -c Debug

# 3. Fix whatever the compiler reports, one error at a time, committing each
#    fix separately (small, reviewable commits — match the existing
#    `fix(maui): <what and why>` style already in `git log --oneline`).

# 4. Repeat step 2 for the other three TFMs once Android compiles clean:
dotnet build SnakeGame.Maui/SnakeGame.Maui.csproj -f net10.0-ios          # needs a Mac
dotnet build SnakeGame.Maui/SnakeGame.Maui.csproj -f net10.0-maccatalyst  # needs a Mac
dotnet build SnakeGame.Maui/SnakeGame.Maui.csproj -f net10.0-windows10.0.19041.0  # needs Windows
```

## Ground rules for whoever continues this

- **Don't touch anything under `Pages/`, `Layout/`, `wwwroot/`, `App.razor`,
  `Program.cs`, `SnakeGame.csproj`, or the root `README.md`** — that's the
  original Blazor project; it must stay exactly as it was (the user's
  explicit requirement).
- **Don't rewrite existing `app`-branch commit history.** Add new commits on
  top. If you must amend something from this session specifically because it
  hasn't been shared/reviewed yet, say so explicitly rather than silently
  force-pushing.
- **Preserve the visual design** in `Controls/GameBoardView.cs` and
  `Resources/Styles/Colors.xaml` — colors/gradients were copied 1:1 from
  `wwwroot/css/app.css`. If a build fix requires changing a color/shape API
  call, keep the resulting *visual output* the same, don't simplify the
  palette away.
- **Keep the single shared responsive layout.** Don't fork `MainPage.xaml`
  into separate phone/tablet/TV layouts — the whole point of the current
  design is one layout that scales, per the user's "same design fit to
  screen" requirement.

# Wiggly Snake — .NET MAUI

A native port of the [Blazor WebAssembly Wiggly Snake game](../README.md) to
.NET 10 MAUI, targeting Android, iOS, Mac Catalyst (macOS) and Windows from
one shared codebase — phone, tablet, TV, watch-sized and desktop screens, in
both portrait and landscape.

Building distributables (`.apk`, `.ipa`, `.pkg`/`.dmg`, `.exe`/`.msix`)?
See **[../BUILD-GUIDE.md](../BUILD-GUIDE.md)**.

## Architecture

```
GameEngine.cs           Pure C# game rules (grid, collisions, scoring, state
                         machine) — no UI or platform code. Ported straight
                         from Pages/Home.razor's @code block.
Controls/
  GameBoardView.cs       GraphicsView + IDrawable board renderer. Draws the
                         checkerboard, snake gradient, head/eyes and food
                         emoji using the same palette as wwwroot/css/app.css.
Services/
  IHighScoreService.cs   Best-score persistence (MAUI Preferences).
  IAudioService.cs       Two short effects (eat / game over) via each
                         platform's built-in tone facility — no bundled
                         audio files, no extra NuGet packages.
MainPage.xaml(.cs)       Header, scoreboard, board + state overlay, D-pad.
                         Owns the Dispatcher-timer game loop and all input
                         (touch/swipe, on-screen buttons, physical keyboard
                         on Windows).
Platforms/               Per-platform entry points and manifests.
Resources/                Vector app icon, splash screen, color/style
                         resource dictionaries.
```

The engine is intentionally UI-agnostic: it raises `Changed` / `FoodEaten` /
`GameOver` events instead of touching any control, so it can be unit tested
on its own (`new GameEngine()`, feed it `SetDirection`/`Tick()` calls, assert
on `Score`/`State`) without spinning up MAUI at all.

## What was intentionally kept "simpler" than a production app

- **No custom font bundled.** The web app uses the "Fredoka" Google Font;
  it isn't included here (no network access to fetch it while this project
  was authored). Drop `Fredoka-Regular.ttf`/`-SemiBold.ttf` into
  `Resources/Fonts` and wire them up in `MauiProgram.ConfigureFonts(...)` to
  match exactly — everything else already uses the right colors/sizes.
- **No bundled audio files.** Sound effects use each OS's built-in
  tone/beep API (see `Services/AudioService.cs`) rather than shipping .wav
  assets — same "no audio files needed" spirit as the original's generated
  Web Audio beeps.
- **Keyboard input is wired for Windows** (the desktop platform where a
  physical keyboard is the norm). Android/iOS/Mac Catalyst rely on
  touch/swipe, the on-screen D-pad, and mouse/trackpad clicks on those same
  buttons — extending native keyboard/game-controller support to those
  platforms is a small, isolated follow-up (see `MainPage.xaml.cs`).

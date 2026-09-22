using Microsoft.Extensions.Logging;
using WigglySnake.Services;

namespace WigglySnake;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // The web version uses the "Fredoka" Google Font. It isn't bundled
        // here (no internet access at authoring time to fetch the .ttf), so
        // the app uses each platform's default system font instead — drop
        // Fredoka-Regular.ttf / Fredoka-SemiBold.ttf into Resources/Fonts and
        // wire them up with builder.ConfigureFonts(...) to match exactly.

        // Services are registered as singletons: one game session, one
        // high-score store and one audio service live for the app's lifetime.
        builder.Services.AddSingleton<IHighScoreService, HighScoreService>();
        builder.Services.AddSingleton<IAudioService, AudioService>();
        builder.Services.AddSingleton<GameEngine>(sp =>
        {
            var highScores = sp.GetRequiredService<IHighScoreService>();
            return new GameEngine(highScores.Load());
        });
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

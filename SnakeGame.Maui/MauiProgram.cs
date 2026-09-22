using Microsoft.Extensions.Logging;
using WigglySnake.Services;

namespace WigglySnake;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Fredoka-Regular.ttf", "FredokaRegular");
                fonts.AddFont("Fredoka-SemiBold.ttf", "FredokaSemiBold");
            });

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

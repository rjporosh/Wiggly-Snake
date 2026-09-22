namespace WigglySnake.Services;

/// <summary>
/// Stores the high score with .NET MAUI's cross-platform <see cref="Preferences"/> API
/// (backed by NSUserDefaults on Apple platforms, SharedPreferences on Android,
/// ApplicationDataContainer on Windows) — no extra packages required.
/// </summary>
public sealed class HighScoreService : IHighScoreService
{
    private const string Key = "wiggly_snake_high_score";

    public int Load() => Preferences.Default.Get(Key, 0);

    public void Save(int score) => Preferences.Default.Set(Key, score);
}

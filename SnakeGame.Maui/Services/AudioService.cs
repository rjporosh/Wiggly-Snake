using System.Runtime.InteropServices;

#if ANDROID
using Android.Media;
#endif

namespace WigglySnake.Services;

/// <summary>
/// Plays two short, generated-feel effects: a cheerful "eat" blip and a low
/// "game over" tone. Each platform uses whatever built-in synth/beep facility
/// it already ships with, so the app needs zero bundled audio files and zero
/// extra NuGet packages — the same "no audio files needed" spirit as the
/// original Blazor version's Web Audio implementation.
/// </summary>
public sealed class AudioService : IAudioService
{
    public bool Muted { get; set; }

    public void PlayEat()
    {
        if (Muted) return;
        PlayTone(highPitched: true);
    }

    public void PlayGameOver()
    {
        if (Muted) return;
        PlayTone(highPitched: false);
    }

    private static void PlayTone(bool highPitched)
    {
        try
        {
#if ANDROID
            using var tone = new ToneGenerator(Stream.Music, 90);
            tone.StartTone(highPitched ? Tone.PropBeep2 : Tone.PropNack, 160);
#elif IOS || MACCATALYST
            // A short, standard system sound; no bundled asset required.
            AudioServicesPlaySystemSound(highPitched ? 1104u /* SMS received */ : 1053u /* tock */);
#elif WINDOWS
            MessageBeep(highPitched ? 0x00000040u /* MB_ICONASTERISK */ : 0x00000010u /* MB_ICONHAND */);
#endif
        }
        catch
        {
            // Audio is a nice-to-have; never let a platform quirk crash gameplay.
        }
    }

#if IOS || MACCATALYST
    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern void AudioServicesPlaySystemSound(uint soundId);
#endif

#if WINDOWS
    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);
#endif
}

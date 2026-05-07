namespace TinySynth.Synth;

internal static class MidiUtilities
{
    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    public static bool IsWhiteKey(int midiNote)
    {
        return midiNote % 12 is 0 or 2 or 4 or 5 or 7 or 9 or 11;
    }

    public static float MidiToFrequency(int midiNote)
    {
        return 440f * MathF.Pow(2f, (midiNote - 69) / 12f);
    }

    public static string MidiToNoteName(int midiNote)
    {
        int octave = (midiNote / 12) - 1;
        return $"{NoteNames[midiNote % 12]}{octave}";
    }
}

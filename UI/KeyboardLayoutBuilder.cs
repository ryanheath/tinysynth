using Raylib_CSharp.Transformations;
using TinySynth.Synth;

namespace TinySynth.UI;

internal static class KeyboardLayoutBuilder
{
    public static PianoKeyLayout[] Build(Rectangle bounds, int startingMidi, int noteCount)
    {
        int whiteKeyCount = 0;

        for (int i = 0; i < noteCount; i++)
        {
            if (MidiUtilities.IsWhiteKey(startingMidi + i))
            {
                whiteKeyCount++;
            }
        }

        float topPadding = 64f;
        float bottomPadding = 10f;
        float whiteKeyWidth = bounds.Width / whiteKeyCount;
        float whiteKeyHeight = MathF.Max(52f, bounds.Height - topPadding - bottomPadding);
        float blackKeyWidth = whiteKeyWidth * 0.62f;
        float blackKeyHeight = whiteKeyHeight * 0.58f;
        float currentX = bounds.X;

        List<PianoKeyLayout> whiteKeys = [];
        List<PianoKeyLayout> blackKeys = [];
        bool hasPreviousWhite = false;

        for (int i = 0; i < noteCount; i++)
        {
            int midiNote = startingMidi + i;
            string label = MidiUtilities.MidiToNoteName(midiNote);

            if (MidiUtilities.IsWhiteKey(midiNote))
            {
                Rectangle keyBounds = new(currentX, bounds.Y + topPadding, whiteKeyWidth, whiteKeyHeight);
                whiteKeys.Add(new PianoKeyLayout(midiNote, false, keyBounds, label));
                hasPreviousWhite = true;
                currentX += whiteKeyWidth;
            }
            else if (hasPreviousWhite)
            {
                Rectangle keyBounds = new(currentX - (blackKeyWidth / 2f), bounds.Y + topPadding, blackKeyWidth, blackKeyHeight);
                blackKeys.Add(new PianoKeyLayout(midiNote, true, keyBounds, label));
            }
        }

        return [.. whiteKeys, .. blackKeys];
    }

    public static int GetHoveredMidiNote(PianoKeyLayout[] keys, System.Numerics.Vector2 mousePosition)
    {
        foreach (PianoKeyLayout key in keys.Where(static key => key.IsBlack))
        {
            if (UiHitTesting.Contains(key.Bounds, mousePosition))
            {
                return key.MidiNote;
            }
        }

        foreach (PianoKeyLayout key in keys.Where(static key => !key.IsBlack))
        {
            if (UiHitTesting.Contains(key.Bounds, mousePosition))
            {
                return key.MidiNote;
            }
        }

        return -1;
    }

}

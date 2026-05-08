namespace TinySynth.Synth;

internal static class ChordUtilities
{
    private static readonly (int[] Intervals, string Suffix, int Score)[] ChordPatterns =
    [
        ([0, 4, 7, 11], "maj7", 100),
        ([0, 4, 7, 10], "7", 95),
        ([0, 3, 7, 10], "m7", 94),
        ([0, 3, 6, 10], "m7b5", 92),
        ([0, 3, 6, 9], "dim7", 91),
        ([0, 4, 8], "aug", 82),
        ([0, 3, 6], "dim", 84),
        ([0, 5, 7], "sus4", 83),
        ([0, 2, 7], "sus2", 82),
        ([0, 4, 7], "", 80),
        ([0, 3, 7], "m", 79),
        ([0, 7], "5", 45),
        ([0, 3], "m(no5)", 28),
        ([0, 4], "(no5)", 27)
    ];

    public static string? GetChordName(IReadOnlySet<int> activeNotes)
    {
        if (activeNotes.Count < 2)
        {
            return null;
        }

        HashSet<int> pitchClasses = [];

        foreach (int note in activeNotes)
        {
            pitchClasses.Add(((note % 12) + 12) % 12);
        }

        if (pitchClasses.Count < 2)
        {
            return null;
        }

        string? bestMatch = null;
        int bestScore = int.MinValue;

        foreach (int root in pitchClasses)
        {
            foreach ((int[] intervals, string suffix, int score) in ChordPatterns)
            {
                bool matches = true;

                foreach (int interval in intervals)
                {
                    if (!pitchClasses.Contains((root + interval) % 12))
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches)
                {
                    continue;
                }

                int extraTonePenalty = Math.Max(0, pitchClasses.Count - intervals.Length) * 6;
                int candidateScore = score - extraTonePenalty;
                if (candidateScore > bestScore)
                {
                    bestScore = candidateScore;
                    bestMatch = $"{MidiUtilities.PitchClassToNoteName(root)}{suffix}";
                }
            }
        }

        return bestMatch;
    }
}
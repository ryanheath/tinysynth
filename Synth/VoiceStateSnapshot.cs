namespace TinySynth.Synth;

internal readonly record struct VoiceStateSnapshot(
    HashSet<int> ActiveNotes,
    int ActiveVoiceCount,
    VoiceSlot? DisplaySlot,
    float AverageEnvelopeLevel,
    int KeyTrackMidiNote);

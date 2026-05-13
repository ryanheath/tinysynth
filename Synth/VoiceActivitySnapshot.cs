namespace TinySynth.Synth;

internal readonly record struct VoiceActivitySnapshot(
    HashSet<int> ActiveNotes,
    int ActiveVoiceCount,
    VoiceSlot? DisplaySlot);

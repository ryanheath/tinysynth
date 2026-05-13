namespace TinySynth.Synth.Modulation;

internal readonly record struct GlobalModulationInputs(
    float AverageEnvelopeLevel,
    int KeyTrackMidiNote);

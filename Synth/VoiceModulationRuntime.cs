namespace TinySynth.Synth;

internal sealed class VoiceModulationRuntime
{
    public float LfoPhase1 { get; set; }

    public float LfoPhase2 { get; set; }

    public float AverageEnvelopeLevel { get; set; }

    public void Reset()
    {
        LfoPhase1 = 0f;
        LfoPhase2 = 0f;
        AverageEnvelopeLevel = 0f;
    }
}

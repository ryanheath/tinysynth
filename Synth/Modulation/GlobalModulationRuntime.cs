using TinySynth.Synth.Dsp;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Modulation;

internal sealed class GlobalModulationRuntime(int sampleRate)
{
    private readonly int _sampleRate = sampleRate;

    public float LfoPhase1 { get; private set; }

    public float LfoPhase2 { get; private set; }

    public GlobalModulationState AdvanceAndBuild(SynthPatchSnapshot patchSnapshot, int frameCount, float averageEnvelopeLevel, int keyTrackMidiNote)
    {
        float deltaTime = frameCount / (float)_sampleRate;
        LfoPhase1 = PhaseMath.Advance(LfoPhase1, patchSnapshot.Lfo1.RateHz * deltaTime);
        LfoPhase2 = PhaseMath.Advance(LfoPhase2, patchSnapshot.Lfo2.RateHz * deltaTime);
        return GlobalModulationStateBuilder.Build(patchSnapshot, averageEnvelopeLevel, keyTrackMidiNote, LfoPhase1, LfoPhase2);
    }
}

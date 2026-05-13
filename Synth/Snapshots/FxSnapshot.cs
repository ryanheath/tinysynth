using TinySynth.Synth.Modulation;

namespace TinySynth.Synth.Snapshots;

internal readonly record struct FxSnapshot(
    ChorusType ChorusType,
    float ChorusMix,
    float ChorusRateHz,
    float ChorusDepth,
    float ChorusTremoloDepth,
    ReverbType ReverbType,
    float ReverbMix,
    float ReverbSize,
    float ReverbDamping,
    DelayType DelayType,
    float DelayMix,
    float DelayTimeSeconds,
    float DelayFeedback)
{
    public static FxSnapshot Create(SynthParameters source)
    {
        return new FxSnapshot(
            source.ChorusType,
            source.ChorusMix,
            source.ChorusRateHz,
            source.ChorusDepth,
            source.ChorusTremoloDepth,
            source.ReverbType,
            source.ReverbMix,
            source.ReverbSize,
            source.ReverbDamping,
            source.DelayType,
            source.DelayMix,
            source.DelayTimeSeconds,
            source.DelayFeedback);
    }

    public FxSnapshot WithModulation(GlobalModulationState modulationState)
    {
        return this with
        {
            ChorusMix = Math.Clamp(ChorusMix + modulationState.ChorusMix, 0f, 1f),
            DelayMix = Math.Clamp(DelayMix + modulationState.DelayMix, 0f, 1f),
            ReverbMix = Math.Clamp(ReverbMix + modulationState.ReverbMix, 0f, 1f)
        };
    }
}

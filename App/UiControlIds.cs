namespace TinySynth.App;

internal enum UiControlId
{
    OscillatorGain,
    OscillatorDetune,
    OscillatorGlide,
    OscillatorVibratoDepth,
    OscillatorVibratoRate,
    OscillatorAttack,
    OscillatorDecay,
    OscillatorSustain,
    OscillatorRelease,
    KeyboardMasterVolume,
    FilterCutoff,
    FilterResonance,
    FilterEnvelopeAmount,
    FilterLfoDepth,
    FilterLfoRate,
    FxChorusMix,
    FxChorusRate,
    FxChorusDepth,
    FxReverbMix,
    FxReverbSize,
    FxReverbDamping,
    FxDelayMix,
    FxDelayTime,
    FxDelayFeedback,
    OscillatorPulseWidth,
    OscillatorPwmRate,
    OscillatorPan,
    FxChorusTremoloDepth,
    ModulationLfo1Rate,
    ModulationLfo1Depth,
    ModulationLfo2Rate,
    ModulationLfo2Depth,
    ModulationRoute1Amount,
    ModulationRoute2Amount,
    ModulationRoute3Amount,
    ModulationRoute4Amount,
    ModulationRoute5Amount,
    ModulationRoute6Amount
}

internal static class UiControlIds
{
    public static UiControlId RouteAmount(int routeIndex)
    {
        return routeIndex switch
        {
            0 => UiControlId.ModulationRoute1Amount,
            1 => UiControlId.ModulationRoute2Amount,
            2 => UiControlId.ModulationRoute3Amount,
            3 => UiControlId.ModulationRoute4Amount,
            4 => UiControlId.ModulationRoute5Amount,
            5 => UiControlId.ModulationRoute6Amount,
            _ => throw new ArgumentOutOfRangeException(nameof(routeIndex))
        };
    }
}

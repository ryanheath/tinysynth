using TinySynth.Synth.Modulation;

namespace TinySynth.Synth.Snapshots;

internal sealed class SynthPatchSnapshot
{
    private readonly OscillatorSnapshot[] _oscillators;

    private SynthPatchSnapshot(
        OscillatorSnapshot[] oscillators,
        ModulationLfoSnapshot lfo1,
        ModulationLfoSnapshot lfo2,
        FilterType filterType,
        float filterCutoffHz,
        float filterResonance,
        float filterEnvelopeAmount,
        float filterLfoDepth,
        float filterLfoRateHz,
        FxSnapshot fx,
        ModulationMatrix modulationMatrix)
    {
        _oscillators = oscillators;
        Lfo1 = lfo1;
        Lfo2 = lfo2;
        FilterType = filterType;
        FilterCutoffHz = filterCutoffHz;
        FilterResonance = filterResonance;
        FilterEnvelopeAmount = filterEnvelopeAmount;
        FilterLfoDepth = filterLfoDepth;
        FilterLfoRateHz = filterLfoRateHz;
        Fx = fx;
        ModulationMatrix = modulationMatrix;
    }

    public IReadOnlyList<OscillatorSnapshot> Oscillators => _oscillators;

    public ModulationLfoSnapshot Lfo1 { get; }

    public ModulationLfoSnapshot Lfo2 { get; }

    public FilterType FilterType { get; }

    public float FilterCutoffHz { get; }

    public float FilterResonance { get; }

    public float FilterEnvelopeAmount { get; }

    public float FilterLfoDepth { get; }

    public float FilterLfoRateHz { get; }

    public FxSnapshot Fx { get; }

    public ModulationMatrix ModulationMatrix { get; }

    public OscillatorSnapshot GetOscillator(int index)
    {
        return _oscillators[Math.Clamp(index, 0, _oscillators.Length - 1)];
    }

    public static SynthPatchSnapshot Create(SynthParameters source)
    {
        OscillatorSnapshot[] oscillators = source.Oscillators
            .Select(OscillatorSnapshot.Create)
            .ToArray();

        return new SynthPatchSnapshot(
            oscillators,
            ModulationLfoSnapshot.Create(source.Lfo1),
            ModulationLfoSnapshot.Create(source.Lfo2),
            source.FilterType,
            source.FilterCutoffHz,
            source.FilterResonance,
            source.FilterEnvelopeAmount,
            source.FilterLfoDepth,
            source.FilterLfoRateHz,
            FxSnapshot.Create(source),
            ModulationMatrix.Create(source));
    }
}

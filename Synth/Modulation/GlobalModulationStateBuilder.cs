using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Modulation;

internal static class GlobalModulationStateBuilder
{
    public static GlobalModulationState Build(
        SynthPatchSnapshot patchSnapshot,
        float averageEnvelopeLevel,
        int keyTrackMidiNote,
        float lfoPhase1,
        float lfoPhase2)
    {
        float lfo1Value = ModulationLfoEvaluator.Evaluate(patchSnapshot.Lfo1, lfoPhase1);
        float lfo2Value = ModulationLfoEvaluator.Evaluate(patchSnapshot.Lfo2, lfoPhase2);
        float keyTrackValue = keyTrackMidiNote < 0
            ? 0f
            : Math.Clamp((keyTrackMidiNote - 60f) / 24f, -1f, 1f);

        ModulationSourceValues sourceValues = new(lfo1Value, lfo2Value, averageEnvelopeLevel, keyTrackValue);
        return patchSnapshot.ModulationMatrix.EvaluateGlobal(sourceValues);
    }

}

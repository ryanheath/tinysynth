using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Modulation;

internal static class ModulationLfoEvaluator
{
    public static float Evaluate(ModulationLfoSnapshot parameters, float phase)
    {
        float value = parameters.Shape switch
        {
            ModulationLfoShape.Sine => MathF.Sin(phase * MathF.Tau),
            ModulationLfoShape.Triangle => 1f - (4f * MathF.Abs(phase - 0.5f)),
            ModulationLfoShape.Saw => (2f * phase) - 1f,
            ModulationLfoShape.Square => phase < 0.5f ? 1f : -1f,
            _ => 0f
        };

        return value * Math.Clamp(parameters.Depth, 0f, 1f);
    }
}

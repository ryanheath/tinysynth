namespace TinySynth.Synth.Dsp;

internal static class PhaseMath
{
    public static float Advance(float phase, float increment)
    {
        phase += increment;
        phase -= MathF.Floor(phase);
        return phase;
    }
}

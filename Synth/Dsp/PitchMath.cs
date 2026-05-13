namespace TinySynth.Synth.Dsp;

internal static class PitchMath
{
    public static float ApplyDetune(float frequency, float cents)
    {
        return frequency * MathF.Pow(2f, cents / 1200f);
    }
}

namespace TinySynth.Synth.Dsp;

internal static class StereoMath
{
    public static (float Left, float Right) GetPanGains(float pan)
    {
        float normalizedPan = Math.Clamp(pan, -1f, 1f);
        float angle = (normalizedPan + 1f) * (MathF.PI * 0.25f);
        return (MathF.Cos(angle), MathF.Sin(angle));
    }
}

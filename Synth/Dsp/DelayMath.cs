namespace TinySynth.Synth.Dsp;

internal static class DelayMath
{
    public static float ReadInterpolated(float[] buffer, int writeIndex, float delaySamples)
    {
        double readPosition = writeIndex - delaySamples;
        readPosition %= buffer.Length;

        if (readPosition < 0d)
        {
            readPosition += buffer.Length;
        }

        int sampleIndexA = (int)readPosition;

        if (sampleIndexA >= buffer.Length)
        {
            sampleIndexA = 0;
        }

        int sampleIndexB = (sampleIndexA + 1) % buffer.Length;
        float fraction = (float)(readPosition - sampleIndexA);
        return buffer[sampleIndexA] + ((buffer[sampleIndexB] - buffer[sampleIndexA]) * fraction);
    }
}

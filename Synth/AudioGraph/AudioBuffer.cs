namespace TinySynth.Synth.AudioGraph;

internal sealed class AudioBuffer
{
    private float[] _samples = [];

    public int ChannelCount { get; } = 2;

    public int FrameCount { get; private set; }

    public float[] SampleArray => _samples;

    public Span<float> Samples => _samples;

    public ReadOnlySpan<float> ReadOnlySamples => _samples;

    public void EnsureFrameCount(int frameCount)
    {
        frameCount = Math.Max(0, frameCount);
        int sampleCount = frameCount * ChannelCount;

        if (_samples.Length != sampleCount)
        {
            _samples = new float[sampleCount];
        }

        FrameCount = frameCount;
    }

    public void Clear()
    {
        if (_samples.Length > 0)
        {
            Array.Clear(_samples);
        }
    }

    public void AddFrom(AudioBuffer source)
    {
        int sampleCount = Math.Min(_samples.Length, source._samples.Length);

        for (int i = 0; i < sampleCount; i++)
        {
            _samples[i] += source._samples[i];
        }
    }

    public void CopyTo(float[] destination)
    {
        Array.Copy(_samples, destination, Math.Min(_samples.Length, destination.Length));
    }
}

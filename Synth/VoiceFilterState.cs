namespace TinySynth.Synth;

internal sealed class VoiceFilterState
{
    private const int StereoChannelCount = 2;

    private readonly float[] _input1 = new float[StereoChannelCount];
    private readonly float[] _input2 = new float[StereoChannelCount];
    private readonly float[] _output1 = new float[StereoChannelCount];
    private readonly float[] _output2 = new float[StereoChannelCount];
    private float[] _cutoffBuffer = [];
    private float[] _resonanceBuffer = [];

    public float[] Input1 => _input1;
    public float[] Input2 => _input2;
    public float[] Output1 => _output1;
    public float[] Output2 => _output2;
    public float[] CutoffBuffer => _cutoffBuffer;
    public float[] ResonanceBuffer => _resonanceBuffer;

    public void EnsureBuffers(int frameCount)
    {
        if (_cutoffBuffer.Length != frameCount)
        {
            _cutoffBuffer = new float[frameCount];
            _resonanceBuffer = new float[frameCount];
        }
    }

    public void Reset()
    {
        Array.Clear(_input1);
        Array.Clear(_input2);
        Array.Clear(_output1);
        Array.Clear(_output2);
    }
}

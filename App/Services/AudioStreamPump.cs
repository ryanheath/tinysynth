using System.Runtime.InteropServices;
using Raylib_CSharp.Audio;
using TinySynth.Synth;

namespace TinySynth.App.Services;

internal sealed class AudioStreamPump
{
    private readonly AudioStream _audioStream;
    private readonly IntPtr _audioBufferPointer;
    private readonly float[] _audioBuffer;
    private readonly float[] _scopeBuffer;

    private int _scopeWriteIndex;

    public AudioStreamPump(
        AudioStream audioStream,
        IntPtr audioBufferPointer,
        float[] audioBuffer,
        int scopeBufferLength = 2048)
    {
        _audioStream = audioStream;
        _audioBufferPointer = audioBufferPointer;
        _audioBuffer = audioBuffer;
        _scopeBuffer = new float[scopeBufferLength];
    }

    public float[] ScopeBuffer => _scopeBuffer;

    public int ScopeWriteIndex => _scopeWriteIndex;

    public void Pump(SynthEngine synthEngine, SynthParameters parameters)
    {
        while (_audioStream.IsProcessed())
        {
            synthEngine.FillBuffer(_audioBuffer, _scopeBuffer, ref _scopeWriteIndex, parameters);
            Marshal.Copy(_audioBuffer, 0, _audioBufferPointer, _audioBuffer.Length);
            _audioStream.Update(_audioBufferPointer, _audioBuffer.Length / 2);
        }
    }
}

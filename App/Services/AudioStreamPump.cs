using System.Runtime.InteropServices;
using Raylib_CSharp.Audio;
using TinySynth.Synth;

namespace TinySynth.App.Services;

internal sealed class AudioStreamPump(
    AudioStream audioStream,
    IntPtr audioBufferPointer,
    float[] audioBuffer,
    int scopeBufferLength = 2048)
{
    private readonly AudioStream _audioStream = audioStream;
    private readonly IntPtr _audioBufferPointer = audioBufferPointer;
    private readonly float[] _audioBuffer = audioBuffer;
    private readonly float[] _scopeBuffer = new float[scopeBufferLength];

    private int _blockId;
    private int _scopeWriteIndex;

    public float[] ScopeBuffer => _scopeBuffer;

    public int ScopeWriteIndex => _scopeWriteIndex;

    public void Pump(SynthEngine synthEngine, SynthParameters parameters)
    {
        while (_audioStream.IsProcessed())
        {
            synthEngine.RenderBlock(_audioBuffer, _scopeBuffer, ref _scopeWriteIndex, _blockId++, parameters);
            Marshal.Copy(_audioBuffer, 0, _audioBufferPointer, _audioBuffer.Length);
            _audioStream.Update(_audioBufferPointer, _audioBuffer.Length / 2);
        }
    }
}

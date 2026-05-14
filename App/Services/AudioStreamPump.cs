using System.Diagnostics;
using System.Runtime.InteropServices;
using Raylib_CSharp.Audio;
using TinySynth.Synth;

namespace TinySynth.App.Services;

internal sealed class AudioStreamPump(
    AudioStream audioStream,
    IntPtr audioBufferPointer,
    float[] audioBuffer,
    int sampleRate = 44100,
    int audioChannelCount = 2,
    int scopeBufferLength = 2048)
{
    private const float DiscontinuityThreshold = 0.6f;
    private const float ClipThreshold = 0.999f;

    private readonly AudioStream _audioStream = audioStream;
    private readonly IntPtr _audioBufferPointer = audioBufferPointer;
    private readonly float[] _audioBuffer = audioBuffer;
    private readonly float[] _scopeBuffer = new float[scopeBufferLength];
    private readonly double _blockBudgetMilliseconds = ((audioBuffer.Length / audioChannelCount) / (double)sampleRate) * 1000.0;
    private readonly Stopwatch _renderStopwatch = new();

    private int _blockId;
    private int _scopeWriteIndex;
    private float _previousMixedSample;
    private double _averageRenderMilliseconds;
    private int _diagnosticSampleCount;
    private int _overrunCount;
    private int _discontinuityCount;
    private int _clipCount;
    private float _maxDiscontinuity;
    private float _peakLevel;
    private double _lastRenderMilliseconds;

    public float[] ScopeBuffer => _scopeBuffer;

    public int ScopeWriteIndex => _scopeWriteIndex;

    public AudioDiagnostics Diagnostics => new(
        _lastRenderMilliseconds,
        _averageRenderMilliseconds,
        _blockBudgetMilliseconds,
        _overrunCount,
        _discontinuityCount,
        _clipCount,
        _maxDiscontinuity,
        _peakLevel);

    public void Pump(SynthEngine synthEngine, SynthParameters parameters)
    {
        while (_audioStream.IsProcessed())
        {
            _renderStopwatch.Restart();
            synthEngine.RenderBlock(_audioBuffer, _scopeBuffer, ref _scopeWriteIndex, _blockId++, parameters);
            _renderStopwatch.Stop();
            UpdateDiagnostics();
            Marshal.Copy(_audioBuffer, 0, _audioBufferPointer, _audioBuffer.Length);
            _audioStream.Update(_audioBufferPointer, _audioBuffer.Length / 2);
        }
    }

    private void UpdateDiagnostics()
    {
        _lastRenderMilliseconds = _renderStopwatch.Elapsed.TotalMilliseconds;
        _diagnosticSampleCount++;
        _averageRenderMilliseconds += (_lastRenderMilliseconds - _averageRenderMilliseconds) / _diagnosticSampleCount;

        if (_lastRenderMilliseconds > _blockBudgetMilliseconds)
        {
            _overrunCount++;
        }

        float peakLevel = 0f;
        float maxDiscontinuity = _maxDiscontinuity;
        int discontinuityCount = _discontinuityCount;
        int clipCount = _clipCount;
        float previousMixedSample = _previousMixedSample;

        for (int i = 0; i < _audioBuffer.Length; i += 2)
        {
            float mixedSample = (_audioBuffer[i] + _audioBuffer[i + 1]) * 0.5f;
            float discontinuity = MathF.Abs(mixedSample - previousMixedSample);

            if (discontinuity > DiscontinuityThreshold)
            {
                discontinuityCount++;
            }

            if (discontinuity > maxDiscontinuity)
            {
                maxDiscontinuity = discontinuity;
            }

            float absLeft = MathF.Abs(_audioBuffer[i]);
            float absRight = MathF.Abs(_audioBuffer[i + 1]);
            peakLevel = MathF.Max(peakLevel, MathF.Max(absLeft, absRight));

            if (absLeft >= ClipThreshold || absRight >= ClipThreshold)
            {
                clipCount++;
            }

            previousMixedSample = mixedSample;
        }

        _previousMixedSample = previousMixedSample;
        _peakLevel = peakLevel;
        _maxDiscontinuity = maxDiscontinuity;
        _discontinuityCount = discontinuityCount;
        _clipCount = clipCount;
    }
}

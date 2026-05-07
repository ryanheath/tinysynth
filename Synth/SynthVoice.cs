namespace TinySynth.Synth;

internal sealed class SynthVoice
{
    private readonly int _sampleRate;
    private readonly float _masterGain;

    private float _envelopeLevel;
    private float _releaseStartLevel;
    private float _releaseElapsed;
    private float _oscillatorPhase;
    private float _currentFrequency;

    public SynthVoice(int sampleRate, float masterGain, int defaultMidiNote)
    {
        _sampleRate = sampleRate;
        _masterGain = masterGain;
        _currentFrequency = MidiUtilities.MidiToFrequency(defaultMidiNote);
    }

    public EnvelopeStage EnvelopeStage { get; private set; } = EnvelopeStage.Idle;

    public float CurrentFrequency => _currentFrequency;

    public int ActiveMidiNote { get; private set; } = -1;

    public void StartNote(int midiNote, SynthParameters parameters)
    {
        bool isAudible = EnvelopeStage != EnvelopeStage.Idle && _envelopeLevel > 0f;

        ActiveMidiNote = midiNote;
        _currentFrequency = MidiUtilities.MidiToFrequency(midiNote);

        if (!isAudible)
        {
            _oscillatorPhase = 0f;
            _envelopeLevel = 0f;
        }

        _releaseElapsed = 0f;
        _releaseStartLevel = 0f;
        EnvelopeStage = EnvelopeStage.Attack;
    }

    public void ReleaseNote()
    {
        if (ActiveMidiNote < 0 || EnvelopeStage == EnvelopeStage.Idle)
        {
            return;
        }

        _releaseStartLevel = _envelopeLevel;
        _releaseElapsed = 0f;
        EnvelopeStage = EnvelopeStage.Release;
    }

    public void FillBuffer(float[] audioBuffer, float[] scopeBuffer, ref int scopeWriteIndex, SynthParameters parameters)
    {
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            float sample = NextSample(parameters);
            audioBuffer[i] = sample;
            scopeBuffer[scopeWriteIndex] = sample;
            scopeWriteIndex = (scopeWriteIndex + 1) % scopeBuffer.Length;
        }
    }

    private float NextSample(SynthParameters parameters)
    {
        float deltaTime = 1f / _sampleRate;
        UpdateEnvelope(deltaTime, parameters);

        if (EnvelopeStage == EnvelopeStage.Idle || ActiveMidiNote < 0)
        {
            return 0f;
        }

        float sample = parameters.Waveform switch
        {
            Waveform.Sine => MathF.Sin(_oscillatorPhase * MathF.Tau),
            Waveform.Square => _oscillatorPhase < 0.5f ? 1f : -1f,
            Waveform.Saw => (2f * _oscillatorPhase) - 1f,
            Waveform.Triangle => 1f - (4f * MathF.Abs(_oscillatorPhase - 0.5f)),
            _ => 0f
        };

        _oscillatorPhase += _currentFrequency / _sampleRate;
        _oscillatorPhase -= MathF.Floor(_oscillatorPhase);

        return sample * _envelopeLevel * _masterGain;
    }

    private void UpdateEnvelope(float deltaTime, SynthParameters parameters)
    {
        switch (EnvelopeStage)
        {
            case EnvelopeStage.Idle:
                _envelopeLevel = 0f;
                break;

            case EnvelopeStage.Attack:
                _envelopeLevel += deltaTime / MathF.Max(parameters.AttackSeconds, 0.0001f);
                if (_envelopeLevel >= 1f)
                {
                    _envelopeLevel = 1f;
                    EnvelopeStage = EnvelopeStage.Decay;
                }
                break;

            case EnvelopeStage.Decay:
                if (parameters.DecaySeconds <= 0.01f)
                {
                    _envelopeLevel = parameters.SustainLevel;
                    EnvelopeStage = EnvelopeStage.Sustain;
                }
                else
                {
                    _envelopeLevel -= ((1f - parameters.SustainLevel) / parameters.DecaySeconds) * deltaTime;
                    if (_envelopeLevel <= parameters.SustainLevel)
                    {
                        _envelopeLevel = parameters.SustainLevel;
                        EnvelopeStage = EnvelopeStage.Sustain;
                    }
                }
                break;

            case EnvelopeStage.Sustain:
                _envelopeLevel = parameters.SustainLevel;
                break;

            case EnvelopeStage.Release:
                _releaseElapsed += deltaTime;
                if (parameters.ReleaseSeconds <= 0.01f || _releaseElapsed >= parameters.ReleaseSeconds)
                {
                    _envelopeLevel = 0f;
                    ActiveMidiNote = -1;
                    EnvelopeStage = EnvelopeStage.Idle;
                }
                else
                {
                    float releaseProgress = _releaseElapsed / parameters.ReleaseSeconds;
                    _envelopeLevel = _releaseStartLevel * (1f - releaseProgress);
                }
                break;
        }
    }
}

namespace TinySynth.Synth;

internal static class VoiceRuntimeInspector
{
    public static float GetKeyTrackValue(int activeMidiNote)
    {
        if (activeMidiNote < 0)
        {
            return 0f;
        }

        return Math.Clamp((activeMidiNote - 60f) / 24f, -1f, 1f);
    }

    public static bool HasAudibleOscillator(SynthVoice voice)
    {
        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            if (voice.GetOscillatorState(i).EnvelopeLevel > 0f)
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasEnabledOscillator(SynthVoice voice, SynthParameters parameters)
    {
        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            if (parameters.GetOscillator(i).Enabled)
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasOnlyOneShotOscillators(SynthVoice voice, SynthParameters parameters)
    {
        bool hasEnabledOscillator = false;

        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            OscillatorParameters oscillatorParameters = parameters.GetOscillator(i);
            if (!oscillatorParameters.Enabled)
            {
                continue;
            }

            hasEnabledOscillator = true;

            if (oscillatorParameters.EnvelopeMode != EnvelopeMode.OneShot)
            {
                return false;
            }
        }

        return hasEnabledOscillator;
    }

    public static bool AreAllOscillatorsAtOrBelow(SynthVoice voice, float value)
    {
        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            if (voice.GetOscillatorState(i).EnvelopeLevel > value)
            {
                return false;
            }
        }

        return true;
    }
}

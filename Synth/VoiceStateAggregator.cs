using TinySynth.Synth.Modulation;

namespace TinySynth.Synth;

internal static class VoiceStateAggregator
{
    public static void CaptureActivity(IReadOnlyList<VoiceSlot> slots, HashSet<int> activeNotes, out int activeVoiceCount, out VoiceSlot? displaySlot)
    {
        activeNotes.Clear();
        activeVoiceCount = 0;
        displaySlot = null;

        foreach (VoiceSlot slot in slots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                continue;
            }

            activeNotes.Add(slot.Voice.ActiveMidiNote);
            activeVoiceCount++;

            if (displaySlot is null || slot.LastStartOrder > displaySlot.LastStartOrder)
            {
                displaySlot = slot;
            }
        }
    }

    public static GlobalModulationInputs CaptureGlobalModulationInputs(IReadOnlyList<VoiceSlot> slots)
    {
        float averageEnvelopeLevel = 0f;
        int keyTrackMidiNote = -1;
        int activeVoiceCount = 0;

        foreach (VoiceSlot slot in slots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                continue;
            }

            activeVoiceCount++;
            keyTrackMidiNote = Math.Max(keyTrackMidiNote, slot.Voice.ActiveMidiNote);
            averageEnvelopeLevel += slot.Runtime.ModulationRuntime.AverageEnvelopeLevel;
        }

        if (activeVoiceCount > 0)
        {
            averageEnvelopeLevel /= activeVoiceCount;
        }

        return new GlobalModulationInputs(averageEnvelopeLevel, keyTrackMidiNote);
    }
}

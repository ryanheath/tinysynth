using TinySynth.Synth.Nodes;

namespace TinySynth.Synth;

internal static class VoiceStateAggregator
{
    public static VoiceStateSnapshot Capture(IReadOnlyList<VoiceSlot> slots, IReadOnlyDictionary<SynthVoice, VoiceOscNode> oscNodes)
    {
        HashSet<int> activeNotes = [];
        int activeVoiceCount = 0;
        float averageEnvelopeLevel = 0f;
        int keyTrackMidiNote = -1;
        VoiceSlot? displaySlot = null;

        foreach (VoiceSlot slot in slots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                slot.IsHeld = false;
                continue;
            }

            activeNotes.Add(slot.Voice.ActiveMidiNote);
            activeVoiceCount++;
            keyTrackMidiNote = Math.Max(keyTrackMidiNote, slot.Voice.ActiveMidiNote);

            if (oscNodes.TryGetValue(slot.Voice, out VoiceOscNode? oscNode))
            {
                averageEnvelopeLevel += oscNode.AverageEnvelopeLevel;
            }

            if (displaySlot is null || slot.LastStartOrder > displaySlot.LastStartOrder)
            {
                displaySlot = slot;
            }
        }

        if (activeVoiceCount > 0)
        {
            averageEnvelopeLevel /= activeVoiceCount;
        }

        return new VoiceStateSnapshot(activeNotes, activeVoiceCount, displaySlot, averageEnvelopeLevel, keyTrackMidiNote);
    }
}

using TinySynth.Synth.Modulation;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.AudioGraph;

internal readonly record struct AudioRenderContext(
    int BlockId,
    int FrameCount,
    int SampleRate,
    SynthPatchSnapshot PatchSnapshot,
    GlobalModulationState GlobalModulationState);

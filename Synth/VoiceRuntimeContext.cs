using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth;

internal sealed class VoiceRuntimeContext
{
    public VoiceFilterState FilterState { get; } = new();

    public VoiceModulationRuntime ModulationRuntime { get; } = new();

    public OscillatorSnapshot[] OscillatorSnapshots { get; } = new OscillatorSnapshot[SynthParameters.OscillatorCount];

    public SynthVoice.OscillatorState[] OscillatorStates { get; } = new SynthVoice.OscillatorState[SynthParameters.OscillatorCount];
}

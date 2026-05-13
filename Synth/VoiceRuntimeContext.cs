namespace TinySynth.Synth;

internal sealed class VoiceRuntimeContext
{
    public VoiceFilterState FilterState { get; } = new();

    public VoiceModulationRuntime ModulationRuntime { get; } = new();
}

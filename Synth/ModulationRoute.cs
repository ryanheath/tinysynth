namespace TinySynth.Synth;

internal sealed class ModulationRoute
{
    public ModulationSource Source { get; set; } = ModulationSource.None;

    public ModulationDestination Destination { get; set; } = ModulationDestination.None;

    public float Amount { get; set; }

    public int OscillatorIndex { get; set; } = -1;
}

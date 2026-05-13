namespace TinySynth.Synth.Modulation;

internal sealed class ModulationMatrix(IEnumerable<ModulationRoute> routes)
{
    private readonly RouteEntry[] _routes = routes
            .Select(route => new RouteEntry(
                route.Source,
                route.Destination,
                route.Amount,
                route.OscillatorIndex))
            .ToArray();

    public IReadOnlyList<RouteEntry> Routes => _routes;

    public static ModulationMatrix Create(SynthParameters parameters)
    {
        return new ModulationMatrix(parameters.ModulationRoutes);
    }

    public VoiceModulationState EvaluateVoice(in ModulationSourceValues sourceValues, int oscillatorIndex)
    {
        return ModulationEvaluator.EvaluateVoice(_routes, sourceValues, oscillatorIndex);
    }

    public GlobalModulationState EvaluateGlobal(in ModulationSourceValues sourceValues)
    {
        return ModulationEvaluator.EvaluateGlobal(_routes, sourceValues);
    }

    internal readonly record struct RouteEntry(
        ModulationSource Source,
        ModulationDestination Destination,
        float Amount,
        int OscillatorIndex);
}

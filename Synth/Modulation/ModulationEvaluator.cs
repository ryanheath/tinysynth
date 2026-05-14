namespace TinySynth.Synth.Modulation;

internal static class ModulationEvaluator
{
    public static float EvaluateAmount(
        IReadOnlyList<ModulationMatrix.RouteEntry> routes,
        ModulationDestination destination,
        in ModulationSourceValues sourceValues,
        int oscillatorIndex)
    {
        float total = 0f;

        foreach (ModulationMatrix.RouteEntry route in routes)
        {
            if (route.Destination != destination
                || route.Source == ModulationSource.None
                || route.Destination == ModulationDestination.None
                || (route.OscillatorIndex >= 0 && route.OscillatorIndex != oscillatorIndex))
            {
                continue;
            }

            total += sourceValues.GetValue(route.Source) * route.Amount;
        }

        return total;
    }

    public static VoiceModulationState EvaluateVoice(
        IReadOnlyList<ModulationMatrix.RouteEntry> routes,
        in ModulationSourceValues sourceValues,
        int oscillatorIndex)
    {
        return new VoiceModulationState(
            Pitch: EvaluateAmount(routes, ModulationDestination.Pitch, sourceValues, oscillatorIndex),
            FilterCutoff: EvaluateAmount(routes, ModulationDestination.FilterCutoff, sourceValues, oscillatorIndex: -1),
            FilterResonance: EvaluateAmount(routes, ModulationDestination.FilterResonance, sourceValues, oscillatorIndex: -1),
            Gain: EvaluateAmount(routes, ModulationDestination.Gain, sourceValues, oscillatorIndex),
            Pan: EvaluateAmount(routes, ModulationDestination.Pan, sourceValues, oscillatorIndex),
            PulseWidth: EvaluateAmount(routes, ModulationDestination.PulseWidth, sourceValues, oscillatorIndex),
            Lfo1Rate: EvaluateAmount(routes, ModulationDestination.Lfo1Rate, sourceValues, oscillatorIndex),
            Lfo2Rate: EvaluateAmount(routes, ModulationDestination.Lfo2Rate, sourceValues, oscillatorIndex));
    }

    public static VoiceModulationState EvaluateOscillator(
        IReadOnlyList<ModulationMatrix.RouteEntry> routes,
        in ModulationSourceValues sourceValues,
        int oscillatorIndex)
    {
        return new VoiceModulationState(
            Pitch: EvaluateAmount(routes, ModulationDestination.Pitch, sourceValues, oscillatorIndex),
            FilterCutoff: 0f,
            FilterResonance: 0f,
            Gain: EvaluateAmount(routes, ModulationDestination.Gain, sourceValues, oscillatorIndex),
            Pan: EvaluateAmount(routes, ModulationDestination.Pan, sourceValues, oscillatorIndex),
            PulseWidth: EvaluateAmount(routes, ModulationDestination.PulseWidth, sourceValues, oscillatorIndex),
            Lfo1Rate: 0f,
            Lfo2Rate: 0f);
    }

    public static GlobalModulationState EvaluateGlobal(
        IReadOnlyList<ModulationMatrix.RouteEntry> routes,
        in ModulationSourceValues sourceValues)
    {
        return new GlobalModulationState(
            ChorusMix: EvaluateAmount(routes, ModulationDestination.ChorusMix, sourceValues, oscillatorIndex: -1),
            DelayMix: EvaluateAmount(routes, ModulationDestination.DelayMix, sourceValues, oscillatorIndex: -1),
            ReverbMix: EvaluateAmount(routes, ModulationDestination.ReverbMix, sourceValues, oscillatorIndex: -1));
    }
}

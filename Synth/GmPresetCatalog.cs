namespace TinySynth.Synth;

internal sealed record GmPreset(
    GmInstrumentFamily Family,
    string Name,
    string Description,
    Action<SynthParameters> Apply);

internal static class GmPresetCatalog
{
    private static readonly IReadOnlyList<GmPreset> _presets =
    [
        CreatePreset(GmInstrumentFamily.Piano, "Grand Piano", "Bright layered attack with short room ambience.", p => ConfigurePiano(p, bright: true, electric: false)),
        CreatePreset(GmInstrumentFamily.Piano, "Honky-Tonk", "Detuned midrange piano with a lively room.", p => ConfigurePiano(p, bright: false, electric: true)),
        CreatePreset(GmInstrumentFamily.Piano, "Electric Piano 1", "Bright tine electric piano with chorus shimmer.", p => ConfigureElectricPiano(p, bright: true)),
        CreatePreset(GmInstrumentFamily.Piano, "Electric Piano 2", "Softer electric piano with darker body and wider chorus.", p => ConfigureElectricPiano(p, bright: false)),
        CreatePreset(GmInstrumentFamily.ChromaticPercussion, "Celesta", "Bell-like sine shimmer with soft reverb.", p => ConfigureChromatic(p, metallic: false, vibraphone: false)),
        CreatePreset(GmInstrumentFamily.ChromaticPercussion, "Vibraphone", "Rounded struck tone with chorus and tremolo.", p => ConfigureChromatic(p, metallic: true, vibraphone: true)),
        CreatePreset(GmInstrumentFamily.ChromaticPercussion, "Music Box", "Sparkling music box pluck with glassy bloom.", p =>
        {
            ConfigureChromatic(p, metallic: true, vibraphone: false);
            p.GetOscillator(0).DecaySeconds = 0.44f;
            p.GetOscillator(0).ReleaseSeconds = 0.72f;
            p.GetOscillator(1).Gain = 0.30f;
            p.FilterCutoffHz = 6000f;
            p.ReverbMix = 0.20f;
            p.ReverbSize = 0.48f;
        }),
        CreatePreset(GmInstrumentFamily.ChromaticPercussion, "Tubular Bells", "Large metallic strike with long cathedral tail.", p =>
        {
            ConfigureChromatic(p, metallic: true, vibraphone: false);
            p.GetOscillator(0).DecaySeconds = 1.10f;
            p.GetOscillator(0).ReleaseSeconds = 1.80f;
            p.GetOscillator(1).DetuneCents = 19f;
            p.FilterCutoffHz = 4600f;
            p.ReverbMix = 0.34f;
            p.ReverbSize = 0.88f;
        }),
        CreatePreset(GmInstrumentFamily.Organ, "Drawbar Organ", "Full-bodied organ with chorus and long sustain.", p => ConfigureOrgan(p, church: false)),
        CreatePreset(GmInstrumentFamily.Organ, "Church Organ", "Wide sustained organ with hall bloom.", p => ConfigureOrgan(p, church: true)),
        CreatePreset(GmInstrumentFamily.Organ, "Rock Organ", "Brighter stage organ with tighter room and bite.", p =>
        {
            ConfigureOrgan(p, church: false);
            p.GetOscillator(0).Gain = 0.42f;
            p.GetOscillator(1).Gain = 0.32f;
            p.FilterCutoffHz = 4200f;
            p.ChorusMix = 0.12f;
            p.ReverbMix = 0.08f;
        }),
        CreatePreset(GmInstrumentFamily.Organ, "Reed Organ", "Mid-focused organ with restrained space and motion.", p =>
        {
            ConfigureOrgan(p, church: true);
            p.FilterCutoffHz = 2500f;
            p.FilterResonance = 0.18f;
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.10f;
            p.ReverbMix = 0.12f;
        }),
        CreatePreset(GmInstrumentFamily.Guitar, "Nylon Guitar", "Warm plucked guitar with soft attack and room.", p => ConfigureGuitar(p, electric: false, muted: false)),
        CreatePreset(GmInstrumentFamily.Guitar, "Muted Guitar", "Short damped pluck with filtered attack.", p => ConfigureGuitar(p, electric: true, muted: true)),
        CreatePreset(GmInstrumentFamily.Guitar, "Clean Guitar", "Bright electric guitar with a short slap echo.", p =>
        {
            ConfigureGuitar(p, electric: true, muted: false);
            p.FilterCutoffHz = 5000f;
            p.FilterResonance = 0.18f;
            p.DelayMix = 0.06f;
            p.ReverbMix = 0.10f;
        }),
        CreatePreset(GmInstrumentFamily.Guitar, "Overdrive Guitar", "Edgy electric guitar with tighter mids and pick noise.", p =>
        {
            ConfigureGuitar(p, electric: true, muted: false);
            p.GetOscillator(0).Waveform = Waveform.Square;
            p.GetOscillator(2).Gain = 0.08f;
            p.FilterCutoffHz = 3000f;
            p.FilterResonance = 0.34f;
            p.ReverbMix = 0.06f;
        }),
        CreatePreset(GmInstrumentFamily.Bass, "Finger Bass", "Rounded electric bass with subtle growl.", p => ConfigureBass(p, synth: false)),
        CreatePreset(GmInstrumentFamily.Bass, "Synth Bass", "Punchy low-end synth bass with filter snap.", p => ConfigureBass(p, synth: true)),
        CreatePreset(GmInstrumentFamily.Bass, "Pick Bass", "Sharper bass attack with extra upper-mid definition.", p =>
        {
            ConfigureBass(p, synth: false);
            p.GetOscillator(0).Waveform = Waveform.Saw;
            p.FilterCutoffHz = 2800f;
            p.FilterResonance = 0.20f;
            p.FilterEnvelopeAmount = 0.22f;
        }),
        CreatePreset(GmInstrumentFamily.Bass, "Fretless Bass", "Smooth singing bass with gentle vibrato.", p =>
        {
            ConfigureBass(p, synth: false);
            p.GetOscillator(0).Waveform = Waveform.Triangle;
            p.GetOscillator(0).VibratoDepthCents = 4f;
            p.GetOscillator(0).VibratoRateHz = 4.4f;
            p.FilterCutoffHz = 1800f;
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.06f;
            p.ChorusDepth = 0.12f;
        }),
        CreatePreset(GmInstrumentFamily.Strings, "Violin Section", "Focused bowed strings with gentle chorus.", p => ConfigureStrings(p, solo: true)),
        CreatePreset(GmInstrumentFamily.Strings, "Pizzicato", "Short plucked strings with woody decay.", p => ConfigureStrings(p, solo: false)),
        CreatePreset(GmInstrumentFamily.Strings, "Cello Section", "Darker bowed strings with deeper body and hall.", p =>
        {
            ConfigureStrings(p, solo: true);
            p.GetOscillator(0).Gain = 0.38f;
            p.GetOscillator(1).Gain = 0.32f;
            p.FilterCutoffHz = 3400f;
            p.ChorusMix = 0.10f;
            p.ReverbMix = 0.20f;
        }),
        CreatePreset(GmInstrumentFamily.Strings, "Tremolo Strings", "Animated string layer with extra motion and spread.", p =>
        {
            ConfigureStrings(p, solo: false);
            p.GetOscillator(0).Gain = 0.34f;
            p.GetOscillator(0).DecaySeconds = 0.20f;
            p.GetOscillator(0).SustainLevel = 0.68f;
            p.GetOscillator(0).ReleaseSeconds = 0.34f;
            p.GetOscillator(1).Gain = 0.24f;
            p.GetOscillator(1).DecaySeconds = 0.18f;
            p.GetOscillator(1).SustainLevel = 0.62f;
            p.GetOscillator(1).ReleaseSeconds = 0.30f;
            ConfigureOsc(p, 2, Waveform.Triangle, 0.10f, 0f, 4f, 0.04f, 0.16f, 0.56f, 0.28f, 0.18f, pan: 0.06f);
            p.FilterCutoffHz = 3400f;
            p.ChorusMix = 0.26f;
            p.ChorusDepth = 0.34f;
            p.ChorusTremoloDepth = 0.24f;
            p.ReverbMix = 0.16f;
        }),
        CreatePreset(GmInstrumentFamily.Ensemble, "Slow Strings", "Layered ensemble pad with hall reverb.", p => ConfigureEnsemble(p, choir: false)),
        CreatePreset(GmInstrumentFamily.Ensemble, "Choir Aahs", "Airy vocal pad with shimmer and softness.", p => ConfigureEnsemble(p, choir: true)),
        CreatePreset(GmInstrumentFamily.Ensemble, "Synth Strings", "Wide synthetic ensemble with glossy chorused motion.", p =>
        {
            ConfigureEnsemble(p, choir: false);
            p.GetOscillator(0).Waveform = Waveform.SuperSaw;
            p.GetOscillator(0).Gain = 0.30f;
            p.GetOscillator(1).Waveform = Waveform.SuperSaw;
            p.GetOscillator(1).Gain = 0.18f;
            p.GetOscillator(2).Gain = 0.12f;
            p.GetOscillator(3).Gain = 0.08f;
            p.FilterCutoffHz = 4200f;
            p.ChorusMix = 0.36f;
            p.ReverbMix = 0.16f;
        }),
        CreatePreset(GmInstrumentFamily.Ensemble, "Voice Oohs", "Rounded vocal ensemble with soft shimmer tail.", p =>
        {
            ConfigureEnsemble(p, choir: true);
            p.GetOscillator(0).Gain = 0.42f;
            p.GetOscillator(1).Waveform = Waveform.Triangle;
            p.GetOscillator(1).Gain = 0.20f;
            p.GetOscillator(2).Gain = 0.22f;
            p.GetOscillator(3).Waveform = Waveform.Sine;
            p.GetOscillator(3).Gain = 0.06f;
            p.FilterCutoffHz = 1700f;
            p.ChorusMix = 0.10f;
            p.ChorusDepth = 0.14f;
            p.ReverbMix = 0.28f;
            p.ReverbType = ReverbType.Shimmer;
        }),
        CreatePreset(GmInstrumentFamily.Brass, "Trumpet", "Brassy lead with a quick bite and body.", p => ConfigureBrass(p, mellow: false)),
        CreatePreset(GmInstrumentFamily.Brass, "French Horn", "Rounded orchestral brass with deeper resonance.", p => ConfigureBrass(p, mellow: true)),
        CreatePreset(GmInstrumentFamily.Brass, "Trombone Section", "Broad brass layer with softer attack and weight.", p =>
        {
            ConfigureBrass(p, mellow: true);
            p.GetOscillator(0).Gain = 0.54f;
            p.GetOscillator(1).Waveform = Waveform.Triangle;
            p.GetOscillator(1).Gain = 0.22f;
            p.FilterCutoffHz = 1800f;
            p.FilterResonance = 0.10f;
            p.ReverbMix = 0.12f;
        }),
        CreatePreset(GmInstrumentFamily.Brass, "Brass Section", "Stacked section brass with wider bite and room.", p =>
        {
            ConfigureBrass(p, mellow: false);
            p.GetOscillator(0).Gain = 0.64f;
            p.GetOscillator(1).Gain = 0.28f;
            p.GetOscillator(1).DetuneCents = -9f;
            p.GetOscillator(2).Gain = 0.06f;
            p.FilterCutoffHz = 3600f;
            p.FilterResonance = 0.28f;
            p.ReverbMix = 0.08f;
        }),
        CreatePreset(GmInstrumentFamily.Reed, "Alto Sax", "Breathy reed tone with gentle vibrato.", p => ConfigureReed(p, clarinet: false)),
        CreatePreset(GmInstrumentFamily.Reed, "Clarinet", "Woody focused reed with softer highs.", p => ConfigureReed(p, clarinet: true)),
        CreatePreset(GmInstrumentFamily.Reed, "Tenor Sax", "Lower sax lead with reed breath and room.", p =>
        {
            ConfigureReed(p, clarinet: false);
            p.GetOscillator(0).Gain = 0.54f;
            p.GetOscillator(0).DetuneCents = -2f;
            p.GetOscillator(1).Gain = 0.26f;
            p.GetOscillator(1).DetuneCents = -8f;
            p.GetOscillator(2).Gain = 0.07f;
            p.FilterCutoffHz = 2200f;
            p.FilterResonance = 0.18f;
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.05f;
            p.ChorusDepth = 0.10f;
            p.ReverbType = ReverbType.Room;
            p.ReverbMix = 0.10f;
        }),
        CreatePreset(GmInstrumentFamily.Reed, "Bassoon", "Dark reed voice with focused low-mid resonance.", p =>
        {
            ConfigureReed(p, clarinet: true);
            p.GetOscillator(0).Waveform = Waveform.Triangle;
            p.GetOscillator(0).Gain = 0.52f;
            p.GetOscillator(1).Waveform = Waveform.Sine;
            p.GetOscillator(1).Gain = 0.14f;
            p.GetOscillator(1).DetuneCents = -5f;
            p.GetOscillator(2).Gain = 0.005f;
            p.FilterCutoffHz = 1350f;
            p.FilterResonance = 0.28f;
            p.ReverbType = ReverbType.Room;
            p.ReverbMix = 0.06f;
        }),
        CreatePreset(GmInstrumentFamily.Pipe, "Flute", "Pure airy flute with subtle motion.", p => ConfigurePipe(p, panFlute: false)),
        CreatePreset(GmInstrumentFamily.Pipe, "Pan Flute", "Breathy pan flute with delayed tail.", p => ConfigurePipe(p, panFlute: true)),
        CreatePreset(GmInstrumentFamily.Pipe, "Recorder", "Focused recorder tone with light breath noise.", p =>
        {
            ConfigurePipe(p, panFlute: false);
            p.GetOscillator(0).Waveform = Waveform.Triangle;
            p.GetOscillator(0).Gain = 0.70f;
            p.GetOscillator(1).Gain = 0.22f;
            p.GetOscillator(2).Gain = 0.04f;
            p.FilterCutoffHz = 3800f;
            p.FilterResonance = 0.22f;
            p.ReverbMix = 0.06f;
        }),
        CreatePreset(GmInstrumentFamily.Pipe, "Ocarina", "Rounded clay flute with warm mids and bloom.", p =>
        {
            ConfigurePipe(p, panFlute: false);
            p.GetOscillator(0).Gain = 0.76f;
            p.GetOscillator(1).Waveform = Waveform.Sine;
            p.GetOscillator(1).Gain = 0.12f;
            p.GetOscillator(2).Gain = 0.005f;
            p.FilterCutoffHz = 2100f;
            p.FilterResonance = 0.18f;
            p.ReverbType = ReverbType.Hall;
            p.ReverbMix = 0.12f;
        }),
        CreatePreset(GmInstrumentFamily.SynthLead, "Lead Saw", "Classic bright synth lead with slight delay.", p => ConfigureLead(p, square: false)),
        CreatePreset(GmInstrumentFamily.SynthLead, "Pulse Lead", "Animated PWM lead with chorus width.", p => ConfigureLead(p, square: true)),
        CreatePreset(GmInstrumentFamily.SynthLead, "Square Lead", "Solid square lead with tighter filter and bite.", p =>
        {
            ConfigureLead(p, square: true);
            p.GetOscillator(0).Waveform = Waveform.Square;
            p.FilterCutoffHz = 3200f;
            p.DelayMix = 0.10f;
            p.ChorusMix = 0.06f;
        }),
        CreatePreset(GmInstrumentFamily.SynthLead, "Chiff Lead", "Bright attack lead with a breathy edge.", p =>
        {
            ConfigureLead(p, square: false);
            ConfigureOsc(p, 2, Waveform.Noise, 0.04f, 0f, 0f, 0.001f, 0.03f, 0.00f, 0.00f, 0.03f, envelopeMode: EnvelopeMode.OneShot);
            p.FilterCutoffHz = 6200f;
            p.DelayMix = 0.08f;
        }),
        CreatePreset(GmInstrumentFamily.SynthPad, "Warm Pad", "Smooth analog pad with chorus and hall.", p => ConfigurePad(p, airy: false)),
        CreatePreset(GmInstrumentFamily.SynthPad, "Halo Pad", "Airy pad with shimmer and long release.", p => ConfigurePad(p, airy: true)),
        CreatePreset(GmInstrumentFamily.SynthPad, "Polysynth", "Glossy poly pad with tighter body and movement.", p =>
        {
            ConfigurePad(p, airy: false);
            p.FilterCutoffHz = 3000f;
            p.ChorusMix = 0.16f;
            p.ReverbMix = 0.18f;
        }),
        CreatePreset(GmInstrumentFamily.SynthPad, "Sweep Pad", "Slow evolving pad with broader sweep and shimmer.", p =>
        {
            ConfigurePad(p, airy: true);
            p.FilterType = FilterType.BandPass;
            p.FilterCutoffHz = 2400f;
            ConfigureRoute(p, 2, ModulationSource.Lfo1, ModulationDestination.FilterCutoff, 0.24f);
            p.ReverbMix = 0.34f;
        }),
        CreatePreset(GmInstrumentFamily.SynthEffects, "Atmosphere", "Moving texture with noise wash and delay.", p => ConfigureSynthFx(p, beam: false)),
        CreatePreset(GmInstrumentFamily.SynthEffects, "Sci-Fi Beam", "Sweeping resonant effect with pinging tail.", p => ConfigureSynthFx(p, beam: true)),
        CreatePreset(GmInstrumentFamily.SynthEffects, "Crystal", "Glassy effect with metallic sparkle and shimmer.", p =>
        {
            ConfigureSynthFx(p, beam: false);
            p.GetOscillator(0).Waveform = Waveform.Metallic;
            p.FilterCutoffHz = 1800f;
            p.DelayMix = 0.20f;
            p.ReverbType = ReverbType.Shimmer;
        }),
        CreatePreset(GmInstrumentFamily.SynthEffects, "Goblins", "Quirky resonant effect with animated stereo motion.", p =>
        {
            ConfigureSynthFx(p, beam: true);
            p.GetOscillator(1).DetuneCents = 18f;
            p.FilterCutoffHz = 1200f;
            p.DelayTimeSeconds = 0.18f;
            p.DelayFeedback = 0.30f;
        }),
        CreatePreset(GmInstrumentFamily.Ethnic, "Sitar", "Plucked string with buzzing brightness and delay.", p => ConfigureEthnic(p, plucked: true)),
        CreatePreset(GmInstrumentFamily.Ethnic, "Shamisen", "Sharp percussive string with short body.", p => ConfigureEthnic(p, plucked: false)),
        CreatePreset(GmInstrumentFamily.Ethnic, "Koto", "Clean plucked string with bright transient and short echo.", p =>
        {
            ConfigureEthnic(p, plucked: true);
            p.GetOscillator(0).DecaySeconds = 0.10f;
            p.GetOscillator(0).ReleaseSeconds = 0.14f;
            p.FilterCutoffHz = 4200f;
            p.DelayMix = 0.08f;
        }),
        CreatePreset(GmInstrumentFamily.Ethnic, "Banjo", "Snappy picked string with woody attack and bite.", p =>
        {
            ConfigureEthnic(p, plucked: false);
            p.GetOscillator(2).Gain = 0.10f;
            p.FilterCutoffHz = 3000f;
            p.FilterResonance = 0.24f;
            p.DelayMix = 0.04f;
        }),
        CreatePreset(GmInstrumentFamily.Percussive, "Timpani", "Deep mallet hit with resonant decay.", p => ConfigurePercussive(p, tuned: true)),
        CreatePreset(GmInstrumentFamily.Percussive, "Taiko", "Boomy drum hit with filtered noise thump.", p => ConfigurePercussive(p, tuned: false)),
        CreatePreset(GmInstrumentFamily.Percussive, "Melodic Tom", "Focused tom strike with a shorter tuned ring.", p =>
        {
            ConfigurePercussive(p, tuned: true);
            p.GetOscillator(0).DecaySeconds = 0.34f;
            p.GetOscillator(0).ReleaseSeconds = 0.42f;
            p.FilterCutoffHz = 2400f;
            p.ReverbMix = 0.08f;
        }),
        CreatePreset(GmInstrumentFamily.Percussive, "Synth Drum", "Electronic drum hit with deeper low-end punch.", p =>
        {
            ConfigurePercussive(p, tuned: false);
            p.GetOscillator(0).Waveform = Waveform.Saw;
            p.GetOscillator(2).Gain = 0.24f;
            p.FilterCutoffHz = 1400f;
            p.ReverbMix = 0.12f;
        }),
        CreatePreset(GmInstrumentFamily.SoundEffects, "Rain", "Filtered noise with spacious ambience.", p => ConfigureSoundFx(p, burst: false)),
        CreatePreset(GmInstrumentFamily.SoundEffects, "Helicopter", "Chopped noise wash with PWM rotor feel.", p => ConfigureSoundFx(p, burst: true)),
        CreatePreset(GmInstrumentFamily.SoundEffects, "Seashore", "Rolling filtered noise with slow stereo drift.", p =>
        {
            ConfigureSoundFx(p, burst: false);
            p.FilterCutoffHz = 2400f;
            ConfigureLfo(p.Lfo1, ModulationLfoShape.Sine, 0.12f, 1f);
            p.ReverbMix = 0.28f;
        }),
        CreatePreset(GmInstrumentFamily.SoundEffects, "Explosion", "Heavy burst effect with low boom and quick echo.", p =>
        {
            ConfigureSoundFx(p, burst: true);
            p.FilterType = FilterType.LowPass;
            p.FilterCutoffHz = 700f;
            p.DelayMix = 0.10f;
            p.ReverbMix = 0.12f;
        })
    ];

    public static IReadOnlyList<GmInstrumentFamily> Families { get; } = Enum.GetValues<GmInstrumentFamily>();

    private static readonly IReadOnlyDictionary<GmInstrumentFamily, IReadOnlyList<GmPreset>> _presetsByFamily =
        Families.ToDictionary(
            static family => family,
            static family => (IReadOnlyList<GmPreset>)_presets.Where(p => p.Family == family).ToArray());

    public static IReadOnlyList<GmPreset> Presets => _presets;

    public static IReadOnlyList<GmPreset> GetPresets(GmInstrumentFamily family)
    {
        return _presetsByFamily[family];
    }

    public static void ApplyPreset(GmPreset preset, SynthParameters parameters)
    {
        parameters.ResetToDefaults();
        preset.Apply(parameters);
    }

    private static GmPreset CreatePreset(GmInstrumentFamily family, string name, string description, Action<SynthParameters> apply)
    {
        return new GmPreset(family, name, description, apply);
    }

    private static void ConfigurePiano(SynthParameters p, bool bright, bool electric)
    {
        ConfigureOsc(p, 0, Waveform.Sine, bright ? 0.65f : 0.55f, 0f, 0.002f, 0.08f, 0.22f, 0.10f, 0.90f, 0.20f, pan: -0.10f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, electric ? Waveform.Triangle : Waveform.Saw, 0.32f, electric ? 7f : 2f, 0f, 0.01f, 0.35f, 0.00f, 0.75f, 0.22f, pan: 0.12f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 2, Waveform.Noise, bright ? 0.06f : 0.03f, 0f, 0f, 0.001f, 0.05f, 0.00f, 0.00f, 0.05f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = bright ? 7600f : 4600f;
        p.FilterResonance = electric ? 0.28f : 0.10f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, bright ? 0.35f : 0.20f);
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = electric ? 0.18f : 0.12f;
        p.ReverbSize = 0.30f;
        p.ReverbDamping = 0.38f;
        if (electric)
        {
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.14f;
            p.ChorusRateHz = 0.7f;
            p.ChorusDepth = 0.20f;
        }
    }

    private static void ConfigureElectricPiano(SynthParameters p, bool bright)
    {
        ConfigureOsc(p, 0, Waveform.Metallic, bright ? 0.34f : 0.26f, 0f, 0f, 0.001f, bright ? 0.12f : 0.08f, 0.00f, 0.00f, bright ? 0.18f : 0.14f, pan: -0.06f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, bright ? Waveform.Triangle : Waveform.Sine, bright ? 0.56f : 0.60f, 0f, bright ? 0f : 1.5f, 0.003f, bright ? 0.18f : 0.24f, bright ? 0.78f : 0.84f, bright ? 0.42f : 0.50f, 0.04f, pan: -0.10f);
        ConfigureOsc(p, 2, bright ? Waveform.Sine : Waveform.Triangle, bright ? 0.28f : 0.24f, bright ? 7f : -4f, 0f, 0.006f, bright ? 0.22f : 0.30f, bright ? 0.56f : 0.62f, bright ? 0.48f : 0.56f, 0.04f, pan: 0.10f);
        DisableOsc(p, 3);

        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = bright ? 4700f : 2800f;
        p.FilterResonance = bright ? 0.20f : 0.10f;
        ConfigureRoute(p, 0, ModulationSource.Lfo1, ModulationDestination.Gain, bright ? 0.08f : 0.12f);
        ConfigureRoute(p, 1, ModulationSource.Envelope, ModulationDestination.FilterCutoff, bright ? 0.18f : 0.12f);
        ConfigureRoute(p, 2, ModulationSource.KeyTrack, ModulationDestination.FilterCutoff, bright ? 0.14f : 0.08f);
        ConfigureLfo(p.Lfo1, ModulationLfoShape.Sine, bright ? 4.8f : 4.2f, 0.60f);

        p.ChorusType = bright ? ChorusType.Light : ChorusType.Wide;
        p.ChorusMix = bright ? 0.16f : 0.24f;
        p.ChorusRateHz = bright ? 0.85f : 0.60f;
        p.ChorusDepth = bright ? 0.20f : 0.30f;
        p.ChorusTremoloDepth = bright ? 0.16f : 0.24f;

        p.ReverbType = ReverbType.Room;
        p.ReverbMix = bright ? 0.10f : 0.14f;
        p.ReverbSize = bright ? 0.24f : 0.30f;
        p.ReverbDamping = 0.42f;
    }

    private static void ConfigureChromatic(SynthParameters p, bool metallic, bool vibraphone)
    {
        ConfigureOsc(p, 0, Waveform.Sine, 0.68f, 0f, vibraphone ? 15f : 0f, 0.004f, 0.80f, 0.00f, 0.00f, 1.40f, pan: vibraphone ? -0.18f : -0.08f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, metallic ? Waveform.Metallic : Waveform.Sine, metallic ? 0.22f : 0.28f, 12f, vibraphone ? 0f : 0f, 0.002f, 0.65f, 0.00f, 0.00f, 1.10f, pan: vibraphone ? 0.18f : 0.08f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = metallic ? 5200f : 4800f;
        p.FilterResonance = metallic ? 0.14f : 0.18f;
        p.ReverbType = ReverbType.Hall;
        p.ReverbMix = 0.26f;
        p.ReverbSize = 0.62f;
        p.ReverbDamping = 0.22f;
        if (vibraphone)
        {
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.12f;
            p.ChorusRateHz = 1.6f;
            p.ChorusDepth = 0.18f;
            p.ChorusTremoloDepth = 0.26f;
        }
    }

    private static void ConfigureOrgan(SynthParameters p, bool church)
    {
        ConfigureOsc(p, 0, Waveform.Organ, church ? 0.34f : 0.38f, 0f, 0f, 0.01f, 0.08f, 0.90f, 0.85f, 0.50f, pan: -0.32f);
        ConfigureOsc(p, 1, Waveform.Sine, 0.28f, 1200f, 0f, 0.01f, 0.06f, 0.88f, 0.82f, 0.52f);
        ConfigureOsc(p, 2, Waveform.Organ, church ? 0.16f : 0.18f, -1200f, 0f, 0.01f, 0.07f, 0.86f, 0.80f, 0.48f, pan: 0.32f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = church ? 3000f : 3600f;
        p.FilterResonance = church ? 0.12f : 0.14f;
        p.ChorusType = church ? ChorusType.Wide : ChorusType.Ensemble;
        p.ChorusMix = church ? 0.16f : 0.20f;
        p.ChorusRateHz = 0.35f;
        p.ChorusDepth = church ? 0.40f : 0.26f;
        p.ChorusTremoloDepth = church ? 0.12f : 0.08f;
        p.ReverbType = church ? ReverbType.Hall : ReverbType.Room;
        p.ReverbMix = church ? 0.30f : 0.14f;
        p.ReverbSize = church ? 0.82f : 0.35f;
        p.ReverbDamping = 0.32f;
    }

    private static void ConfigureGuitar(SynthParameters p, bool electric, bool muted)
    {
        ConfigureOsc(p, 0, electric ? Waveform.Saw : Waveform.Triangle, 0.48f, 0f, 0f, 0.002f, muted ? 0.10f : 0.18f, 0.00f, muted ? 0.12f : 0.22f, muted ? 0.08f : 0.25f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, Waveform.Sine, 0.26f, 7f, 0f, 0.001f, 0.10f, 0.00f, 0.00f, muted ? 0.06f : 0.18f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 2, Waveform.Noise, muted ? 0.10f : 0.04f, 0f, 0f, 0.001f, 0.03f, 0.00f, 0.00f, 0.03f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = muted ? 2600f : (electric ? 4200f : 5200f);
        p.FilterResonance = electric ? 0.22f : 0.12f;
        p.FilterEnvelopeAmount = 0.28f;
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = muted ? 0.06f : 0.12f;
        p.ReverbSize = 0.28f;
        p.ReverbDamping = 0.42f;
        if (electric)
        {
            p.DelayType = DelayType.Slap;
            p.DelayMix = 0.10f;
            p.DelayTimeSeconds = 0.16f;
            p.DelayFeedback = 0.18f;
        }
    }

    private static void ConfigureBass(SynthParameters p, bool synth)
    {
        ConfigureOsc(p, 0, synth ? Waveform.Saw : Waveform.Square, 0.58f, 0f, 0f, 0.003f, 0.12f, 0.65f, 0.52f, 0.18f, pulseWidth: synth ? 0.50f : 0.38f, pwmRate: synth ? 0f : 0.4f, pan: synth ? -0.08f : 0f);
        ConfigureOsc(p, 1, Waveform.Sine, 0.34f, -1200f, 0f, 0.003f, 0.08f, 0.70f, 0.56f, 0.22f, pan: synth ? 0.08f : 0f);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = synth ? 1400f : 2200f;
        p.FilterResonance = synth ? 0.34f : 0.18f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, synth ? 0.55f : 0.18f);
        if (synth)
        {
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.08f;
            p.ChorusRateHz = 0.45f;
            p.ChorusDepth = 0.18f;
        }
    }

    private static void ConfigureStrings(SynthParameters p, bool solo)
    {
        ConfigureOsc(p, 0, Waveform.Saw, 0.44f, 0f, solo ? 4f : 0f, 0.04f, solo ? 0.30f : 0.05f, solo ? 0.70f : 0.00f, solo ? 0.55f : 0.00f, solo ? 0.42f : 0.22f, pan: solo ? -0.18f : -0.24f);
        ConfigureOsc(p, 1, solo ? Waveform.Triangle : Waveform.Saw, 0.28f, solo ? 7f : -7f, 0f, 0.03f, solo ? 0.24f : 0.08f, solo ? 0.66f : 0.00f, solo ? 0.50f : 0.00f, solo ? 0.38f : 0.18f, pan: solo ? 0.14f : 0.24f);
        if (solo)
        {
            ConfigureOsc(p, 2, Waveform.Sine, 0.10f, 0f, 5f, 0.05f, 0.18f, 0.60f, 0.46f, 0.34f, pan: 0.04f);
        }
        else
        {
            ConfigureOsc(p, 2, Waveform.Noise, 0.06f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f, envelopeMode: EnvelopeMode.OneShot);
        }
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = solo ? 4800f : 3000f;
        p.FilterResonance = 0.12f;
        p.ChorusType = solo ? ChorusType.Light : ChorusType.Ensemble;
        p.ChorusMix = solo ? 0.12f : 0.22f;
        p.ChorusRateHz = 0.42f;
        p.ChorusDepth = solo ? 0.18f : 0.30f;
        p.ReverbType = ReverbType.Hall;
        p.ReverbMix = solo ? 0.16f : 0.10f;
        p.ReverbSize = 0.58f;
        p.ReverbDamping = 0.44f;
    }

    private static void ConfigureEnsemble(SynthParameters p, bool choir)
    {
        ConfigureOsc(p, 0, choir ? Waveform.Triangle : Waveform.Saw, choir ? 0.44f : 0.46f, 0f, 0f, 0.12f, 0.60f, 0.82f, 0.70f, 0.90f, pan: -0.34f);
        ConfigureOsc(p, 1, Waveform.Saw, choir ? 0.30f : 0.32f, 8f, 0f, 0.10f, 0.55f, 0.80f, 0.68f, 0.84f, pan: -0.12f);
        ConfigureOsc(p, 2, choir ? Waveform.Sine : Waveform.Triangle, choir ? 0.24f : 0.26f, -8f, choir ? 8f : 0f, 0.15f, 0.48f, 0.78f, 0.66f, 0.92f, pan: 0.12f);
        ConfigureOsc(p, 3, choir ? Waveform.Noise : Waveform.Saw, choir ? 0.05f : 0.18f, 0f, 0f, 0.03f, 0.30f, 0.70f, 0.58f, 0.70f, pan: 0.34f);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = choir ? 2400f : 3200f;
        p.FilterResonance = 0.10f;
        p.ChorusType = ChorusType.Wide;
        p.ChorusMix = choir ? 0.22f : 0.30f;
        p.ChorusRateHz = 0.28f;
        p.ChorusDepth = choir ? 0.26f : 0.36f;
        p.ChorusTremoloDepth = choir ? 0.12f : 0.08f;
        p.ReverbType = choir ? ReverbType.Shimmer : ReverbType.Hall;
        p.ReverbMix = choir ? 0.28f : 0.20f;
        p.ReverbSize = 0.86f;
        p.ReverbDamping = choir ? 0.26f : 0.38f;
    }

    private static void ConfigureBrass(SynthParameters p, bool mellow)
    {
        ConfigureOsc(p, 0, Waveform.Saw, mellow ? 0.46f : 0.58f, 0f, 0f, 0.01f, 0.12f, 0.76f, 0.58f, 0.24f);
        ConfigureOsc(p, 1, Waveform.Square, mellow ? 0.18f : 0.24f, mellow ? -4f : 3f, 0f, 0.01f, 0.10f, 0.70f, 0.54f, 0.20f, pulseWidth: mellow ? 0.42f : 0.48f, pwmRate: mellow ? 0.20f : 0f);
        ConfigureOsc(p, 2, Waveform.Noise, mellow ? 0.02f : 0.04f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = mellow ? 2600f : 3400f;
        p.FilterResonance = mellow ? 0.16f : 0.24f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, 0.45f);
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = mellow ? 0.14f : 0.10f;
        p.ReverbSize = 0.34f;
        p.ReverbDamping = 0.44f;
    }

    private static void ConfigureReed(SynthParameters p, bool clarinet)
    {
        ConfigureOsc(p, 0, clarinet ? Waveform.Square : Waveform.Saw, 0.46f, 0f, clarinet ? 8f : 14f, 0.03f, 0.15f, 0.74f, 0.62f, 0.18f, pulseWidth: clarinet ? 0.34f : 0.50f, pwmRate: clarinet ? 0.25f : 0f);
        ConfigureOsc(p, 1, Waveform.Triangle, 0.20f, clarinet ? 2f : -3f, clarinet ? 6f : 10f, 0.04f, 0.14f, 0.68f, 0.56f, 0.20f);
        ConfigureOsc(p, 2, Waveform.Noise, clarinet ? 0.01f : 0.05f, 0f, 0f, 0.001f, 0.05f, 0.00f, 0.00f, 0.04f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = clarinet ? 2200f : 2800f;
        p.FilterResonance = 0.22f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, 0.18f);
    }

    private static void ConfigurePipe(SynthParameters p, bool panFlute)
    {
        ConfigureOsc(p, 0, Waveform.Sine, 0.62f, 0f, panFlute ? 7f : 4f, 0.02f, 0.10f, 0.82f, 0.72f, 0.22f);
        ConfigureOsc(p, 1, Waveform.Triangle, 0.16f, 3f, panFlute ? 10f : 6f, 0.03f, 0.12f, 0.70f, 0.60f, 0.24f);
        ConfigureOsc(p, 2, Waveform.Noise, panFlute ? 0.06f : 0.03f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = panFlute ? 3600f : 5200f;
        p.FilterResonance = 0.12f;
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = panFlute ? 0.18f : 0.12f;
        p.ReverbSize = 0.42f;
        p.ReverbDamping = 0.30f;
        if (panFlute)
        {
            p.DelayType = DelayType.Slap;
            p.DelayMix = 0.08f;
            p.DelayTimeSeconds = 0.22f;
            p.DelayFeedback = 0.16f;
        }
    }

    private static void ConfigureLead(SynthParameters p, bool square)
    {
        ConfigureOsc(p, 0, square ? Waveform.Pulse : Waveform.Saw, square ? 0.48f : 0.54f, 0f, 0f, 0.01f, 0.12f, 0.82f, 0.68f, 0.20f, pulseWidth: 0.34f, pwmRate: square ? 3.2f : 0f, pan: -0.10f);
        ConfigureOsc(p, 1, Waveform.Saw, 0.22f, square ? 8f : 5f, 4f, 0.02f, 0.14f, 0.78f, 0.64f, 0.18f, pan: 0.10f);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = square ? 2800f : 5400f;
        p.FilterResonance = square ? 0.18f : 0.24f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, square ? 0.40f : 0.34f);
        p.DelayType = DelayType.Tape;
        p.DelayMix = square ? 0.16f : 0.12f;
        p.DelayTimeSeconds = 0.24f;
        p.DelayFeedback = 0.28f;
        if (square)
        {
            p.GetOscillator(0).PwmRateHz = 0f;
            ConfigureLfo(p.Lfo1, ModulationLfoShape.Sine, 3.2f, 0.75f);
            ConfigureRoute(p, 1, ModulationSource.Lfo1, ModulationDestination.PulseWidth, 0.38f, oscillatorIndex: 0);
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.10f;
            p.ChorusRateHz = 0.6f;
            p.ChorusDepth = 0.18f;
            p.ChorusTremoloDepth = 0.09f;
        }
        else
        {
            ConfigureRoute(p, 1, ModulationSource.KeyTrack, ModulationDestination.FilterCutoff, 0.20f);
        }
    }

    private static void ConfigurePad(SynthParameters p, bool airy)
    {
        ConfigureOsc(p, 0, airy ? Waveform.Triangle : Waveform.SuperSaw, airy ? 0.42f : 0.32f, 0f, 0f, 0.18f, 0.60f, 0.84f, 0.76f, 1.10f, pan: -0.30f);
        ConfigureOsc(p, 1, airy ? Waveform.Saw : Waveform.SuperSaw, airy ? 0.32f : 0.24f, 7f, 0f, 0.20f, 0.56f, 0.82f, 0.74f, 1.00f, pan: -0.10f);
        ConfigureOsc(p, 2, Waveform.Pulse, airy ? 0.14f : 0.16f, -7f, 0f, 0.18f, 0.48f, 0.78f, 0.70f, 1.08f, pulseWidth: airy ? 0.42f : 0.35f, pwmRate: airy ? 0.25f : 0.55f, pan: 0.10f);
        ConfigureOsc(p, 3, airy ? Waveform.Noise : Waveform.Sine, airy ? 0.06f : 0.12f, 0f, airy ? 0f : 3f, 0.22f, 0.42f, 0.76f, 0.68f, 1.10f, pan: 0.30f);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = airy ? 2800f : 2200f;
        p.FilterResonance = airy ? 0.14f : 0.12f;
        p.GetOscillator(2).PwmRateHz = 0f;
        ConfigureLfo(p.Lfo1, ModulationLfoShape.Sine, airy ? 0.22f : 0.30f, 0.85f);
        ConfigureLfo(p.Lfo2, ModulationLfoShape.Triangle, airy ? 0.12f : 0.18f, 0.65f);
        ConfigureRoute(p, 0, ModulationSource.Lfo1, ModulationDestination.Pan, airy ? 0.18f : 0.14f);
        ConfigureRoute(p, 1, ModulationSource.Lfo2, ModulationDestination.PulseWidth, airy ? 0.20f : 0.26f, oscillatorIndex: 2);
        ConfigureRoute(p, 2, ModulationSource.Envelope, ModulationDestination.FilterCutoff, airy ? 0.16f : 0.22f);
        p.ChorusType = airy ? ChorusType.Wide : ChorusType.Ensemble;
        p.ChorusMix = airy ? 0.24f : 0.20f;
        p.ChorusRateHz = 0.22f;
        p.ChorusDepth = airy ? 0.34f : 0.28f;
        p.ChorusTremoloDepth = airy ? 0.16f : 0.10f;
        p.ReverbType = airy ? ReverbType.Shimmer : ReverbType.Hall;
        p.ReverbMix = airy ? 0.26f : 0.20f;
        p.ReverbSize = 0.88f;
        p.ReverbDamping = airy ? 0.24f : 0.36f;
    }

    private static void ConfigureSynthFx(SynthParameters p, bool beam)
    {
        ConfigureOsc(p, 0, beam ? Waveform.Metallic : Waveform.PinkNoise, beam ? 0.36f : 0.32f, 0f, beam ? 0f : 0f, 0.08f, 0.34f, 0.72f, 0.58f, 0.80f, pan: -0.24f);
        ConfigureOsc(p, 1, beam ? Waveform.Pulse : Waveform.Triangle, beam ? 0.26f : 0.28f, beam ? 12f : -8f, beam ? 5f : 0f, 0.06f, 0.40f, 0.70f, 0.54f, 0.88f, pulseWidth: 0.30f, pwmRate: beam ? 4.0f : 1.2f, pan: 0.24f);
        ConfigureOsc(p, 2, Waveform.Sine, beam ? 0.22f : 0.20f, beam ? -1200f : 0f, beam ? 2f : 0f, 0.12f, 0.50f, 0.68f, 0.50f, 0.92f, pan: beam ? 0.10f : 0f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.BandPass;
        p.FilterCutoffHz = beam ? 1500f : 1000f;
        p.FilterResonance = beam ? 0.36f : 0.26f;
        ConfigureLfo(p.Lfo1, beam ? ModulationLfoShape.Saw : ModulationLfoShape.Sine, beam ? 1.20f : 0.30f, 1f);
        ConfigureLfo(p.Lfo2, beam ? ModulationLfoShape.Triangle : ModulationLfoShape.Sine, beam ? 0.38f : 0.18f, 0.80f);
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, beam ? 0.80f : 0.40f);
        ConfigureRoute(p, 1, ModulationSource.Lfo1, ModulationDestination.FilterCutoff, beam ? 0.55f : 0.28f);
        ConfigureRoute(p, 2, ModulationSource.Lfo2, ModulationDestination.Pan, beam ? 0.22f : 0.12f);
        p.DelayType = beam ? DelayType.PingPong : DelayType.Tape;
        p.DelayMix = beam ? 0.22f : 0.16f;
        p.DelayTimeSeconds = beam ? 0.28f : 0.42f;
        p.DelayFeedback = beam ? 0.42f : 0.35f;
        p.ReverbType = beam ? ReverbType.Shimmer : ReverbType.Hall;
        p.ReverbMix = beam ? 0.12f : 0.20f;
        p.ReverbSize = 0.76f;
        p.ReverbDamping = beam ? 0.18f : 0.32f;
        if (!beam)
        {
            ConfigureRoute(p, 3, ModulationSource.Lfo2, ModulationDestination.ReverbMix, 0.12f);
        }
    }

    private static void ConfigureEthnic(SynthParameters p, bool plucked)
    {
        ConfigureOsc(p, 0, plucked ? Waveform.Saw : Waveform.Square, 0.46f, 0f, 0f, 0.002f, 0.14f, 0.00f, 0.22f, 0.20f, pulseWidth: plucked ? 0.50f : 0.28f, pwmRate: plucked ? 0f : 0.8f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, Waveform.Sine, 0.18f, plucked ? 7f : 14f, 0f, 0.001f, 0.10f, 0.00f, 0.00f, 0.14f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 2, Waveform.Noise, plucked ? 0.04f : 0.08f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = plucked ? 3600f : 2200f;
        p.FilterResonance = plucked ? 0.26f : 0.32f;
        p.FilterEnvelopeAmount = 0.34f;
        p.DelayType = DelayType.Slap;
        p.DelayMix = plucked ? 0.12f : 0.06f;
        p.DelayTimeSeconds = 0.18f;
        p.DelayFeedback = 0.20f;
    }

    private static void ConfigurePercussive(SynthParameters p, bool tuned)
    {
        ConfigureOsc(p, 0, tuned ? Waveform.Sine : Waveform.Triangle, tuned ? 0.74f : 0.46f, 0f, 0f, 0.001f, tuned ? 0.55f : 0.18f, 0.00f, 0.00f, tuned ? 0.80f : 0.40f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, tuned ? Waveform.Triangle : Waveform.Noise, tuned ? 0.18f : 0.30f, 12f, 0f, 0.001f, tuned ? 0.28f : 0.20f, 0.00f, 0.00f, tuned ? 0.50f : 0.24f, envelopeMode: EnvelopeMode.OneShot);
        if (tuned)
        {
            DisableOsc(p, 2);
        }
        else
        {
            ConfigureOsc(p, 2, Waveform.Sine, 0.18f, -1200f, 0f, 0.001f, 0.24f, 0.00f, 0.00f, 0.28f, envelopeMode: EnvelopeMode.OneShot);
        }
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = tuned ? 2800f : 1800f;
        p.FilterResonance = tuned ? 0.22f : 0.18f;
        p.ReverbType = tuned ? ReverbType.Room : ReverbType.Hall;
        p.ReverbMix = tuned ? 0.12f : 0.18f;
        p.ReverbSize = tuned ? 0.38f : 0.62f;
        p.ReverbDamping = 0.40f;
    }

    private static void ConfigureSoundFx(SynthParameters p, bool burst)
    {
        ConfigureOsc(p, 0, burst ? Waveform.Noise : Waveform.PinkNoise, burst ? 0.64f : 0.38f, 0f, 0f, 0.02f, burst ? 0.20f : 0.80f, burst ? 0.50f : 0.70f, burst ? 0.36f : 0.52f, burst ? 0.18f : 0.90f, pan: burst ? -0.20f : -0.12f);
        ConfigureOsc(p, 1, burst ? Waveform.Pulse : Waveform.Sine, burst ? 0.28f : 0.14f, 0f, burst ? 0f : 0f, 0.01f, 0.20f, burst ? 0.60f : 0.72f, burst ? 0.42f : 0.54f, burst ? 0.16f : 0.80f, pulseWidth: 0.24f, pwmRate: burst ? 6.4f : 0f, pan: burst ? 0.20f : 0.12f);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = burst ? FilterType.BandPass : FilterType.HighPass;
        p.FilterCutoffHz = burst ? 950f : 3200f;
        p.FilterResonance = burst ? 0.34f : 0.18f;
        ConfigureLfo(p.Lfo1, burst ? ModulationLfoShape.Square : ModulationLfoShape.Sine, burst ? 5.5f : 0.24f, 1f);
        ConfigureRoute(p, 0, ModulationSource.Lfo1, ModulationDestination.FilterCutoff, burst ? 0.22f : 0.16f);
        ConfigureRoute(p, 1, ModulationSource.Lfo1, ModulationDestination.Pan, burst ? 0.35f : 0.10f);
        p.ReverbType = burst ? ReverbType.Room : ReverbType.Hall;
        p.ReverbMix = burst ? 0.06f : 0.18f;
        p.ReverbSize = burst ? 0.26f : 0.82f;
        p.ReverbDamping = burst ? 0.44f : 0.18f;
        if (burst)
        {
            p.DelayType = DelayType.PingPong;
            p.DelayMix = 0.14f;
            p.DelayTimeSeconds = 0.12f;
            p.DelayFeedback = 0.20f;
            ConfigureRoute(p, 2, ModulationSource.Lfo1, ModulationDestination.DelayMix, 0.10f);
        }
        else
        {
            ConfigureLfo(p.Lfo2, ModulationLfoShape.Triangle, 0.08f, 0.60f);
            ConfigureRoute(p, 2, ModulationSource.Lfo2, ModulationDestination.ReverbMix, 0.08f);
        }
    }

    private static void ConfigureLfo(ModulationLfoParameters lfo, ModulationLfoShape shape, float rateHz, float depth)
    {
        lfo.Shape = shape;
        lfo.RateHz = rateHz;
        lfo.Depth = depth;
    }

    private static void ConfigureRoute(SynthParameters parameters, int routeIndex, ModulationSource source, ModulationDestination destination, float amount, int oscillatorIndex = -1)
    {
        ModulationRoute route = parameters.GetModulationRoute(routeIndex);
        route.Source = source;
        route.Destination = destination;
        route.Amount = amount;
        route.OscillatorIndex = oscillatorIndex;
    }

    private static void ConfigureOsc(
        SynthParameters parameters,
        int index,
        Waveform waveform,
        float gain,
        float detuneCents,
        float vibratoDepthCents,
        float attackSeconds,
        float decaySeconds,
        float sustainLevel,
        float releaseSeconds,
        float glideSeconds,
        float pulseWidth = 0.50f,
        float pwmRate = 0f,
        float pan = 0f,
        EnvelopeMode envelopeMode = EnvelopeMode.Sustain)
    {
        OscillatorParameters osc = parameters.GetOscillator(index);
        osc.Enabled = true;
        osc.Waveform = waveform;
        osc.Gain = gain;
        osc.DetuneCents = detuneCents;
        osc.VibratoDepthCents = vibratoDepthCents;
        osc.VibratoRateHz = vibratoDepthCents > 0f ? 5.2f : 0f;
        osc.AttackSeconds = attackSeconds;
        osc.DecaySeconds = decaySeconds;
        osc.SustainLevel = sustainLevel;
        osc.ReleaseSeconds = releaseSeconds;
        osc.GlideSeconds = glideSeconds;
        osc.PulseWidth = pulseWidth;
        osc.PwmRateHz = pwmRate;
        osc.Pan = pan;
        osc.EnvelopeMode = envelopeMode;
    }

    private static void DisableOsc(SynthParameters parameters, int index)
    {
        OscillatorParameters osc = parameters.GetOscillator(index);
        osc.Enabled = false;
        osc.Gain = 0f;
        osc.DetuneCents = 0f;
        osc.VibratoDepthCents = 0f;
        osc.VibratoRateHz = 0f;
        osc.GlideSeconds = 0f;
        osc.PwmRateHz = 0f;
        osc.Pan = 0f;
        osc.EnvelopeMode = EnvelopeMode.Sustain;
    }
}

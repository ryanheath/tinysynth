using Raylib_CSharp.Audio;
using Raylib_CSharp.Colors;
using Raylib_CSharp.Interact;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using Raylib_CSharp.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using TinySynth.Synth;
using TinySynth.UI;

namespace TinySynth.App;

internal enum ParameterSection
{
    Presets,
    Oscillator,
    Filter,
    Fx
}

internal sealed class TinySynthController
{
    private static readonly (KeyboardKey Key, int MidiNote)[] _computerKeyboardMappings =
    [
        (KeyboardKey.Z, 60),
        (KeyboardKey.S, 61),
        (KeyboardKey.X, 62),
        (KeyboardKey.D, 63),
        (KeyboardKey.C, 64),
        (KeyboardKey.V, 65),
        (KeyboardKey.G, 66),
        (KeyboardKey.B, 67),
        (KeyboardKey.H, 68),
        (KeyboardKey.N, 69),
        (KeyboardKey.J, 70),
        (KeyboardKey.M, 71),
        (KeyboardKey.Comma, 72)
    ];

    private readonly int _keyboardStartMidi;
    private readonly int _keyboardNoteCount;
    private readonly float _panelGap;
    private readonly float _panelMargin;
    private readonly float _controlPanelHeight;
    private readonly float _keyboardPanelHeight;
    private readonly int _sampleRate;
    private readonly AudioStream _audioStream;
    private readonly IntPtr _audioBufferPointer;
    private readonly float[] _audioBuffer;
    private readonly float[] _scopeBuffer;
    private readonly SynthParameters _synthParameters;
    private readonly SynthEngine _synthEngine;
    private readonly HashSet<int> _currentKeyboardMidiNotes = [];
    private readonly HashSet<int> _currentPhysicalMidiNotes = [];
    private readonly HashSet<int> _previousPhysicalMidiNotes = [];
    private readonly List<int> _noteChangeBuffer = [];

    private readonly Color _backgroundColor = new(242, 245, 250, 255);
    private readonly Color _panelColor = new(252, 253, 255, 255);
    private readonly Color _keyboardPanelColor = new(233, 238, 247, 255);
    private readonly Color _borderColor = new(208, 214, 224, 255);
    private readonly Color _textColor = new(52, 60, 76, 255);
    private readonly Color _mutedTextColor = new(105, 114, 132, 255);
    private readonly Color _accentColor = new(84, 146, 255, 255);
    private readonly Color _accentStrongColor = new(47, 111, 237, 255);
    private readonly Color _accentSoftColor = new(213, 231, 255, 255);
    private readonly Color _whiteKeyColor = new(255, 255, 255, 255);
    private readonly Color _darkKeyColor = new(40, 46, 60, 255);

    private int _activePointerMidiNote = -1;
    private int _activeOscillatorIndex;
    private int _activePresetFamilyIndex;
    private int _activePresetIndex;
    private int _activeSlider = -1;
    private int _scopeWriteIndex;
    private bool _holdPedalEnabled;
    private ParameterSection _activeParameterSection = ParameterSection.Presets;
    private float _masterVolume;

    public TinySynthController(
        AudioStream audioStream,
        IntPtr audioBufferPointer,
        float[] audioBuffer,
        int keyboardStartMidi,
        int keyboardNoteCount,
        int sampleRate,
        float masterGain,
        float panelGap,
        float panelMargin,
        float controlPanelHeight,
        float keyboardPanelHeight)
    {
        _audioStream = audioStream;
        _audioBufferPointer = audioBufferPointer;
        _audioBuffer = audioBuffer;
        _scopeBuffer = new float[2048];
        _keyboardStartMidi = keyboardStartMidi;
        _keyboardNoteCount = keyboardNoteCount;
        _panelGap = panelGap;
        _panelMargin = panelMargin;
        _controlPanelHeight = controlPanelHeight;
        _keyboardPanelHeight = keyboardPanelHeight;
        _sampleRate = sampleRate;
        _synthParameters = new SynthParameters();
        GmPresetCatalog.ApplyPreset(GmPresetCatalog.GetPresets(GmPresetCatalog.Families[_activePresetFamilyIndex])[_activePresetIndex], _synthParameters);
        _synthEngine = new SynthEngine(sampleRate, masterGain, keyboardStartMidi, voiceCount: 4);
        _masterVolume = masterGain;
    }

    public void RunFrame()
    {
        if (Window.IsMinimized())
        {
            return;
        }

        Vector2 dpiScale = Window.GetScaleDPI();
        float dpiScaleX = dpiScale.X > 0f ? dpiScale.X : 1f;
        float dpiScaleY = dpiScale.Y > 0f ? dpiScale.Y : 1f;
        int currentScreenWidth = (int)MathF.Round(Window.GetRenderWidth() / dpiScaleX);
        int currentScreenHeight = (int)MathF.Round(Window.GetRenderHeight() / dpiScaleY);

        if (currentScreenWidth <= 0 || currentScreenHeight <= 0)
        {
            return;
        }

        LayoutMetrics layout = LayoutCalculator.Calculate(currentScreenWidth, currentScreenHeight, _panelMargin, _panelGap, _controlPanelHeight, _keyboardPanelHeight);
        Rectangle controlPanel = layout.ControlPanel;
        Rectangle waveformPanel = layout.WaveformPanel;
        Rectangle keyboardPanel = layout.KeyboardPanel;
        OscillatorParameters activeOscillator = _synthParameters.GetOscillator(_activeOscillatorIndex);

        Vector2 mousePosition = Input.GetMousePosition();
        bool mousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
        bool mouseDown = Input.IsMouseButtonDown(MouseButton.Left);
        bool mouseReleased = Input.IsMouseButtonReleased(MouseButton.Left);
        bool holdPedalTogglePressed = Input.IsKeyPressed(KeyboardKey.Space);

        if (!mouseDown)
        {
            _activeSlider = -1;
        }

        float sliderRowOneY = layout.SliderRowOneY;
        float sliderRowTwoY = layout.SliderRowTwoY;
        float sliderRowThreeY = layout.SliderRowThreeY;
        float sliderWidth = layout.SliderWidth;
        float filterSliderY = layout.FilterSliderY;
        float filterSliderRowTwoY = layout.FilterSliderRowTwoY;
        float filterSliderRowThreeY = layout.FilterSliderRowThreeY;
        float filterSliderWidth = layout.FilterSliderWidth;
        float filterFullWidth = layout.FilterFullWidth;
        float fxSliderY = layout.FxSliderY;
        float fxSliderRowTwoY = layout.FxSliderRowTwoY;
        float fxSliderWidth = layout.FxSliderWidth;
        float fxFullWidth = layout.FxFullWidth;
        IReadOnlyList<GmInstrumentFamily> presetFamilies = GmPresetCatalog.Families;
        IReadOnlyList<GmPreset> visiblePresets = GmPresetCatalog.GetPresets(presetFamilies[_activePresetFamilyIndex]);
        float maxFilterCutoffHz = MathF.Max(20f, _sampleRate * 0.45f);
        Rectangle filterAnalysisArea = layout.FilterAnalysisArea;
        Rectangle holdPedalBounds = new(keyboardPanel.X + keyboardPanel.Width - 190, keyboardPanel.Y + 12, 192, 24);
        Rectangle masterVolumeBounds = new(keyboardPanel.X + keyboardPanel.Width - 400, keyboardPanel.Y + 12, 180, 20);

        if (holdPedalTogglePressed || (mousePressed && SynthRenderer.Contains(holdPedalBounds, mousePosition)))
        {
            _holdPedalEnabled = !_holdPedalEnabled;
            _synthEngine.SetHoldPedal(_holdPedalEnabled);
        }

        PianoKeyLayout[] keys = KeyboardLayoutBuilder.Build(keyboardPanel, _keyboardStartMidi, _keyboardNoteCount);
        int hoveredMidiNote = KeyboardLayoutBuilder.GetHoveredMidiNote(keys, mousePosition);
        UpdatePointerMidiNote(mousePressed, mouseDown, mouseReleased, hoveredMidiNote);
        GetPressedKeyboardMidiNotes(_currentKeyboardMidiNotes);
        SyncPhysicalNotes();

        while (_audioStream.IsProcessed())
        {
            _synthEngine.FillBuffer(_audioBuffer, _scopeBuffer, ref _scopeWriteIndex, _synthParameters);
            Marshal.Copy(_audioBuffer, 0, _audioBufferPointer, _audioBuffer.Length);
            _audioStream.Update(_audioBufferPointer, _audioBuffer.Length);
        }

        Graphics.BeginDrawing();
        Graphics.ClearBackground(_backgroundColor);

        SynthRenderer.DrawPanel(controlPanel, _panelColor, _borderColor);
        SynthRenderer.DrawPanel(waveformPanel, _panelColor, _borderColor);
        SynthRenderer.DrawPanel(keyboardPanel, _keyboardPanelColor, _borderColor);

        Graphics.DrawText("TinySynth", (int)controlPanel.X + 20, (int)controlPanel.Y + 12, 28, _textColor);
        Graphics.DrawText("Parameters", (int)controlPanel.X + 20, (int)controlPanel.Y + 42, 18, _mutedTextColor);

        ParameterSection previousParameterSection = _activeParameterSection;
        _activeParameterSection = (ParameterSection)SynthRenderer.DrawParameterSectionButtons(layout.ModeButtonsArea, (int)_activeParameterSection, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

        if (previousParameterSection != _activeParameterSection)
        {
            _activeSlider = -1;
        }

        if (_activeParameterSection == ParameterSection.Presets)
        {
            Graphics.DrawText("GM presets", (int)controlPanel.X + 20, (int)controlPanel.Y + 112, 18, _mutedTextColor);

            int previousFamilyIndex = _activePresetFamilyIndex;
            _activePresetFamilyIndex = SynthRenderer.DrawPresetFamilyButtons(
                layout.PresetFamilyArea,
                presetFamilies,
                _activePresetFamilyIndex,
                mousePosition,
                mousePressed,
                _panelColor,
                _borderColor,
                _accentSoftColor,
                _accentStrongColor,
                _textColor);

            if (previousFamilyIndex != _activePresetFamilyIndex)
            {
                _activePresetIndex = 0;
                visiblePresets = GmPresetCatalog.GetPresets(presetFamilies[_activePresetFamilyIndex]);
            }

            visiblePresets = GmPresetCatalog.GetPresets(presetFamilies[_activePresetFamilyIndex]);
            int previousPresetIndex = _activePresetIndex;
            _activePresetIndex = SynthRenderer.DrawPresetButtons(
                layout.PresetOptionArea,
                visiblePresets,
                _activePresetIndex,
                mousePosition,
                mousePressed,
                _panelColor,
                _borderColor,
                _accentSoftColor,
                _accentStrongColor,
                _textColor,
                _mutedTextColor);

            if (previousPresetIndex != _activePresetIndex || (previousFamilyIndex != _activePresetFamilyIndex && visiblePresets.Count > 0))
            {
                GmPresetCatalog.ApplyPreset(visiblePresets[_activePresetIndex], _synthParameters);
                _activeOscillatorIndex = 0;
                _activeSlider = -1;
            }
        }
        else if (_activeParameterSection == ParameterSection.Oscillator)
        {
            int previousOscillatorIndex = _activeOscillatorIndex;
            _activeOscillatorIndex = SynthRenderer.DrawOscillatorButtons(layout.OscillatorButtonsArea, _synthParameters.Oscillators, _activeOscillatorIndex, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

            if (previousOscillatorIndex != _activeOscillatorIndex)
            {
                activeOscillator = _synthParameters.GetOscillator(_activeOscillatorIndex);
                _activeSlider = -1;
            }

            Graphics.DrawText($"Waveform · Oscillator {_activeOscillatorIndex + 1}", (int)controlPanel.X + 20, (int)controlPanel.Y + 162, 18, _mutedTextColor);
            Rectangle oscillatorEnabledBounds = new(controlPanel.X + 410, controlPanel.Y + 158, 192, 24);

            if (mousePressed && SynthRenderer.Contains(oscillatorEnabledBounds, mousePosition))
            {
                activeOscillator.Enabled = !activeOscillator.Enabled;
                _activeSlider = -1;
            }

            SynthRenderer.DrawToggle(
                oscillatorEnabledBounds,
                "Enabled",
                activeOscillator.Enabled,
                _accentColor,
                _accentSoftColor,
                _borderColor,
                _panelColor,
                _textColor,
                _mutedTextColor);

            activeOscillator.Waveform = SynthRenderer.DrawWaveformButtons(layout.WaveformButtonsArea, activeOscillator.Waveform, activeOscillator.Enabled, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

            activeOscillator.Gain = SynthRenderer.DrawSlider(
            index: 0,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Gain",
            valueLabel: $"{activeOscillator.Gain:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.Gain,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.DetuneCents = SynthRenderer.DrawSlider(
            index: 1,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Detune",
            valueLabel: $"{activeOscillator.DetuneCents:0} ct",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.DetuneCents,
            minValue: -100.00f,
            maxValue: 100.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.GlideSeconds = SynthRenderer.DrawSlider(
            index: 2,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Glide",
            valueLabel: $"{activeOscillator.GlideSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.GlideSeconds,
            minValue: 0.00f,
            maxValue: 1.50f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.VibratoDepthCents = SynthRenderer.DrawSlider(
            index: 3,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Vib depth",
            valueLabel: $"{activeOscillator.VibratoDepthCents:0} ct",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.VibratoDepthCents,
            minValue: 0.00f,
            maxValue: 100.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.VibratoRateHz = SynthRenderer.DrawSlider(
            index: 4,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Vib rate",
            valueLabel: $"{activeOscillator.VibratoRateHz:0.0}Hz",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 4), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.VibratoRateHz,
            minValue: 0.10f,
            maxValue: 12.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.AttackSeconds = SynthRenderer.DrawSlider(
            index: 5,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Attack",
            valueLabel: $"{activeOscillator.AttackSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.AttackSeconds,
            minValue: 0.01f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.DecaySeconds = SynthRenderer.DrawSlider(
            index: 6,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Decay",
            valueLabel: $"{activeOscillator.DecaySeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.DecaySeconds,
            minValue: 0.01f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.SustainLevel = SynthRenderer.DrawSlider(
            index: 7,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Sustain",
            valueLabel: $"{activeOscillator.SustainLevel:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.SustainLevel,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.ReleaseSeconds = SynthRenderer.DrawSlider(
            index: 8,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Release",
            valueLabel: $"{activeOscillator.ReleaseSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.ReleaseSeconds,
            minValue: 0.01f,
            maxValue: 2.50f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.PulseWidth = SynthRenderer.DrawSlider(
            index: 24,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled && activeOscillator.Waveform == Waveform.Square,
            label: "Pulse width",
            valueLabel: $"{(activeOscillator.PulseWidth * 100f):0}%",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowThreeY, sliderWidth, 20),
            value: activeOscillator.PulseWidth,
            minValue: 0.10f,
            maxValue: 0.90f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

            activeOscillator.PwmRateHz = SynthRenderer.DrawSlider(
            index: 25,
            activeSlider: ref _activeSlider,
            enabled: activeOscillator.Enabled && activeOscillator.Waveform == Waveform.Square,
            label: "PWM rate",
            valueLabel: $"{activeOscillator.PwmRateHz:0.0}Hz",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowThreeY, sliderWidth, 20),
            value: activeOscillator.PwmRateHz,
            minValue: 0.00f,
            maxValue: 12.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

        }
        else if (_activeParameterSection == ParameterSection.Filter)
        {
            Graphics.DrawText("Filter routing", (int)controlPanel.X + 20, (int)controlPanel.Y + 138, 18, _mutedTextColor);
            _synthParameters.FilterType = SynthRenderer.DrawFilterButtons(layout.FilterButtonsArea, _synthParameters.FilterType, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

            float normalizedCutoff = GetLogNormalizedFrequency(_synthParameters.FilterCutoffHz, 20f, maxFilterCutoffHz);

            normalizedCutoff = SynthRenderer.DrawSlider(
                index: 10,
                activeSlider: ref _activeSlider,
                enabled: _synthParameters.FilterType != FilterType.Off,
                label: "Cutoff",
                valueLabel: $"{_synthParameters.FilterCutoffHz:0} Hz",
                bounds: new Rectangle(controlPanel.X + 20, filterSliderY, filterSliderWidth, 20),
                value: normalizedCutoff,
                minValue: 0f,
                maxValue: 1f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.FilterCutoffHz = GetFrequencyFromLogNormalized(normalizedCutoff, 20f, maxFilterCutoffHz);

            _synthParameters.FilterResonance = SynthRenderer.DrawSlider(
                index: 11,
                activeSlider: ref _activeSlider,
                enabled: _synthParameters.FilterType != FilterType.Off,
                label: "Resonance",
                valueLabel: $"{_synthParameters.FilterResonance:0.00}",
                bounds: new Rectangle(controlPanel.X + 20 + filterSliderWidth + 18, filterSliderY, filterSliderWidth, 20),
                value: _synthParameters.FilterResonance,
                minValue: 0.00f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.FilterEnvelopeAmount = SynthRenderer.DrawSlider(
                index: 12,
                activeSlider: ref _activeSlider,
                enabled: _synthParameters.FilterType != FilterType.Off,
                label: "Env amt",
                valueLabel: $"{_synthParameters.FilterEnvelopeAmount:0.00}x",
                bounds: new Rectangle(controlPanel.X + 20, filterSliderRowTwoY, filterSliderWidth, 20),
                value: _synthParameters.FilterEnvelopeAmount,
                minValue: -2.00f,
                maxValue: 2.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.FilterLfoDepth = SynthRenderer.DrawSlider(
                index: 13,
                activeSlider: ref _activeSlider,
                enabled: _synthParameters.FilterType != FilterType.Off,
                label: "LFO depth",
                valueLabel: $"{_synthParameters.FilterLfoDepth:0.00}x",
                bounds: new Rectangle(controlPanel.X + 20 + filterSliderWidth + 18, filterSliderRowTwoY, filterSliderWidth, 20),
                value: _synthParameters.FilterLfoDepth,
                minValue: 0.00f,
                maxValue: 2.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.FilterLfoRateHz = SynthRenderer.DrawSlider(
                index: 14,
                activeSlider: ref _activeSlider,
                enabled: _synthParameters.FilterType != FilterType.Off,
                label: "LFO rate",
                valueLabel: $"{_synthParameters.FilterLfoRateHz:0.0}Hz",
                bounds: new Rectangle(controlPanel.X + 20, filterSliderRowThreeY, filterFullWidth, 20),
                value: _synthParameters.FilterLfoRateHz,
                minValue: 0.10f,
                maxValue: 12.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            SynthRenderer.DrawFilterAnalysis(
                filterAnalysisArea,
                _scopeBuffer,
                _scopeWriteIndex,
                sampleRate: _sampleRate,
                filterType: _synthParameters.FilterType,
                cutoffHz: _synthParameters.FilterCutoffHz,
                resonance: _synthParameters.FilterResonance,
                spectrumColor: _accentStrongColor,
                responseColor: _accentColor,
                borderColor: _borderColor,
                labelColor: _mutedTextColor);
        }
        else
        {
            float fxMiddleColumnX = layout.FxReverbButtonsArea.X;
            float fxRightColumnX = layout.FxDelayButtonsArea.X;

            Graphics.DrawText("Chorus type", (int)controlPanel.X + 20, (int)controlPanel.Y + 138, 18, _mutedTextColor);
            _synthParameters.ChorusType = SynthRenderer.DrawChorusButtons(layout.FxChorusButtonsArea, _synthParameters.ChorusType, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

            Graphics.DrawText("Reverb type", (int)fxMiddleColumnX, (int)controlPanel.Y + 138, 18, _mutedTextColor);
            _synthParameters.ReverbType = SynthRenderer.DrawReverbButtons(layout.FxReverbButtonsArea, _synthParameters.ReverbType, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

            Graphics.DrawText("Delay type", (int)fxRightColumnX, (int)controlPanel.Y + 138, 18, _mutedTextColor);
            _synthParameters.DelayType = SynthRenderer.DrawDelayButtons(layout.FxDelayButtonsArea, _synthParameters.DelayType, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

            bool chorusEnabled = _synthParameters.ChorusType != ChorusType.Off;
            bool reverbEnabled = _synthParameters.ReverbType != ReverbType.Off;
            bool delayEnabled = _synthParameters.DelayType != DelayType.Off;

            _synthParameters.ChorusMix = SynthRenderer.DrawSlider(
                index: 15,
                activeSlider: ref _activeSlider,
                enabled: chorusEnabled,
                label: "Chorus mix",
                valueLabel: $"{(_synthParameters.ChorusMix * 100f):0}%",
                bounds: new Rectangle(controlPanel.X + 20, fxSliderY, fxSliderWidth, 20),
                value: _synthParameters.ChorusMix,
                minValue: 0.00f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.ChorusRateHz = SynthRenderer.DrawSlider(
                index: 16,
                activeSlider: ref _activeSlider,
                enabled: chorusEnabled,
                label: "Chorus rate",
                valueLabel: $"{_synthParameters.ChorusRateHz:0.0}Hz",
                bounds: new Rectangle(controlPanel.X + 20, fxSliderRowTwoY, fxSliderWidth, 20),
                value: _synthParameters.ChorusRateHz,
                minValue: 0.10f,
                maxValue: 3.50f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.ChorusDepth = SynthRenderer.DrawSlider(
                index: 17,
                activeSlider: ref _activeSlider,
                enabled: chorusEnabled,
                label: "Chorus depth",
                valueLabel: $"{_synthParameters.ChorusDepth:0.00}",
                bounds: new Rectangle(controlPanel.X + 20, fxSliderRowTwoY + 56, fxSliderWidth, 20),
                value: _synthParameters.ChorusDepth,
                minValue: 0.05f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.ReverbMix = SynthRenderer.DrawSlider(
                index: 18,
                activeSlider: ref _activeSlider,
                enabled: reverbEnabled,
                label: "Reverb mix",
                valueLabel: $"{(_synthParameters.ReverbMix * 100f):0}%",
                bounds: new Rectangle(fxMiddleColumnX, fxSliderY, fxSliderWidth, 20),
                value: _synthParameters.ReverbMix,
                minValue: 0.00f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.ReverbSize = SynthRenderer.DrawSlider(
                index: 19,
                activeSlider: ref _activeSlider,
                enabled: reverbEnabled,
                label: "Reverb size",
                valueLabel: $"{_synthParameters.ReverbSize:0.00}",
                bounds: new Rectangle(fxMiddleColumnX, fxSliderRowTwoY, fxSliderWidth, 20),
                value: _synthParameters.ReverbSize,
                minValue: 0.10f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.ReverbDamping = SynthRenderer.DrawSlider(
                index: 20,
                activeSlider: ref _activeSlider,
                enabled: reverbEnabled,
                label: "Reverb damp",
                valueLabel: $"{_synthParameters.ReverbDamping:0.00}",
                bounds: new Rectangle(fxMiddleColumnX, fxSliderRowTwoY + 56, fxSliderWidth, 20),
                value: _synthParameters.ReverbDamping,
                minValue: 0.00f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.DelayMix = SynthRenderer.DrawSlider(
                index: 21,
                activeSlider: ref _activeSlider,
                enabled: delayEnabled,
                label: "Delay mix",
                valueLabel: $"{(_synthParameters.DelayMix * 100f):0}%",
                bounds: new Rectangle(fxRightColumnX, fxSliderY, fxSliderWidth, 20),
                value: _synthParameters.DelayMix,
                minValue: 0.00f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.DelayTimeSeconds = SynthRenderer.DrawSlider(
                index: 22,
                activeSlider: ref _activeSlider,
                enabled: delayEnabled,
                label: "Delay time",
                valueLabel: $"{_synthParameters.DelayTimeSeconds:0.00}s",
                bounds: new Rectangle(fxRightColumnX, fxSliderRowTwoY, fxSliderWidth, 20),
                value: _synthParameters.DelayTimeSeconds,
                minValue: 0.05f,
                maxValue: 0.90f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);

            _synthParameters.DelayFeedback = SynthRenderer.DrawSlider(
                index: 23,
                activeSlider: ref _activeSlider,
                enabled: delayEnabled,
                label: "Delay fb",
                valueLabel: $"{_synthParameters.DelayFeedback:0.00}",
                bounds: new Rectangle(fxRightColumnX, fxSliderRowTwoY + 56, fxSliderWidth, 20),
                value: _synthParameters.DelayFeedback,
                minValue: 0.00f,
                maxValue: 0.85f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown,
                accentColor: _accentColor,
                accentSoftColor: _accentSoftColor,
                borderColor: _borderColor,
                panelColor: _panelColor,
                textColor: _textColor,
                mutedTextColor: _mutedTextColor);
        }

        int displayMidiNote = _synthEngine.DisplayMidiNote;
        string noteStatus = displayMidiNote >= 0
            ? $"Playing {MidiUtilities.MidiToNoteName(displayMidiNote)}  •  {_synthEngine.DisplayFrequency:0.0} Hz  •  {_synthEngine.ActiveVoiceCount} voices"
            : "Click the piano keys or use ZSXDCVGBHNJM, from C4 to C5.";
        Graphics.DrawText(noteStatus, (int)controlPanel.X + 470, (int)controlPanel.Y + 52, 20, _textColor);
        Graphics.DrawText($"Envelope: {_synthEngine.DisplayEnvelopeStage}", (int)controlPanel.X + 470, (int)controlPanel.Y + 82, 18, _mutedTextColor);

        SynthRenderer.DrawWaveformScope(waveformPanel, _scopeBuffer, _scopeWriteIndex, _accentStrongColor, _borderColor, _mutedTextColor);
        Graphics.DrawText("Keyboard", (int)keyboardPanel.X + 18, (int)keyboardPanel.Y + 14, 22, _textColor);
        float newMasterVolume = SynthRenderer.DrawSlider(
            index: 9,
            activeSlider: ref _activeSlider,
            enabled: true,
            label: "Master",
            valueLabel: $"{(_masterVolume * 100):0}%",
            bounds: masterVolumeBounds,
            value: _masterVolume,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: _accentColor,
            accentSoftColor: _accentSoftColor,
            borderColor: _borderColor,
            panelColor: _panelColor,
            textColor: _textColor,
            mutedTextColor: _mutedTextColor);

        if (MathF.Abs(newMasterVolume - _masterVolume) > 0.0001f)
        {
            _masterVolume = newMasterVolume;
            _synthEngine.SetMasterGain(_masterVolume);
        }

        SynthRenderer.DrawToggle(
            holdPedalBounds,
            "Hold pedal",
            _holdPedalEnabled,
            _accentColor,
            _accentSoftColor,
            _borderColor,
            _panelColor,
            _textColor,
            _mutedTextColor);
        SynthRenderer.DrawKeyboard(keys, _synthEngine.ActiveNotes, hoveredMidiNote, _whiteKeyColor, _borderColor, _darkKeyColor, _accentSoftColor, _accentStrongColor, _textColor);

        Graphics.EndDrawing();
    }

    private void UpdatePointerMidiNote(bool mousePressed, bool mouseDown, bool mouseReleased, int hoveredMidiNote)
    {
        if (mousePressed && hoveredMidiNote >= 0)
        {
            _activePointerMidiNote = hoveredMidiNote;
            return;
        }

        if (mouseDown && _activePointerMidiNote >= 0 && hoveredMidiNote >= 0)
        {
            _activePointerMidiNote = hoveredMidiNote;
            return;
        }

        if (mouseReleased)
        {
            _activePointerMidiNote = -1;
        }
    }

    private void SyncPhysicalNotes()
    {
        _currentPhysicalMidiNotes.Clear();

        foreach (int note in _currentKeyboardMidiNotes)
        {
            _currentPhysicalMidiNotes.Add(note);
        }

        if (_activePointerMidiNote >= 0)
        {
            _currentPhysicalMidiNotes.Add(_activePointerMidiNote);
        }

        _noteChangeBuffer.Clear();

        foreach (int note in _currentPhysicalMidiNotes)
        {
            if (!_previousPhysicalMidiNotes.Contains(note))
            {
                _noteChangeBuffer.Add(note);
            }
        }

        foreach (int note in _noteChangeBuffer)
        {
            _synthEngine.NoteOn(note, _synthParameters);
        }

        _noteChangeBuffer.Clear();

        foreach (int note in _previousPhysicalMidiNotes)
        {
            if (!_currentPhysicalMidiNotes.Contains(note))
            {
                _noteChangeBuffer.Add(note);
            }
        }

        foreach (int note in _noteChangeBuffer)
        {
            _synthEngine.NoteOff(note);
        }

        _previousPhysicalMidiNotes.Clear();

        foreach (int note in _currentPhysicalMidiNotes)
        {
            _previousPhysicalMidiNotes.Add(note);
        }
    }

    private static float GetLogNormalizedFrequency(float frequency, float minFrequency, float maxFrequency)
    {
        minFrequency = MathF.Max(minFrequency, 0.0001f);
        maxFrequency = MathF.Max(maxFrequency, minFrequency);
        frequency = Math.Clamp(frequency, minFrequency, maxFrequency);

        float minLog = MathF.Log(minFrequency);
        float maxLog = MathF.Log(maxFrequency);
        return (MathF.Log(frequency) - minLog) / (maxLog - minLog);
    }

    private static float GetFrequencyFromLogNormalized(float normalizedValue, float minFrequency, float maxFrequency)
    {
        minFrequency = MathF.Max(minFrequency, 0.0001f);
        maxFrequency = MathF.Max(maxFrequency, minFrequency);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        float minLog = MathF.Log(minFrequency);
        float maxLog = MathF.Log(maxFrequency);
        return MathF.Exp(minLog + ((maxLog - minLog) * normalizedValue));
    }

    private static void GetPressedKeyboardMidiNotes(HashSet<int> pressedNotes)
    {
        pressedNotes.Clear();

        foreach ((KeyboardKey key, int midiNote) in _computerKeyboardMappings)
        {
            if (Input.IsKeyDown(key))
            {
                pressedNotes.Add(midiNote);
            }
        }
    }
}

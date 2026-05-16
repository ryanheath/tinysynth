using Raylib_CSharp.Colors;
using Raylib_CSharp.Interact;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using Raylib_CSharp.Windowing;
using System.Numerics;
using TinySynth.App.Input;
using TinySynth.App.Services;
using TinySynth.Synth;
using TinySynth.UI;
using RaylibInput = Raylib_CSharp.Interact.Input;

namespace TinySynth.App;

internal enum ParameterSection
{
    Presets,
    Oscillator,
    Filter,
    Mod,
    Fx
}

internal sealed class TinySynthController : IDisposable
{
    private readonly int _keyboardStartMidi;
    private readonly int _keyboardNoteCount;
    private readonly float _panelGap;
    private readonly float _panelMargin;
    private readonly float _controlPanelHeight;
    private readonly float _keyboardPanelHeight;
    private readonly int _sampleRate;
    private readonly AudioStreamPump _audioStreamPump;
    private readonly SynthParameters _synthParameters;
    private readonly SynthEngine _synthEngine;
    private readonly IReadOnlyList<IInputDevice> _inputDevices;
    private readonly MidiController _midiController;
    private readonly InputActionProcessor _inputActionProcessor = new();
    private readonly List<InputAction> _inputActions = [];

    private readonly Color _textColor = UiTheme.TextColor;
    private readonly Color _mutedTextColor = UiTheme.MutedTextColor;
    private readonly Color _accentStrongColor = UiTheme.AccentStrongColor;
    private readonly Color _borderColor = UiTheme.BorderColor;

    private int _activeOscillatorIndex;
    private int _activePresetFamilyIndex;
    private int _activePresetIndex;
    private int _activeSlider = -1;
    private ParameterSection _activeParameterSection = ParameterSection.Presets;
    private float _masterVolume;

    public TinySynthController(
        AudioStreamPump audioStreamPump,
        int keyboardStartMidi,
        int keyboardNoteCount,
        int sampleRate,
        float masterGain,
        float panelGap,
        float panelMargin,
        float controlPanelHeight,
        float keyboardPanelHeight,
        IReadOnlyList<IInputDevice> inputDevices)
    {
        _audioStreamPump = audioStreamPump;
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
        _inputDevices = inputDevices;
        _midiController = new MidiController(_inputDevices);
    }

    public void Dispose()
    {
        _midiController.Dispose();
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

        Vector2 mousePosition = RaylibInput.GetMousePosition();
        bool mousePressed = RaylibInput.IsMouseButtonPressed(MouseButton.Left);
        bool mouseDown = RaylibInput.IsMouseButtonDown(MouseButton.Left);
        bool mouseReleased = RaylibInput.IsMouseButtonReleased(MouseButton.Left);

        if (!mouseDown)
        {
            _activeSlider = -1;
        }

        float sliderRowOneY = layout.SliderRowOneY;
        float sliderRowTwoY = layout.SliderRowTwoY;
        float sliderRowThreeY = layout.SliderRowThreeY;
        float sliderWidth = layout.SliderWidth;
        float fxSliderY = layout.FxSliderY;
        float fxSliderRowTwoY = layout.FxSliderRowTwoY;
        float fxSliderWidth = layout.FxSliderWidth;
        Rectangle holdPedalBounds = new(keyboardPanel.X + keyboardPanel.Width - 190, keyboardPanel.Y + 12, 192, 24);
        Rectangle masterVolumeBounds = new(keyboardPanel.X + keyboardPanel.Width - 400, keyboardPanel.Y + 12, 180, 20);

        PianoKeyLayout[] keys = KeyboardLayoutBuilder.Build(keyboardPanel, _keyboardStartMidi, _keyboardNoteCount);
        int hoveredMidiNote = KeyboardLayoutBuilder.GetHoveredMidiNote(keys, mousePosition);
        InputDeviceContext inputDeviceContext = new(mousePosition, mousePressed, mouseDown, mouseReleased, _inputActionProcessor.HoldPedalEnabled);
        _inputActions.Clear();

        foreach (IInputDevice inputDevice in _inputDevices)
        {
            if (inputDevice is IOnScreenInputDevice onScreenInputDevice)
            {
                onScreenInputDevice.SetLayout(keys, holdPedalBounds);
            }

            inputDevice.Update(inputDeviceContext, _inputActions);
        }

        _inputActionProcessor.ApplyActions(_inputActions, _synthEngine, _synthParameters);

        _audioStreamPump.Pump(_synthEngine, _synthParameters);

        Graphics.BeginDrawing();
        Graphics.ClearBackground(UiTheme.BackgroundColor);

        SynthRenderer.DrawPanel(controlPanel, UiTheme.PanelColor);
        SynthRenderer.DrawPanel(waveformPanel, UiTheme.PanelColor);
        SynthRenderer.DrawPanel(keyboardPanel, UiTheme.KeyboardPanelColor);

        Graphics.DrawText("TinySynth", (int)controlPanel.X + 20, (int)controlPanel.Y + 12, 28, UiTheme.TextColor);
        Graphics.DrawText("Parameters", (int)controlPanel.X + 20, (int)controlPanel.Y + 42, 18, UiTheme.MutedTextColor);

        ParameterSection previousParameterSection = _activeParameterSection;
        _activeParameterSection = (ParameterSection)SynthRenderer.DrawParameterSectionButtons(layout.ModeButtonsArea, (int)_activeParameterSection, mousePosition, mousePressed);

        if (previousParameterSection != _activeParameterSection)
        {
            _activeSlider = -1;
        }

        switch (_activeParameterSection)
        {
            case ParameterSection.Presets:
                {
                    PresetSectionResult presetSectionResult = PresetSectionController.Draw(
                        controlPanel,
                        layout,
                        _activePresetFamilyIndex,
                        _activePresetIndex,
                        mousePosition,
                        mousePressed,
                        _synthParameters);

                    _activePresetFamilyIndex = presetSectionResult.ActivePresetFamilyIndex;
                    _activePresetIndex = presetSectionResult.ActivePresetIndex;

                    if (presetSectionResult.PresetApplied)
                    {
                        _activeOscillatorIndex = 0;
                        _activeSlider = -1;
                    }

                    break;
                }

            case ParameterSection.Oscillator:
                {
                    OscillatorSectionResult oscillatorSectionResult = OscillatorSectionController.Draw(
                        controlPanel,
                        layout,
                        _activeOscillatorIndex,
                        ref _activeSlider,
                        sliderRowOneY,
                        sliderRowTwoY,
                        sliderRowThreeY,
                        sliderWidth,
                        mousePosition,
                        mousePressed,
                        mouseDown,
                        _synthParameters);

                    _activeOscillatorIndex = oscillatorSectionResult.ActiveOscillatorIndex;
                    break;
                }

            case ParameterSection.Filter:
                FilterSectionController.Draw(
                            controlPanel,
                            layout,
                            _audioStreamPump,
                            _sampleRate,
                            ref _activeSlider,
                            mousePosition,
                            mousePressed,
                            mouseDown,
                            _synthParameters);
                break;
            case ParameterSection.Mod:
                ModSectionController.Draw(
                        controlPanel,
                        layout,
                        ref _activeSlider,
                        mousePosition,
                        mousePressed,
                        mouseDown,
                        _synthParameters);
                break;
            default:
                FxSectionController.Draw(
                        controlPanel,
                        layout,
                        ref _activeSlider,
                        fxSliderY,
                        fxSliderRowTwoY,
                        fxSliderWidth,
                        mousePosition,
                        mousePressed,
                        mouseDown,
                        _synthParameters);
                break;
        }

        int displayMidiNote = _synthEngine.DisplayMidiNote;
        string? possibleChord = ChordUtilities.GetChordName(_synthEngine.ActiveNotes);
        string noteStatus = displayMidiNote >= 0
            ? $"Playing {MidiUtilities.MidiToNoteName(displayMidiNote)} | {_synthEngine.DisplayFrequency:0.0} Hz | {_synthEngine.ActiveVoiceCount} voices"
            : "Click the piano keys or use ZSXDCVGBHNJM, from C4 to C5.";
        Graphics.DrawText(noteStatus, (int)controlPanel.X + 470, (int)controlPanel.Y + 52, 20, _textColor);
        string envelopeStatus = possibleChord is null
            ? $"Envelope: {_synthEngine.DisplayEnvelopeStage}"
            : $"Envelope: {_synthEngine.DisplayEnvelopeStage} | Chord: {possibleChord}";
        Graphics.DrawText(envelopeStatus, (int)controlPanel.X + 470, (int)controlPanel.Y + 82, 18, _mutedTextColor);
        _midiController.DrawStatus((int)controlPanel.X + 470, (int)controlPanel.Y + 112, _mutedTextColor);

        SynthRenderer.DrawWaveformScope(waveformPanel, _audioStreamPump.ScopeBuffer, _audioStreamPump.ScopeWriteIndex);
        float newMasterVolume = KeyboardController.Draw(
            keyboardPanel,
            holdPedalBounds,
            masterVolumeBounds,
            _synthEngine.ActiveNotes,
            hoveredMidiNote,
            _inputActionProcessor.HoldPedalEnabled,
            keys,
            ref _activeSlider,
            _masterVolume,
            mousePosition,
            mousePressed,
            mouseDown);

        if (MathF.Abs(newMasterVolume - _masterVolume) > 0.0001f)
        {
            _masterVolume = newMasterVolume;
            _synthEngine.SetMasterGain(_masterVolume);
        }
        Graphics.EndDrawing();
    }
}

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

internal sealed class TinySynthController
{
    private readonly int _keyboardStartMidi;
    private readonly int _keyboardNoteCount;
    private readonly float _panelGap;
    private readonly float _panelMargin;
    private readonly float _controlPanelHeight;
    private readonly float _keyboardPanelHeight;
    private readonly AudioStream _audioStream;
    private readonly IntPtr _audioBufferPointer;
    private readonly float[] _audioBuffer;
    private readonly float[] _scopeBuffer;
    private readonly SynthParameters _synthParameters;
    private readonly SynthVoice _synthVoice;

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
    private int _activeSlider = -1;
    private int _scopeWriteIndex;

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
        _synthParameters = new SynthParameters();
        _synthVoice = new SynthVoice(sampleRate, masterGain, keyboardStartMidi);
    }

    public void RunFrame()
    {
        int currentScreenWidth = Window.GetScreenWidth();
        int currentScreenHeight = Window.GetScreenHeight();
        LayoutMetrics layout = LayoutCalculator.Calculate(currentScreenWidth, currentScreenHeight, _panelMargin, _panelGap, _controlPanelHeight, _keyboardPanelHeight);
        Rectangle controlPanel = layout.ControlPanel;
        Rectangle waveformPanel = layout.WaveformPanel;
        Rectangle keyboardPanel = layout.KeyboardPanel;

        Vector2 mousePosition = Input.GetMousePosition();
        bool mousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
        bool mouseDown = Input.IsMouseButtonDown(MouseButton.Left);
        bool mouseReleased = Input.IsMouseButtonReleased(MouseButton.Left);

        if (!mouseDown)
        {
            _activeSlider = -1;
        }

        float sliderY = layout.SliderY;
        float sliderWidth = layout.SliderWidth;

        PianoKeyLayout[] keys = KeyboardLayoutBuilder.Build(keyboardPanel, _keyboardStartMidi, _keyboardNoteCount);
        int hoveredMidiNote = KeyboardLayoutBuilder.GetHoveredMidiNote(keys, mousePosition);

        if (mousePressed && hoveredMidiNote >= 0)
        {
            _activePointerMidiNote = hoveredMidiNote;
            _synthVoice.StartNote(hoveredMidiNote, _synthParameters);
        }
        else if (mouseDown && _activePointerMidiNote >= 0 && hoveredMidiNote >= 0 && hoveredMidiNote != _activePointerMidiNote)
        {
            _activePointerMidiNote = hoveredMidiNote;
            _synthVoice.StartNote(hoveredMidiNote, _synthParameters);
        }

        if (mouseReleased && _activePointerMidiNote >= 0)
        {
            _synthVoice.ReleaseNote();
            _activePointerMidiNote = -1;
        }

        while (_audioStream.IsProcessed())
        {
            _synthVoice.FillBuffer(_audioBuffer, _scopeBuffer, ref _scopeWriteIndex, _synthParameters);
            Marshal.Copy(_audioBuffer, 0, _audioBufferPointer, _audioBuffer.Length);
            _audioStream.Update(_audioBufferPointer, _audioBuffer.Length);
        }

        Graphics.BeginDrawing();
        Graphics.ClearBackground(_backgroundColor);

        SynthRenderer.DrawPanel(controlPanel, _panelColor, _borderColor);
        SynthRenderer.DrawPanel(waveformPanel, _panelColor, _borderColor);
        SynthRenderer.DrawPanel(keyboardPanel, _keyboardPanelColor, _borderColor);

        Graphics.DrawText("TinySynth", (int)controlPanel.X + 20, (int)controlPanel.Y + 12, 28, _textColor);
        Graphics.DrawText("Base waveform", (int)controlPanel.X + 20, (int)controlPanel.Y + 82, 18, _mutedTextColor);

        _synthParameters.Waveform = SynthRenderer.DrawWaveformButtons(layout.WaveformButtonsArea, _synthParameters.Waveform, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

        _synthParameters.AttackSeconds = SynthRenderer.DrawSlider(
            index: 0,
            activeSlider: ref _activeSlider,
            label: "Attack",
            valueLabel: $"{_synthParameters.AttackSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderY, sliderWidth, 20),
            value: _synthParameters.AttackSeconds,
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

        _synthParameters.DecaySeconds = SynthRenderer.DrawSlider(
            index: 1,
            activeSlider: ref _activeSlider,
            label: "Decay",
            valueLabel: $"{_synthParameters.DecaySeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderY, sliderWidth, 20),
            value: _synthParameters.DecaySeconds,
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

        _synthParameters.SustainLevel = SynthRenderer.DrawSlider(
            index: 2,
            activeSlider: ref _activeSlider,
            label: "Sustain",
            valueLabel: $"{_synthParameters.SustainLevel:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderY, sliderWidth, 20),
            value: _synthParameters.SustainLevel,
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

        _synthParameters.ReleaseSeconds = SynthRenderer.DrawSlider(
            index: 3,
            activeSlider: ref _activeSlider,
            label: "Release",
            valueLabel: $"{_synthParameters.ReleaseSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderY, sliderWidth, 20),
            value: _synthParameters.ReleaseSeconds,
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

        string noteStatus = _synthVoice.ActiveMidiNote >= 0
            ? $"Playing {MidiUtilities.MidiToNoteName(_synthVoice.ActiveMidiNote)}  •  {_synthVoice.CurrentFrequency:0.0} Hz  •  {_synthParameters.Waveform}"
            : "Click and hold the piano keys to play the synth.";
        Graphics.DrawText(noteStatus, (int)controlPanel.X + 410, (int)controlPanel.Y + 52, 20, _textColor);
        Graphics.DrawText($"Envelope: {_synthVoice.EnvelopeStage}", (int)controlPanel.X + 410, (int)controlPanel.Y + 82, 18, _mutedTextColor);

        SynthRenderer.DrawWaveformScope(waveformPanel, _scopeBuffer, _scopeWriteIndex, _accentStrongColor, _borderColor, _mutedTextColor);
        Graphics.DrawText("Keyboard", (int)keyboardPanel.X + 18, (int)keyboardPanel.Y + 14, 22, _textColor);
        SynthRenderer.DrawKeyboard(keys, _synthVoice.ActiveMidiNote, hoveredMidiNote, _whiteKeyColor, _borderColor, _darkKeyColor, _accentSoftColor, _accentStrongColor, _textColor);

        Graphics.EndDrawing();
    }
}

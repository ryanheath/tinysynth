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
    private int _activeSlider = -1;
    private int _scopeWriteIndex;
    private bool _holdPedalEnabled;

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
        _synthEngine = new SynthEngine(sampleRate, masterGain, keyboardStartMidi, voiceCount: 4);
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
        bool holdPedalTogglePressed = Input.IsKeyPressed(KeyboardKey.Space);

        if (!mouseDown)
        {
            _activeSlider = -1;
        }

        float sliderRowOneY = layout.SliderRowOneY;
        float sliderRowTwoY = layout.SliderRowTwoY;
        float sliderWidth = layout.SliderWidth;
        Rectangle holdPedalBounds = new(keyboardPanel.X + keyboardPanel.Width - 158, keyboardPanel.Y + 12, 126, 24);

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
        Graphics.DrawText("Base waveform", (int)controlPanel.X + 20, (int)controlPanel.Y + 82, 18, _mutedTextColor);

        _synthParameters.Waveform = SynthRenderer.DrawWaveformButtons(layout.WaveformButtonsArea, _synthParameters.Waveform, mousePosition, mousePressed, _panelColor, _borderColor, _accentSoftColor, _accentStrongColor, _textColor);

        _synthParameters.Gain = SynthRenderer.DrawSlider(
            index: 0,
            activeSlider: ref _activeSlider,
            label: "Gain",
            valueLabel: $"{_synthParameters.Gain:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowOneY, sliderWidth, 20),
            value: _synthParameters.Gain,
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

        _synthParameters.DetuneCents = SynthRenderer.DrawSlider(
            index: 1,
            activeSlider: ref _activeSlider,
            label: "Detune",
            valueLabel: $"{_synthParameters.DetuneCents:0} ct",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowOneY, sliderWidth, 20),
            value: _synthParameters.DetuneCents,
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

        _synthParameters.GlideSeconds = SynthRenderer.DrawSlider(
            index: 2,
            activeSlider: ref _activeSlider,
            label: "Glide",
            valueLabel: $"{_synthParameters.GlideSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderRowOneY, sliderWidth, 20),
            value: _synthParameters.GlideSeconds,
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

        _synthParameters.VibratoDepthCents = SynthRenderer.DrawSlider(
            index: 3,
            activeSlider: ref _activeSlider,
            label: "Vib depth",
            valueLabel: $"{_synthParameters.VibratoDepthCents:0} ct",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderRowOneY, sliderWidth, 20),
            value: _synthParameters.VibratoDepthCents,
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

        _synthParameters.VibratoRateHz = SynthRenderer.DrawSlider(
            index: 4,
            activeSlider: ref _activeSlider,
            label: "Vib rate",
            valueLabel: $"{_synthParameters.VibratoRateHz:0.0}Hz",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 4), sliderRowOneY, sliderWidth, 20),
            value: _synthParameters.VibratoRateHz,
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

        _synthParameters.AttackSeconds = SynthRenderer.DrawSlider(
            index: 5,
            activeSlider: ref _activeSlider,
            label: "Attack",
            valueLabel: $"{_synthParameters.AttackSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowTwoY, sliderWidth, 20),
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
            index: 6,
            activeSlider: ref _activeSlider,
            label: "Decay",
            valueLabel: $"{_synthParameters.DecaySeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowTwoY, sliderWidth, 20),
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
            index: 7,
            activeSlider: ref _activeSlider,
            label: "Sustain",
            valueLabel: $"{_synthParameters.SustainLevel:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderRowTwoY, sliderWidth, 20),
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
            index: 8,
            activeSlider: ref _activeSlider,
            label: "Release",
            valueLabel: $"{_synthParameters.ReleaseSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderRowTwoY, sliderWidth, 20),
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

        int displayMidiNote = _synthEngine.DisplayMidiNote;
        string noteStatus = displayMidiNote >= 0
            ? $"Playing {MidiUtilities.MidiToNoteName(displayMidiNote)}  •  {_synthEngine.DisplayFrequency:0.0} Hz  •  {_synthEngine.ActiveVoiceCount} voices"
            : "Click the piano keys or use ZSXDCVGBHNJM, from C4 to C5.";
        Graphics.DrawText(noteStatus, (int)controlPanel.X + 410, (int)controlPanel.Y + 52, 20, _textColor);
        Graphics.DrawText($"Envelope: {_synthEngine.DisplayEnvelopeStage}", (int)controlPanel.X + 410, (int)controlPanel.Y + 82, 18, _mutedTextColor);

        SynthRenderer.DrawWaveformScope(waveformPanel, _scopeBuffer, _scopeWriteIndex, _accentStrongColor, _borderColor, _mutedTextColor);
        Graphics.DrawText("Keyboard", (int)keyboardPanel.X + 18, (int)keyboardPanel.Y + 14, 22, _textColor);
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

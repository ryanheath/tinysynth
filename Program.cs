using Raylib_CSharp.Audio;
using Raylib_CSharp.Colors;
using Raylib_CSharp.Interact;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using Raylib_CSharp.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using static Raylib_CSharp.Time;

const int screenWidth = 1200;
const int screenHeight = 720;
const int sampleRate = 44100;
const int audioBufferSize = 512;
const int keyboardStartMidi = 60;
const int keyboardNoteCount = 24;
const float masterGain = 0.22f;
const float panelGap = 20f;
const float panelMargin = 20f;
const float controlPanelHeight = 170f;
const float keyboardPanelHeight = 180f;

Color backgroundColor = new(242, 245, 250, 255);
Color panelColor = new(252, 253, 255, 255);
Color keyboardPanelColor = new(233, 238, 247, 255);
Color borderColor = new(208, 214, 224, 255);
Color textColor = new(52, 60, 76, 255);
Color mutedTextColor = new(105, 114, 132, 255);
Color accentColor = new(84, 146, 255, 255);
Color accentStrongColor = new(47, 111, 237, 255);
Color accentSoftColor = new(213, 231, 255, 255);
Color whiteKeyColor = new(255, 255, 255, 255);
Color darkKeyColor = new(40, 46, 60, 255);

Waveform waveform = Waveform.Sine;
float attackSeconds = 0.05f;
float decaySeconds = 0.18f;
float sustainLevel = 0.72f;
float releaseSeconds = 0.30f;

EnvelopeStage envelopeStage = EnvelopeStage.Idle;
float envelopeLevel = 0f;
float releaseStartLevel = 0f;
float releaseElapsed = 0f;
float oscillatorPhase = 0f;
float currentFrequency = MidiToFrequency(keyboardStartMidi);
int activeMidiNote = -1;
int activePointerMidiNote = -1;
int activeSlider = -1;

float[] audioBuffer = new float[audioBufferSize];
float[] scopeBuffer = new float[2048];
int scopeWriteIndex = 0;
IntPtr audioBufferPointer = IntPtr.Zero;

Raylib_CSharp.Raylib.SetConfigFlags(ConfigFlags.HighDpiWindow);
Window.Init(screenWidth, screenHeight, "TinySynth");
SetTargetFPS(60);

AudioDevice.Init();
AudioStream.SetBufferSizeDefault(audioBufferSize);
AudioStream audioStream = AudioStream.Load(sampleRate, 32, 1);
audioStream.Play();
audioBufferPointer = Marshal.AllocHGlobal(audioBuffer.Length * sizeof(float));

try
{
    while (!Window.ShouldClose())
    {
        int currentScreenWidth = Window.GetScreenWidth();
        int currentScreenHeight = Window.GetScreenHeight();

        float availableHeight = currentScreenHeight - (panelMargin * 2);
        float minControlHeight = 140f;
        float minWaveformHeight = 120f;
        float minKeyboardHeight = 120f;
        float extraHeight = MathF.Max(0f, availableHeight - (minControlHeight + minWaveformHeight + minKeyboardHeight + (panelGap * 2)));

        float adaptiveControlHeight = MathF.Min(controlPanelHeight, minControlHeight + (extraHeight * 0.30f));
        float adaptiveKeyboardHeight = MathF.Min(keyboardPanelHeight, minKeyboardHeight + (extraHeight * 0.25f));
        float adaptiveWaveformHeight = MathF.Max(minWaveformHeight, availableHeight - adaptiveControlHeight - adaptiveKeyboardHeight - (panelGap * 2));

        Rectangle controlPanel = new(panelMargin, panelMargin, currentScreenWidth - (panelMargin * 2), adaptiveControlHeight);
        Rectangle waveformPanel = new(panelMargin, controlPanel.Y + controlPanel.Height + panelGap, currentScreenWidth - (panelMargin * 2), adaptiveWaveformHeight);
        Rectangle keyboardPanel = new(panelMargin, waveformPanel.Y + waveformPanel.Height + panelGap, currentScreenWidth - (panelMargin * 2), adaptiveKeyboardHeight);

        Vector2 mousePosition = Input.GetMousePosition();
        bool mousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
        bool mouseDown = Input.IsMouseButtonDown(MouseButton.Left);
        bool mouseReleased = Input.IsMouseButtonReleased(MouseButton.Left);

        if (!mouseDown)
        {
            activeSlider = -1;
        }

        float sliderY = controlPanel.Y + 106;
        float sliderWidth = (controlPanel.Width - 40 - (18 * 3)) / 4f;

        PianoKeyLayout[] keys = BuildKeyboardLayout(keyboardPanel, keyboardStartMidi, keyboardNoteCount);
        int hoveredMidiNote = GetHoveredMidiNote(keys, mousePosition);

        if (mousePressed && hoveredMidiNote >= 0)
        {
            activePointerMidiNote = hoveredMidiNote;
            StartNote(hoveredMidiNote);
        }
        else if (mouseDown && activePointerMidiNote >= 0 && hoveredMidiNote >= 0 && hoveredMidiNote != activePointerMidiNote)
        {
            activePointerMidiNote = hoveredMidiNote;
            StartNote(hoveredMidiNote);
        }

        if (mouseReleased && activePointerMidiNote >= 0)
        {
            ReleaseNote();
            activePointerMidiNote = -1;
        }

        while (audioStream.IsProcessed())
        {
            FillAudioBuffer(audioBuffer);
            Marshal.Copy(audioBuffer, 0, audioBufferPointer, audioBuffer.Length);
            audioStream.Update(audioBufferPointer, audioBuffer.Length);
        }

        Graphics.BeginDrawing();
        Graphics.ClearBackground(backgroundColor);

        DrawPanel(controlPanel, panelColor, borderColor);
        DrawPanel(waveformPanel, panelColor, borderColor);
        DrawPanel(keyboardPanel, keyboardPanelColor, borderColor);

        Graphics.DrawText("TinySynth", (int)controlPanel.X + 20, (int)controlPanel.Y + 12, 28, textColor);
        Graphics.DrawText("Base waveform", (int)controlPanel.X + 20, (int)controlPanel.Y + 82, 18, mutedTextColor);

        Rectangle waveformButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 42, 360, 36);
        waveform = DrawWaveformButtons(waveformButtonsArea, waveform, mousePosition, mousePressed, panelColor, borderColor, accentSoftColor, accentStrongColor, textColor);

        attackSeconds = DrawSlider(
            index: 0,
            label: "Attack",
            valueLabel: $"{attackSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderY, sliderWidth, 20),
            value: attackSeconds,
            minValue: 0.01f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: accentColor,
            accentSoftColor: accentSoftColor,
            borderColor: borderColor,
            panelColor: panelColor,
            textColor: textColor,
            mutedTextColor: mutedTextColor);

        decaySeconds = DrawSlider(
            index: 1,
            label: "Decay",
            valueLabel: $"{decaySeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderY, sliderWidth, 20),
            value: decaySeconds,
            minValue: 0.01f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: accentColor,
            accentSoftColor: accentSoftColor,
            borderColor: borderColor,
            panelColor: panelColor,
            textColor: textColor,
            mutedTextColor: mutedTextColor);

        sustainLevel = DrawSlider(
            index: 2,
            label: "Sustain",
            valueLabel: $"{sustainLevel:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderY, sliderWidth, 20),
            value: sustainLevel,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: accentColor,
            accentSoftColor: accentSoftColor,
            borderColor: borderColor,
            panelColor: panelColor,
            textColor: textColor,
            mutedTextColor: mutedTextColor);

        releaseSeconds = DrawSlider(
            index: 3,
            label: "Release",
            valueLabel: $"{releaseSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderY, sliderWidth, 20),
            value: releaseSeconds,
            minValue: 0.01f,
            maxValue: 2.50f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown,
            accentColor: accentColor,
            accentSoftColor: accentSoftColor,
            borderColor: borderColor,
            panelColor: panelColor,
            textColor: textColor,
            mutedTextColor: mutedTextColor);

        string noteStatus = activeMidiNote >= 0
            ? $"Playing {MidiToNoteName(activeMidiNote)}  •  {currentFrequency:0.0} Hz  •  {waveform}"
            : "Click and hold the piano keys to play the synth.";
        Graphics.DrawText(noteStatus, (int)controlPanel.X + 410, (int)controlPanel.Y + 52, 20, textColor);
        Graphics.DrawText($"Envelope: {envelopeStage}", (int)controlPanel.X + 410, (int)controlPanel.Y + 82, 18, mutedTextColor);

        DrawWaveformScope(waveformPanel, scopeBuffer, scopeWriteIndex, accentStrongColor, borderColor, mutedTextColor);
        Graphics.DrawText("Keyboard", (int)keyboardPanel.X + 18, (int)keyboardPanel.Y + 14, 22, textColor);
        DrawKeyboard(keys, activeMidiNote, hoveredMidiNote, whiteKeyColor, borderColor, darkKeyColor, accentSoftColor, accentStrongColor, textColor);

        Graphics.EndDrawing();
    }
}
finally
{
    if (audioBufferPointer != IntPtr.Zero)
    {
        Marshal.FreeHGlobal(audioBufferPointer);
    }

    audioStream.Stop();
    audioStream.Unload();
    AudioDevice.Close();
    Window.Close();
}

void StartNote(int midiNote)
{
    activeMidiNote = midiNote;
    currentFrequency = MidiToFrequency(midiNote);
    oscillatorPhase = 0f;
    envelopeLevel = 0f;
    releaseElapsed = 0f;
    releaseStartLevel = 0f;
    envelopeStage = attackSeconds <= 0.01f ? EnvelopeStage.Decay : EnvelopeStage.Attack;

    if (envelopeStage == EnvelopeStage.Decay)
    {
        envelopeLevel = 1f;
    }
}

void ReleaseNote()
{
    if (activeMidiNote < 0 || envelopeStage == EnvelopeStage.Idle)
    {
        return;
    }

    releaseStartLevel = envelopeLevel;
    releaseElapsed = 0f;
    envelopeStage = EnvelopeStage.Release;
}

void FillAudioBuffer(float[] buffer)
{
    for (int i = 0; i < buffer.Length; i++)
    {
        float sample = NextSample();
        buffer[i] = sample;
        scopeBuffer[scopeWriteIndex] = sample;
        scopeWriteIndex = (scopeWriteIndex + 1) % scopeBuffer.Length;
    }
}

float NextSample()
{
    float deltaTime = 1f / sampleRate;
    UpdateEnvelope(deltaTime);

    if (envelopeStage == EnvelopeStage.Idle || activeMidiNote < 0)
    {
        return 0f;
    }

    float sample = waveform switch
    {
        Waveform.Sine => MathF.Sin(oscillatorPhase * MathF.Tau),
        Waveform.Square => oscillatorPhase < 0.5f ? 1f : -1f,
        Waveform.Saw => (2f * oscillatorPhase) - 1f,
        Waveform.Triangle => 1f - (4f * MathF.Abs(oscillatorPhase - 0.5f)),
        _ => 0f
    };

    oscillatorPhase += currentFrequency / sampleRate;
    oscillatorPhase -= MathF.Floor(oscillatorPhase);

    return sample * envelopeLevel * masterGain;
}

void UpdateEnvelope(float deltaTime)
{
    switch (envelopeStage)
    {
        case EnvelopeStage.Idle:
            envelopeLevel = 0f;
            break;

        case EnvelopeStage.Attack:
            envelopeLevel += deltaTime / MathF.Max(attackSeconds, 0.0001f);
            if (envelopeLevel >= 1f)
            {
                envelopeLevel = 1f;
                envelopeStage = EnvelopeStage.Decay;
            }
            break;

        case EnvelopeStage.Decay:
            if (decaySeconds <= 0.01f)
            {
                envelopeLevel = sustainLevel;
                envelopeStage = EnvelopeStage.Sustain;
            }
            else
            {
                envelopeLevel -= ((1f - sustainLevel) / decaySeconds) * deltaTime;
                if (envelopeLevel <= sustainLevel)
                {
                    envelopeLevel = sustainLevel;
                    envelopeStage = EnvelopeStage.Sustain;
                }
            }
            break;

        case EnvelopeStage.Sustain:
            envelopeLevel = sustainLevel;
            break;

        case EnvelopeStage.Release:
            releaseElapsed += deltaTime;
            if (releaseSeconds <= 0.01f || releaseElapsed >= releaseSeconds)
            {
                envelopeLevel = 0f;
                activeMidiNote = -1;
                envelopeStage = EnvelopeStage.Idle;
            }
            else
            {
                float releaseProgress = releaseElapsed / releaseSeconds;
                envelopeLevel = releaseStartLevel * (1f - releaseProgress);
            }
            break;
    }
}

Waveform DrawWaveformButtons(
    Rectangle area,
    Waveform currentValue,
    Vector2 mousePosition,
    bool mousePressed,
    Color panelColor,
    Color borderColor,
    Color selectedColor,
    Color selectedBorderColor,
    Color textColor)
{
    Waveform[] waveforms = [Waveform.Sine, Waveform.Square, Waveform.Saw, Waveform.Triangle];
    float buttonGap = 10f;
    float buttonWidth = (area.Width - (buttonGap * (waveforms.Length - 1))) / waveforms.Length;

    for (int i = 0; i < waveforms.Length; i++)
    {
        Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
        bool isSelected = currentValue == waveforms[i];
        bool isHovered = Contains(buttonBounds, mousePosition);
        Color fill = isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor);
        Color outline = isSelected ? selectedBorderColor : borderColor;

        Graphics.DrawRectangleRec(buttonBounds, fill);
        Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
        Graphics.DrawText(waveforms[i].ToString(), (int)buttonBounds.X + 18, (int)buttonBounds.Y + 8, 18, textColor);

        if (mousePressed && isHovered)
        {
            currentValue = waveforms[i];
        }
    }

    return currentValue;
}

float DrawSlider(
    int index,
    string label,
    string valueLabel,
    Rectangle bounds,
    float value,
    float minValue,
    float maxValue,
    Vector2 mousePosition,
    bool mousePressed,
    bool mouseDown,
    Color accentColor,
    Color accentSoftColor,
    Color borderColor,
    Color panelColor,
    Color textColor,
    Color mutedTextColor)
{
    Rectangle trackBounds = new(bounds.X, bounds.Y + 22, bounds.Width, bounds.Height);

    if (mousePressed && Contains(trackBounds, mousePosition))
    {
        activeSlider = index;
    }

    if (mouseDown && activeSlider == index)
    {
        float normalized = Math.Clamp((mousePosition.X - trackBounds.X) / trackBounds.Width, 0f, 1f);
        value = minValue + ((maxValue - minValue) * normalized);
    }

    float ratio = (value - minValue) / (maxValue - minValue);
    Rectangle fillBounds = new(trackBounds.X, trackBounds.Y, trackBounds.Width * ratio, trackBounds.Height);
    Rectangle thumbBounds = new(trackBounds.X + (trackBounds.Width * ratio) - 7f, trackBounds.Y - 4f, 14, trackBounds.Height + 8f);

    Graphics.DrawText(label, (int)bounds.X, (int)bounds.Y, 18, textColor);
    Graphics.DrawText(valueLabel, (int)(bounds.X + bounds.Width - 60), (int)bounds.Y, 18, mutedTextColor);
    Graphics.DrawRectangleRec(trackBounds, panelColor);
    Graphics.DrawRectangleLinesEx(trackBounds, 1f, borderColor);
    Graphics.DrawRectangleRec(fillBounds, accentSoftColor);
    Graphics.DrawRectangleLinesEx(fillBounds, 0f, accentSoftColor);
    Graphics.DrawRectangleRec(thumbBounds, accentColor);

    return Math.Clamp(value, minValue, maxValue);
}

void DrawWaveformScope(Rectangle bounds, float[] samples, int writeIndex, Color waveColor, Color borderColor, Color labelColor)
{
    Graphics.DrawText("Output waveform", (int)bounds.X + 18, (int)bounds.Y + 16, 22, labelColor);
    Graphics.DrawText("Recent audio samples from the active synth voice", (int)bounds.X + 18, (int)bounds.Y + 46, 18, labelColor);

    Rectangle graphBounds = new(bounds.X + 18, bounds.Y + 82, bounds.Width - 36, bounds.Height - 100);
    Graphics.DrawRectangleRec(graphBounds, new Color(246, 249, 255, 255));
    Graphics.DrawRectangleLinesEx(graphBounds, 1f, borderColor);

    Vector2 centerStart = new(graphBounds.X, graphBounds.Y + (graphBounds.Height / 2f));
    Vector2 centerEnd = new(graphBounds.X + graphBounds.Width, graphBounds.Y + (graphBounds.Height / 2f));
    Graphics.DrawLineV(centerStart, centerEnd, new Color(215, 223, 237, 255));

    int sampleCount = samples.Length;
    float xStep = graphBounds.Width / (sampleCount - 1);
    float centerY = graphBounds.Y + (graphBounds.Height / 2f);
    float amplitude = graphBounds.Height * 0.42f;

    Vector2 previous = new(graphBounds.X, centerY - (samples[writeIndex] * amplitude));

    for (int i = 1; i < sampleCount; i++)
    {
        int sampleIndex = (writeIndex + i) % sampleCount;
        Vector2 current = new(graphBounds.X + (xStep * i), centerY - (samples[sampleIndex] * amplitude));
        Graphics.DrawLineV(previous, current, waveColor);
        previous = current;
    }
}

void DrawKeyboard(
    PianoKeyLayout[] keys,
    int activeNote,
    int hoveredNote,
    Color whiteKeyColor,
    Color borderColor,
    Color blackKeyColor,
    Color activeWhiteKeyColor,
    Color activeBlackKeyColor,
    Color textColor)
{
    foreach (PianoKeyLayout key in keys.Where(static key => !key.IsBlack))
    {
        bool isActive = key.MidiNote == activeNote;
        bool isHovered = key.MidiNote == hoveredNote;
        Color fill = isActive ? activeWhiteKeyColor : (isHovered ? new Color(242, 247, 255, 255) : whiteKeyColor);

        Graphics.DrawRectangleRec(key.Bounds, fill);
        Graphics.DrawRectangleLinesEx(key.Bounds, 1f, borderColor);

        if (key.MidiNote % 12 == 0)
        {
            Graphics.DrawText(key.Label, (int)key.Bounds.X + 10, (int)(key.Bounds.Y + key.Bounds.Height - 28), 18, textColor);
        }
    }

    foreach (PianoKeyLayout key in keys.Where(static key => key.IsBlack))
    {
        bool isActive = key.MidiNote == activeNote;
        bool isHovered = key.MidiNote == hoveredNote;
        Color fill = isActive ? activeBlackKeyColor : (isHovered ? new Color(70, 78, 99, 255) : blackKeyColor);

        Graphics.DrawRectangleRec(key.Bounds, fill);
        Graphics.DrawRectangleLinesEx(key.Bounds, 1f, new Color(18, 22, 30, 255));
    }
}

void DrawPanel(Rectangle bounds, Color fillColor, Color outlineColor)
{
    Graphics.DrawRectangleRec(bounds, fillColor);
    Graphics.DrawRectangleLinesEx(bounds, 1f, outlineColor);
}

PianoKeyLayout[] BuildKeyboardLayout(Rectangle bounds, int startingMidi, int noteCount)
{
    int whiteKeyCount = 0;

    for (int i = 0; i < noteCount; i++)
    {
        if (IsWhiteKey(startingMidi + i))
        {
            whiteKeyCount++;
        }
    }

    float topPadding = 46f;
    float bottomPadding = 10f;
    float whiteKeyWidth = bounds.Width / whiteKeyCount;
    float whiteKeyHeight = MathF.Max(52f, bounds.Height - topPadding - bottomPadding);
    float blackKeyWidth = whiteKeyWidth * 0.62f;
    float blackKeyHeight = whiteKeyHeight * 0.58f;
    float currentX = bounds.X;

    List<PianoKeyLayout> whiteKeys = [];
    List<PianoKeyLayout> blackKeys = [];
    Rectangle previousWhiteBounds = default;
    bool hasPreviousWhite = false;

    for (int i = 0; i < noteCount; i++)
    {
        int midiNote = startingMidi + i;
        string label = MidiToNoteName(midiNote);

        if (IsWhiteKey(midiNote))
        {
            Rectangle keyBounds = new(currentX, bounds.Y + topPadding, whiteKeyWidth, whiteKeyHeight);
            whiteKeys.Add(new PianoKeyLayout(midiNote, false, keyBounds, label));
            previousWhiteBounds = keyBounds;
            hasPreviousWhite = true;
            currentX += whiteKeyWidth;
        }
        else if (hasPreviousWhite)
        {
            Rectangle keyBounds = new(currentX - (blackKeyWidth / 2f), bounds.Y + topPadding, blackKeyWidth, blackKeyHeight);
            blackKeys.Add(new PianoKeyLayout(midiNote, true, keyBounds, label));
        }
    }

    return [.. whiteKeys, .. blackKeys];
}

int GetHoveredMidiNote(PianoKeyLayout[] keys, Vector2 mousePosition)
{
    foreach (PianoKeyLayout key in keys.Where(static key => key.IsBlack))
    {
        if (Contains(key.Bounds, mousePosition))
        {
            return key.MidiNote;
        }
    }

    foreach (PianoKeyLayout key in keys.Where(static key => !key.IsBlack))
    {
        if (Contains(key.Bounds, mousePosition))
        {
            return key.MidiNote;
        }
    }

    return -1;
}

bool Contains(Rectangle bounds, Vector2 point)
{
    return point.X >= bounds.X
        && point.X <= bounds.X + bounds.Width
        && point.Y >= bounds.Y
        && point.Y <= bounds.Y + bounds.Height;
}

bool IsWhiteKey(int midiNote)
{
    return midiNote % 12 is 0 or 2 or 4 or 5 or 7 or 9 or 11;
}

float MidiToFrequency(int midiNote)
{
    return 440f * MathF.Pow(2f, (midiNote - 69) / 12f);
}

string MidiToNoteName(int midiNote)
{
    string[] noteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    int octave = (midiNote / 12) - 1;
    return $"{noteNames[midiNote % 12]}{octave}";
}

enum Waveform
{
    Sine,
    Square,
    Saw,
    Triangle
}

enum EnvelopeStage
{
    Idle,
    Attack,
    Decay,
    Sustain,
    Release
}

readonly record struct PianoKeyLayout(int MidiNote, bool IsBlack, Rectangle Bounds, string Label);

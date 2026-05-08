using Raylib_CSharp.Audio;
using Raylib_CSharp.Windowing;
using System.Runtime.InteropServices;
using TinySynth.App;
using TinySynth.App.Input;
using static Raylib_CSharp.Time;

const int screenWidth = 1440;
const int screenHeight = 860;
const int sampleRate = 44100;
const int audioBufferFrameCount = 2048;
const int audioChannelCount = 2;
const int keyboardStartMidi = 21;
const int keyboardNoteCount = 88;
const float masterGain = 0.22f;
const float panelGap = 20f;
const float panelMargin = 20f;
const float controlPanelHeight = 420f;
const float keyboardPanelHeight = 180f;

float[] audioBuffer = new float[audioBufferFrameCount * audioChannelCount];
IntPtr audioBufferPointer = IntPtr.Zero;

Raylib_CSharp.Raylib.SetConfigFlags(ConfigFlags.HighDpiWindow);
Window.Init(screenWidth, screenHeight, "TinySynth");
SetTargetFPS(60);

AudioDevice.Init();
AudioStream.SetBufferSizeDefault(audioBufferFrameCount);
AudioStream audioStream = AudioStream.Load(sampleRate, 32, audioChannelCount);
audioStream.Play();
audioBufferPointer = Marshal.AllocHGlobal(audioBuffer.Length * sizeof(float));
IReadOnlyList<IInputDevice> inputDevices =
[
    new ComputerKeyboardInputDevice(),
    new OnScreenKeyboardInputDevice()
];

TinySynthController app = new(
    audioStream,
    audioBufferPointer,
    audioBuffer,
    keyboardStartMidi,
    keyboardNoteCount,
    sampleRate,
    masterGain,
    panelGap,
    panelMargin,
    controlPanelHeight,
    keyboardPanelHeight,
    inputDevices);

try
{
    while (!Window.ShouldClose())
    {
        app.RunFrame();
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

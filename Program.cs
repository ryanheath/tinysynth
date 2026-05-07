using Raylib_CSharp.Audio;
using Raylib_CSharp.Windowing;
using System.Runtime.InteropServices;
using TinySynth.App;
using static Raylib_CSharp.Time;

const int screenWidth = 1200;
const int screenHeight = 720;
const int sampleRate = 44100;
const int audioBufferSize = 2048;
const int keyboardStartMidi = 48;
const int keyboardNoteCount = 49;
const float masterGain = 0.22f;
const float panelGap = 20f;
const float panelMargin = 20f;
const float controlPanelHeight = 235f;
const float keyboardPanelHeight = 180f;

float[] audioBuffer = new float[audioBufferSize];
IntPtr audioBufferPointer = IntPtr.Zero;

Raylib_CSharp.Raylib.SetConfigFlags(ConfigFlags.HighDpiWindow);
Window.Init(screenWidth, screenHeight, "TinySynth");
SetTargetFPS(60);

AudioDevice.Init();
AudioStream.SetBufferSizeDefault(audioBufferSize);
AudioStream audioStream = AudioStream.Load(sampleRate, 32, 1);
audioStream.Play();
audioBufferPointer = Marshal.AllocHGlobal(audioBuffer.Length * sizeof(float));
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
    keyboardPanelHeight);

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

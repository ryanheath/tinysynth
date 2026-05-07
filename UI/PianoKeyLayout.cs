using Raylib_CSharp.Transformations;

namespace TinySynth.UI;

internal readonly record struct PianoKeyLayout(int MidiNote, bool IsBlack, Rectangle Bounds, string Label);

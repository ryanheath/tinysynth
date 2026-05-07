using Raylib_CSharp.Transformations;

namespace TinySynth.UI;

internal readonly record struct LayoutMetrics(
    Rectangle ControlPanel,
    Rectangle WaveformPanel,
    Rectangle KeyboardPanel,
    Rectangle OscillatorButtonsArea,
    float SliderRowOneY,
    float SliderRowTwoY,
    float SliderWidth,
    Rectangle WaveformButtonsArea);

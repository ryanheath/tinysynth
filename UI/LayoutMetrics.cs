using Raylib_CSharp.Transformations;

namespace TinySynth.UI;

internal readonly record struct LayoutMetrics(
    Rectangle ControlPanel,
    Rectangle WaveformPanel,
    Rectangle KeyboardPanel,
    Rectangle ModeButtonsArea,
    Rectangle OscillatorButtonsArea,
    Rectangle FilterButtonsArea,
    float SliderRowOneY,
    float SliderRowTwoY,
    float SliderWidth,
    float FilterSliderY,
    float FilterSliderWidth,
    Rectangle WaveformButtonsArea);

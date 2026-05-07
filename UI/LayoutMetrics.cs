using Raylib_CSharp.Transformations;

namespace TinySynth.UI;

internal readonly record struct LayoutMetrics(
    Rectangle ControlPanel,
    Rectangle WaveformPanel,
    Rectangle KeyboardPanel,
    Rectangle ModeButtonsArea,
    Rectangle OscillatorButtonsArea,
    Rectangle FilterButtonsArea,
    Rectangle FilterAnalysisArea,
    float SliderRowOneY,
    float SliderRowTwoY,
    float SliderWidth,
    float FilterSliderY,
    float FilterSliderRowTwoY,
    float FilterSliderRowThreeY,
    float FilterSliderWidth,
    float FilterFullWidth,
    Rectangle WaveformButtonsArea);

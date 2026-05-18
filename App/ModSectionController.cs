using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.Synth;
using TinySynth.UI;

namespace TinySynth.App;

internal static class ModSectionController
{
    public static void Draw(
        Rectangle controlPanel,
        LayoutMetrics layout,
        ref UiControlId? activeSlider,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown,
        SynthParameters synthParameters)
    {
        float modLfoKnob1X = layout.ModLfo1ShapeArea.X + layout.ModLfo1ShapeArea.Width + 18f;
        float modLfoKnob2X = modLfoKnob1X + layout.ModLfoKnobWidth + 18f;
        float modLfo2Knob1X = layout.ModLfo2ShapeArea.X + layout.ModLfo2ShapeArea.Width + 18f;
        float modLfo2Knob2X = modLfo2Knob1X + layout.ModLfoKnobWidth + 18f;
        float modLfo1LabelY = layout.ModLfo1ShapeArea.Y - 24f;
        float modLfo2LabelY = layout.ModLfo2ShapeArea.Y - 24f;

        Graphics.DrawText("Modulation", (int)controlPanel.X + 20, (int)controlPanel.Y + 132, 18, UiTheme.MutedTextColor);
        Graphics.DrawText("LFO 1", (int)layout.ModLfo1ShapeArea.X, (int)modLfo1LabelY, 18, UiTheme.MutedTextColor);
        synthParameters.Lfo1.Shape = SynthRenderer.DrawModulationLfoShapeButtons(
            layout.ModLfo1ShapeArea,
            synthParameters.Lfo1.Shape,
            mousePosition,
            mousePressed);

        synthParameters.Lfo1.RateHz = SynthRenderer.DrawKnobSlider(
            index: UiControlId.ModulationLfo1Rate,
            activeSlider: ref activeSlider,
            enabled: true,
            label: "Rate",
            valueLabel: $"{synthParameters.Lfo1.RateHz:0.0}Hz",
            bounds: new Rectangle(modLfoKnob1X, modLfo1LabelY, layout.ModLfoKnobWidth, 20),
            value: synthParameters.Lfo1.RateHz,
            minValue: 0.10f,
            maxValue: 12.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.Lfo1.Depth = SynthRenderer.DrawKnobSlider(
            index: UiControlId.ModulationLfo1Depth,
            activeSlider: ref activeSlider,
            enabled: true,
            label: "Depth",
            valueLabel: $"{synthParameters.Lfo1.Depth:0.00}",
            bounds: new Rectangle(modLfoKnob2X, modLfo1LabelY, layout.ModLfoKnobWidth, 20),
            value: synthParameters.Lfo1.Depth,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        Graphics.DrawText("LFO 2", (int)layout.ModLfo2ShapeArea.X, (int)modLfo2LabelY, 18, UiTheme.MutedTextColor);
        synthParameters.Lfo2.Shape = SynthRenderer.DrawModulationLfoShapeButtons(
            layout.ModLfo2ShapeArea,
            synthParameters.Lfo2.Shape,
            mousePosition,
            mousePressed);

        synthParameters.Lfo2.RateHz = SynthRenderer.DrawKnobSlider(
            index: UiControlId.ModulationLfo2Rate,
            activeSlider: ref activeSlider,
            enabled: true,
            label: "Rate",
            valueLabel: $"{synthParameters.Lfo2.RateHz:0.0}Hz",
            bounds: new Rectangle(modLfo2Knob1X, modLfo2LabelY, layout.ModLfoKnobWidth, 20),
            value: synthParameters.Lfo2.RateHz,
            minValue: 0.10f,
            maxValue: 12.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.Lfo2.Depth = SynthRenderer.DrawKnobSlider(
            index: UiControlId.ModulationLfo2Depth,
            activeSlider: ref activeSlider,
            enabled: true,
            label: "Depth",
            valueLabel: $"{synthParameters.Lfo2.Depth:0.00}",
            bounds: new Rectangle(modLfo2Knob2X, modLfo2LabelY, layout.ModLfoKnobWidth, 20),
            value: synthParameters.Lfo2.Depth,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        Graphics.DrawText("Routes 1-3", (int)layout.ModRouteColumn1Area.X, (int)layout.ModRouteColumn1Area.Y - 28, 18, UiTheme.MutedTextColor);
        Graphics.DrawText("Routes 4-6", (int)layout.ModRouteColumn2Area.X, (int)layout.ModRouteColumn2Area.Y - 28, 18, UiTheme.MutedTextColor);

        for (int routeIndex = 0; routeIndex < SynthParameters.ModulationRouteCount; routeIndex++)
        {
            ModulationRoute route = synthParameters.GetModulationRoute(routeIndex);
            Rectangle routeColumnArea = routeIndex < 3 ? layout.ModRouteColumn1Area : layout.ModRouteColumn2Area;
            int routeRowIndex = routeIndex < 3 ? routeIndex : routeIndex - 3;
            float rowY = routeColumnArea.Y + (routeRowIndex * (layout.ModRouteRowHeight + layout.ModRouteGap));
            const float routeLabelWidth = 14f;
            const float routeLabelGap = 6f;
            float sourceWidth = MathF.Max(64f, layout.ModSourceWidth - routeLabelWidth - routeLabelGap);
            Rectangle routeLabelBounds = new(routeColumnArea.X, rowY, routeLabelWidth, layout.ModRouteRowHeight);
            Rectangle sourceBounds = new(routeLabelBounds.X + routeLabelBounds.Width + routeLabelGap, rowY, sourceWidth, layout.ModRouteRowHeight);
            Rectangle destinationBounds = new(sourceBounds.X + sourceBounds.Width + layout.ModRouteGap, rowY, layout.ModDestinationWidth, layout.ModRouteRowHeight);
            Rectangle targetBounds = new(destinationBounds.X + destinationBounds.Width + layout.ModRouteGap, rowY, layout.ModAmountWidth, layout.ModRouteRowHeight);
            Rectangle amountLabelBounds = new(targetBounds.X + targetBounds.Width + layout.ModRouteGap, rowY + 2, layout.ModAmountWidth, 16);
            Rectangle amountBounds = new(amountLabelBounds.X, rowY + 20, layout.ModAmountWidth, 12);
            bool routeActive = route.Source != ModulationSource.None && route.Destination != ModulationDestination.None;

            Graphics.DrawText($"{routeIndex + 1}", (int)routeLabelBounds.X, (int)routeLabelBounds.Y + 8, 16, UiTheme.MutedTextColor);
            route.Source = SynthRenderer.DrawModulationSourceCycler(sourceBounds, route.Source, true, mousePosition, mousePressed);
            route.Destination = SynthRenderer.DrawModulationDestinationCycler(destinationBounds, route.Destination, true, mousePosition, mousePressed);
            route.OscillatorIndex = SynthRenderer.DrawModulationOscillatorTargetCycler(targetBounds, route.OscillatorIndex, true, mousePosition, mousePressed);

            if (route.Source == ModulationSource.None || route.Destination == ModulationDestination.None)
            {
                route.Amount = 0f;
                route.OscillatorIndex = -1;
            }

            Graphics.DrawText($"Amt {route.Amount:0.00}", (int)amountLabelBounds.X, (int)amountLabelBounds.Y, 14, routeActive ? UiTheme.MutedTextColor : UiTheme.DisabledSecondaryTextColor);
            route.Amount = SynthRenderer.DrawCompactSlider(
                index: UiControlIds.RouteAmount(routeIndex),
                activeSlider: ref activeSlider,
                enabled: routeActive,
                bounds: amountBounds,
                value: route.Amount,
                minValue: -1.00f,
                maxValue: 1.00f,
                mousePosition: mousePosition,
                mousePressed: mousePressed,
                mouseDown: mouseDown);
        }
    }
}

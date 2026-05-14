namespace TinySynth.App.Services;

internal readonly record struct AudioDiagnostics(
    double LastRenderMilliseconds,
    double AverageRenderMilliseconds,
    double BlockBudgetMilliseconds,
    int OverrunCount,
    int DiscontinuityCount,
    int ClipCount,
    float MaxDiscontinuity,
    float PeakLevel)
{
    public bool IsOverBudget => LastRenderMilliseconds > BlockBudgetMilliseconds;
}

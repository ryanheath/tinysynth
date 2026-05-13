namespace TinySynth.Synth.AudioGraph;

internal sealed class AudioGraph(AudioNode outputNode)
{
    public AudioNode OutputNode { get; } = outputNode;
}

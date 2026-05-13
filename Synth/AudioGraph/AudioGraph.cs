namespace TinySynth.Synth.AudioGraph;

internal sealed class AudioGraph
{
    public AudioGraph(AudioNode outputNode)
    {
        OutputNode = outputNode;
    }

    public AudioNode OutputNode { get; }
}

namespace TinySynth.Synth.AudioGraph;

internal sealed class AudioGraphScheduler
{
    private readonly AudioGraph _graph;
    private readonly IReadOnlyList<AudioNode> _orderedNodes;

    public AudioGraphScheduler(AudioGraph graph)
    {
        _graph = graph;
        _orderedNodes = BuildTopologicalOrder(graph.OutputNode);
    }

    public IReadOnlyList<AudioNode> OrderedNodes => _orderedNodes;

    public AudioBuffer Execute(in AudioRenderContext context)
    {
        foreach (AudioNode node in _orderedNodes)
        {
            node.Render(context);
        }

        return _graph.OutputNode.Output;
    }

    private static IReadOnlyList<AudioNode> BuildTopologicalOrder(AudioNode outputNode)
    {
        List<AudioNode> orderedNodes = [];
        HashSet<AudioNode> visited = [];
        HashSet<AudioNode> active = [];
        Visit(outputNode, visited, active, orderedNodes);
        return orderedNodes;
    }

    private static void Visit(AudioNode node, HashSet<AudioNode> visited, HashSet<AudioNode> active, List<AudioNode> orderedNodes)
    {
        if (visited.Contains(node))
        {
            return;
        }

        if (!active.Add(node))
        {
            throw new InvalidOperationException($"Cycle detected in audio graph at node '{node.Name}'.");
        }

        foreach (AudioNode input in node.Inputs)
        {
            Visit(input, visited, active, orderedNodes);
        }

        active.Remove(node);
        visited.Add(node);
        orderedNodes.Add(node);
    }
}

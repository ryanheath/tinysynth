namespace TinySynth.Synth.AudioGraph;

internal abstract class AudioNode
{
    private readonly List<AudioNode> _inputs = [];
    private int _lastProcessedBlockId = -1;

    protected AudioNode(string name, params AudioNode[] inputs)
    {
        Name = name;
        _inputs.AddRange(inputs);
    }

    public string Name { get; }

    public IReadOnlyList<AudioNode> Inputs => _inputs;

    public AudioBuffer Output { get; } = new();

    public AudioBuffer GetBuffer(in AudioRenderContext context)
    {
        if (_lastProcessedBlockId == context.BlockId)
        {
            return Output;
        }

        foreach (AudioNode input in _inputs)
        {
            input.GetBuffer(context);
        }

        Render(context);
        return Output;
    }

    public void Render(in AudioRenderContext context)
    {
        if (_lastProcessedBlockId == context.BlockId)
        {
            return;
        }

        Output.EnsureFrameCount(context.FrameCount);
        Output.Clear();
        Process(context, _inputs, Output);
        _lastProcessedBlockId = context.BlockId;
    }

    protected abstract void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output);
}

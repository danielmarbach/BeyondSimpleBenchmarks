using BenchmarkDotNet.Attributes;
using NServiceBus.Pipeline;

namespace PipelineOptimizations.Step2;

[ShortRunJob]
[MemoryDiagnoser]
public class Step2_PipelineWarmup
{
    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizations;

    [GlobalSetup]
    public void SetUp()
    {
        pipelineModificationsBeforeOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Step1.BehaviorOptimization), i.ToString(), b => new Step1.BehaviorOptimization()));
        }

        pipelineModificationsAfterOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Step1.BehaviorOptimization), i.ToString(), b => new Step1.BehaviorOptimization()));
        }
    }

    [Benchmark(Baseline = true)]
    public Step1.PipelineOptimization<IBehaviorContext> Before()
    {
        var pipelineBeforeOptimizations = new Step1.PipelineOptimization<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        return pipelineBeforeOptimizations;
    }

    [Benchmark]
    public PipelineOptimization<IBehaviorContext> After()
    {
        var pipelineAfterOptimizations = new PipelineOptimization<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizations);
        return pipelineAfterOptimizations;
    }
}
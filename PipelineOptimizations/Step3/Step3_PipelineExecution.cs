using BenchmarkDotNet.Attributes;
using NServiceBus.Pipeline;

namespace PipelineOptimizations.Step3;

[ShortRunJob]
[MemoryDiagnoser]
public class Step3_PipelineExecution
{
    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    private BehaviorContext behaviorContext;
    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizations;
    private Step2.PipelineOptimization<IBehaviorContext> pipelineBeforeOptimizations;
    private PipelineOptimization<IBehaviorContext> pipelineAfterOptimizations;

    [GlobalSetup]
    public void SetUp()
    {
        behaviorContext = new BehaviorContext();

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

        pipelineBeforeOptimizations = new Step2.PipelineOptimization<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        pipelineAfterOptimizations = new PipelineOptimization<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizations);
    }

    [Benchmark(Baseline = true)]
    public async Task Before()
    {
        await pipelineBeforeOptimizations.Invoke(behaviorContext);
    }

    [Benchmark]
    public async Task After()
    {
        await pipelineAfterOptimizations.Invoke(behaviorContext);
    }
}
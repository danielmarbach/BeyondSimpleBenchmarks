using BenchmarkDotNet.Attributes;
using NServiceBus.Pipeline;

namespace PipelineOptimizations.Step1;

[ShortRunJob]
[MemoryDiagnoser]
public class Step1_PipelineExecution
{
    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }


    [GlobalSetup]
    public void SetUp()
    {
        behaviorContext = new BehaviorContext();

        pipelineModificationsBeforeOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(BaseLineBehavior), i.ToString(), b => new BaseLineBehavior()));
        }

        pipelineModificationsAfterOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(BehaviorOptimization), i.ToString(), b => new BehaviorOptimization()));
        }

        pipelineBeforeOptimizations = new BaseLinePipeline<IBehaviorContext>(null, new SettingsHolder(),
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

    private BehaviorContext behaviorContext;
    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizations;
    private BaseLinePipeline<IBehaviorContext> pipelineBeforeOptimizations;
    private PipelineOptimization<IBehaviorContext> pipelineAfterOptimizations;
}
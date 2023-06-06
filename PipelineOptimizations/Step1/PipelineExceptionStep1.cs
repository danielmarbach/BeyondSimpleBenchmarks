using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using NServiceBus.Pipeline;

namespace PipelineOptimizations;

[Config(typeof(Config))]
public class PipelineExceptionStep1
{
    class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default);
        }
    }


    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    private BehaviorContext behaviorContext;
    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizations;
    private BaseLinePipeline<IBehaviorContext> pipelineBeforeOptimizations;
    private PipelineStep1Optimization<IBehaviorContext> pipelineAfterOptimizations;

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
        var stepdId = PipelineDepth + 1;
        pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(stepdId.ToString(), typeof(Throwing), "1", b => new Throwing()));

        pipelineModificationsAfterOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(BehaviorStep1Optimization), i.ToString(), b => new BehaviorStep1Optimization()));
        }
        pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(stepdId.ToString(), typeof(Throwing), "1", b => new Throwing()));

        pipelineBeforeOptimizations = new BaseLinePipeline<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        pipelineAfterOptimizations = new PipelineStep1Optimization<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizations);
    }

    [Benchmark(Baseline = true)]
    public async Task Before()
    {
        try
        {
            await pipelineBeforeOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
    }

    [Benchmark]
    public async Task After()
    {
        try
        {
            await pipelineAfterOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
    }
    
    class Throwing : Behavior<IBehaviorContext>
    {
        public override Task Invoke(IBehaviorContext context, Func<Task> next)
        {
            throw new InvalidOperationException();
        }
    }
}
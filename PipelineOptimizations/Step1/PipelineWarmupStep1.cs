using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using NServiceBus.Pipeline;

namespace PipelineOptimizations;

[Config(typeof(Config))]
public class PipelineWarmupStep1
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

    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizations;

    [GlobalSetup]
    public void SetUp()
    {
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
                typeof(BehaviorStep1Optimization), i.ToString(), b => new BehaviorStep1Optimization()));
        }
    }

    [Benchmark(Baseline = true)]
    public BaseLinePipeline<IBehaviorContext> Before()
    {
        var pipelineBeforeOptimizations = new BaseLinePipeline<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        return pipelineBeforeOptimizations;
    }

    [Benchmark]
    public PipelineStep1Optimization<IBehaviorContext> After()
    {
        var pipelineAfterOptimizations = new PipelineStep1Optimization<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizations);
        return pipelineAfterOptimizations;
    }
}
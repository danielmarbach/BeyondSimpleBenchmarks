using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using PipelineOptimizations.Pipeline;

namespace PipelineOptimizations;

[Config(typeof(Config))]
public class PipelineExecutionStep1
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default);
        }
    }


    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    private BehaviorContext behaviorContext;
    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizationsWithUnsafe;
    private BaseLinePipeline<IBehaviorContext> pipelineBeforeOptimizations;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> pipelineAfterOptimizationsWithUnsafe;

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

        pipelineModificationsAfterOptimizationsWithUnsafe = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsAfterOptimizationsWithUnsafe.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
        }

        pipelineBeforeOptimizations = new BaseLinePipeline<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        pipelineAfterOptimizationsWithUnsafe = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizationsWithUnsafe);

        // warmup and cache
        pipelineBeforeOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
        pipelineAfterOptimizationsWithUnsafe.Invoke(behaviorContext).GetAwaiter().GetResult();
    }
}
using NServiceBus.Pipeline;

namespace PipelineOptimizations;

public class PipelineStep1Optimization<TContext>
    where TContext : IBehaviorContext
{
    public PipelineStep1Optimization(IBuilder builder, ReadOnlySettings settings,
        PipelineModifications pipelineModifications)
    {
        var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals,
            pipelineModifications.Replacements);

        foreach (var rego in pipelineModifications.Additions.Where(x => x.IsEnabled(settings)))
        {
            coordinator.Register(rego);
        }

        // Important to keep a reference
        behaviors = coordinator.BuildPipelineModelFor<TContext>()
            .Select(r => r.CreateBehaviorNew(builder)).ToArray();

        pipeline = behaviors.CreatePipelineExecutionFuncFor<TContext>();
    }

    public Task Invoke(TContext context)
    {
        return pipeline(context);
    }

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    IBehavior[] behaviors;
    Func<TContext, Task> pipeline;
}
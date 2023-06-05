namespace NServiceBus.Pipeline;

public class BaseLinePipeline<TContext>
    where TContext : IBehaviorContext
{
    public BaseLinePipeline(IBuilder builder, ReadOnlySettings settings,
        PipelineModifications pipelineModifications)
    {
        var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals,
            pipelineModifications.Replacements);

        foreach (var rego in pipelineModifications.Additions.Where(x => x.IsEnabled(settings)))
        {
            coordinator.Register(rego);
        }

        behaviors = coordinator.BuildPipelineModelFor<TContext>()
            .Select(r => r.CreateBehaviorOld(builder)).ToArray();
    }

    public Task Invoke(TContext context)
    {
        var pipeline = new BehaviorChain(behaviors);
        return pipeline.Invoke(context);
    }

    BehaviorInstance[] behaviors;
}
using NServiceBus.Pipeline;

namespace PipelineOptimizations.Step1;

public class BehaviorOptimization : IBehavior<IBehaviorContext, IBehaviorContext>
{
    public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
    {
        return next(context);
    }
}
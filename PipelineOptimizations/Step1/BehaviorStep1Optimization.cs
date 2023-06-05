using NServiceBus.Pipeline;

namespace PipelineOptimizations;

public class BehaviorStep1Optimization : IBehavior<IBehaviorContext, IBehaviorContext>
{
    public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
    {
        return next(context);
    }
}
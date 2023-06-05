namespace NServiceBus.Pipeline;

public class BaseLineBehavior : Behavior<IBehaviorContext>
{
    public override Task Invoke(IBehaviorContext context, Func<Task> next)
    {
        return next();
    }
}
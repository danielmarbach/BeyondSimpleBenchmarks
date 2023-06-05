namespace NServiceBus.Pipeline;

public abstract class PipelineTerminator<T> : StageConnector<T, PipelineTerminator<T>.ITerminatingContext>,
    IPipelineTerminator where T : IBehaviorContext
{

    protected abstract Task Terminate(T context);

    public sealed override Task Invoke(T context, Func<ITerminatingContext, Task> next)
    {
        return Terminate(context);
    }

    public interface ITerminatingContext : IBehaviorContext
    {
    }
}
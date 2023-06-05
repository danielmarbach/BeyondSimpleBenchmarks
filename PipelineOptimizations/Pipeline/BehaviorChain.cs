namespace NServiceBus.Pipeline;

class BehaviorChain : IDisposable
{
    public BehaviorChain(IEnumerable<BehaviorInstance> behaviorList)
    {
        itemDescriptors = behaviorList.ToArray();
    }

    public void Dispose()
    {
    }

    public Task Invoke(IBehaviorContext context)
    {

        return InvokeNext(context, 0);
    }

    Task InvokeNext(IBehaviorContext context, int currentIndex)
    {
        if (currentIndex == itemDescriptors.Length)
        {
            return Task.CompletedTask;
        }

        var behavior = itemDescriptors[currentIndex];

        return behavior.Invoke(context, newContext => InvokeNext(newContext, currentIndex + 1));
    }

    BehaviorInstance[] itemDescriptors;
}
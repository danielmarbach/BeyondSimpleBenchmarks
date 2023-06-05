namespace NServiceBus.Pipeline;

class BehaviorContext : ContextBag, IBehaviorContext
{
    public ContextBag Extensions => this;
}
namespace NServiceBus.Pipeline;

public partial class ContextBag
{
    internal IBehavior[] Behaviors
    {
        get => behaviors ?? parentBag?.Behaviors;
        set => behaviors = value;
    }
    
    IBehavior[] behaviors;
}
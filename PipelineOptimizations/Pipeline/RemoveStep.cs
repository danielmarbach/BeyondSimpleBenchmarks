namespace NServiceBus.Pipeline;

public class RemoveStep
{
    public RemoveStep(string removeId)
    {
        RemoveId = removeId;
    }

    public string RemoveId { get; private set; }
}
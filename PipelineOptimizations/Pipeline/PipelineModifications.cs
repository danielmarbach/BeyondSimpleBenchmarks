namespace NServiceBus.Pipeline;

public class PipelineModifications
{
    public List<RegisterStep> Additions = new List<RegisterStep>();
    public List<RemoveStep> Removals = new List<RemoveStep>();
    public List<ReplaceStep> Replacements = new List<ReplaceStep>();
}
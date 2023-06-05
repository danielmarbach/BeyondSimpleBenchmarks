using System.ComponentModel;

namespace NServiceBus.Pipeline;

public interface IExtendable
{
    /// <summary>
    /// A <see cref="ContextBag" /> which can be used to extend the current object.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    ContextBag Extensions { get; }
}
namespace NServiceBus.Pipeline;

public interface IBuilder : IDisposable
{
    object Build(Type typeToBuild);

    IBuilder CreateChildBuilder();

    T Build<T>();

    IEnumerable<T> BuildAll<T>();

    IEnumerable<object> BuildAll(Type typeToBuild);

    void Release(object instance);

    void BuildAndDispatch(Type typeToBuild, Action<object> action);
}
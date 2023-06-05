namespace NServiceBus.Pipeline;

public partial class ContextBag
{
    public ContextBag(ContextBag parentBag = null)
    {
        this.parentBag = parentBag;
    }

    public T Get<T>()
    {
        return Get<T>(typeof(T).FullName);
    }

    public bool TryGet<T>(out T result)
    {
        return TryGet(typeof(T).FullName, out result);
    }

    public bool TryGet<T>(string key, out T result)
    {
        if (stash.TryGetValue(key, out var value))
        {
            result = (T)value;
            return true;
        }

        if (parentBag != null)
        {
            return parentBag.TryGet(key, out result);
        }

        result = default;
        return false;
    }

    public T Get<T>(string key)
    {
        if (!TryGet(key, out T result))
        {
            throw new KeyNotFoundException("No item found in behavior context with key: " + key);
        }

        return result;
    }

    public T GetOrCreate<T>() where T : class, new()
    {
        if (TryGet(out T value))
        {
            return value;
        }

        var newInstance = new T();

        Set(newInstance);

        return newInstance;
    }

    public void Set<T>(T t)
    {
        Set(typeof(T).FullName, t);
    }


    public void Remove<T>()
    {
        Remove(typeof(T).FullName);
    }

    public void Remove(string key)
    {
        stash.Remove(key);
    }

    public void Set<T>(string key, T t)
    {
        stash[key] = t;
    }

    internal void Merge(ContextBag context)
    {
        foreach (var kvp in context.stash)
        {
            stash[kvp.Key] = kvp.Value;
        }
    }

    ContextBag parentBag;

    Dictionary<string, object> stash = new Dictionary<string, object>();
}
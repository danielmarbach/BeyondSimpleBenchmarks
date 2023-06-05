using System.Collections.Concurrent;

namespace NServiceBus.Pipeline;

public class SettingsHolder : ReadOnlySettings
{
    public T Get<T>(string key)
    {
        return (T) Get(key);
    }

    public bool TryGet<T>(out T val)
    {
        return TryGet(typeof(T).FullName, out val);
    }

    public bool TryGet<T>(string key, out T val)
    {
        val = default(T);

        object tmp;
        if (!Overrides.TryGetValue(key, out tmp))
        {
            if (!Defaults.TryGetValue(key, out tmp))
            {
                return false;
            }
        }

        if (!(tmp is T))
        {
            return false;
        }

        val = (T) tmp;
        return true;
    }

    public T Get<T>()
    {
        return (T) Get(typeof(T).FullName);
    }

    public object Get(string key)
    {
        object result;
        if (Overrides.TryGetValue(key, out result))
        {
            return result;
        }

        if (Defaults.TryGetValue(key, out result))
        {
            return result;
        }

        throw new KeyNotFoundException($"The given key ({key}) was not present in the dictionary.");
    }

    public T GetOrDefault<T>()
    {
        return GetOrDefault<T>(typeof(T).FullName);
    }

    public T GetOrDefault<T>(string key)
    {
        object result;
        if (Overrides.TryGetValue(key, out result))
        {
            return (T) result;
        }

        if (Defaults.TryGetValue(key, out result))
        {
            return (T) result;
        }

        return default(T);
    }

    public bool HasSetting(string key)
    {
        return Overrides.ContainsKey(key) || Defaults.ContainsKey(key);
    }

    public bool HasSetting<T>()
    {
        var key = typeof(T).FullName;

        return HasSetting(key);
    }

    public bool HasExplicitValue(string key)
    {
        return Overrides.ContainsKey(key);
    }

    public bool HasExplicitValue<T>()
    {
        var key = typeof(T).FullName;

        return HasExplicitValue(key);
    }

    public T GetOrCreate<T>()
        where T : class, new()
    {
        T value;
        if (!TryGet(out value))
        {
            value = new T();
            Set<T>(value);
        }
        return value;
    }

    public void Set(string key, object value)
    {
        EnsureWriteEnabled(key);

        Overrides[key] = value;
    }

    public void Set<T>(object value)
    {
        Set(typeof(T).FullName, value);
    }

    public void Set<T>(Action value)
    {
        Set(typeof(T).FullName, value);
    }

    public void SetDefault<T>(object value)
    {
        SetDefault(typeof(T).FullName, value);
    }

    public void SetDefault<T>(Action value)
    {
        SetDefault(typeof(T).FullName, value);
    }

    public void SetDefault(string key, object value)
    {
        EnsureWriteEnabled(key);

        Defaults[key] = value;
    }

    internal void PreventChanges()
    {
        locked = true;
    }

    internal void Merge(ReadOnlySettings settings)
    {
        EnsureMergingIsPossible();

        var holder = settings as SettingsHolder ?? new SettingsHolder();

        foreach (var @default in holder.Defaults)
        {
            Defaults[@default.Key] = @default.Value;
        }

        foreach (var @override in holder.Overrides)
        {
            Overrides[@override.Key] = @override.Value;
        }
    }

    void EnsureMergingIsPossible()
    {
        if (locked)
        {
            throw new Exception(
                "Unable to merge settings. The settings has been locked for modifications. Move any configuration code earlier in the configuration pipeline");
        }
    }

    void EnsureWriteEnabled(string key)
    {
        if (locked)
        {
            throw new Exception(
                $"Unable to set the value for key: {key}. The settings has been locked for modifications. Move any configuration code earlier in the configuration pipeline");
        }
    }

    public void Clear()
    {
        foreach (var item in Defaults)
        {
            (item.Value as IDisposable)?.Dispose();
        }

        Defaults.Clear();

        foreach (var item in Overrides)
        {
            (item.Value as IDisposable)?.Dispose();
        }

        Overrides.Clear();
    }

    ConcurrentDictionary<string, object> Defaults =
        new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    bool locked;

    ConcurrentDictionary<string, object> Overrides =
        new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
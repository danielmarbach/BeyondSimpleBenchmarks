namespace NServiceBus.Pipeline;

public interface ReadOnlySettings
{
    T Get<T>();

    T Get<T>(string key);

    bool TryGet<T>(out T val);

    bool TryGet<T>(string key, out T val);

    object Get(string key);

    T GetOrDefault<T>();

    T GetOrDefault<T>(string key);

    bool HasSetting(string key);

    bool HasSetting<T>();

    bool HasExplicitValue(string key);

    bool HasExplicitValue<T>();
}
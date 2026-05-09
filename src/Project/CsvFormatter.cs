using System;

namespace Shane32.CsvLinq;

internal sealed class CsvFormatter<TValue> : ICsvFormatter
{
    private readonly Func<string, TValue> _deserialize;
    private readonly Func<TValue, string> _serialize;

    internal CsvFormatter(Func<string, TValue> deserialize, Func<TValue, string> serialize)
    {
        _deserialize = deserialize;
        _serialize = serialize;
    }

    object ICsvFormatter.Deserialize(string value)
    {
        if (_deserialize == null)
            throw new InvalidOperationException($"No deserializer was configured for type {typeof(TValue)}");
        return _deserialize(value);
    }

    string ICsvFormatter.Serialize(object value)
    {
        if (_serialize == null)
            throw new InvalidOperationException($"No serializer was configured for type {typeof(TValue)}");
        return _serialize((TValue)value);
    }
}

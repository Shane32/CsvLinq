using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shane32.CsvLinq;

/// <summary>
/// Describes a configured CSV column.
/// </summary>
public sealed class CsvColumnModel
{
    internal CsvColumnModel(string name, Type type, MemberInfo member)
    {
        Name = name;
        Type = type;
        Member = member;
    }

    /// <summary>
    /// Gets the primary CSV header name for the column.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the model member value type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the model member mapped to the column.
    /// </summary>
    public MemberInfo Member { get; }

    /// <summary>
    /// Gets the alternate CSV header names that can map to the column.
    /// </summary>
    public IList<string> AlternateNames { get; } = new List<string>();

    /// <summary>
    /// Gets a value indicating whether the column is optional when loading CSV data.
    /// </summary>
    public bool Optional { get; internal set; }

    internal Func<string, object> Deserializer { get; set; }

    internal Func<object, string> Serializer { get; set; }
}

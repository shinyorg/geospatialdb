using System;

namespace Shiny.Spatial.Database;

public sealed class PropertyDefinition
{
    public string Name { get; }
    public PropertyType Type { get; }

    public PropertyDefinition(string name, PropertyType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Property name cannot be empty.", nameof(name));
        Name = name;
        Type = type;
    }

    internal string SqlType => Type switch
    {
        PropertyType.Text => "TEXT",
        PropertyType.Integer => "INTEGER",
        PropertyType.Real => "REAL",
        PropertyType.Blob => "BLOB",
        _ => throw new ArgumentOutOfRangeException()
    };
}

public enum PropertyType
{
    Text,
    Integer,
    Real,
    Blob
}

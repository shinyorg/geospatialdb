namespace Shiny.Spatial.Database;

internal sealed class PropertyFilter
{
    public string PropertyName { get; }
    public string Operator { get; }
    public object Value { get; }

    public PropertyFilter(string propertyName, string op, object value)
    {
        PropertyName = propertyName;
        Operator = ValidateOperator(op);
        Value = value;
    }

    private static string ValidateOperator(string op) => op switch
    {
        "=" or "==" => "=",
        "!=" or "<>" => "!=",
        "<" => "<",
        "<=" => "<=",
        ">" => ">",
        ">=" => ">=",
        "LIKE" or "like" => "LIKE",
        _ => throw new System.ArgumentException($"Unsupported operator: {op}")
    };
}

namespace Shane32.CsvLinq;

internal interface ICsvFormatter
{
    public object Deserialize(string value);

    public string Serialize(object value);
}

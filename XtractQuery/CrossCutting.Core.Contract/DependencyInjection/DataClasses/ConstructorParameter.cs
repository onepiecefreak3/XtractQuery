namespace CrossCutting.Core.Contract.DependencyInjection.DataClasses;

public class ConstructorParameter(string name, object value)
{
    public string Name { get; } = name;
    public object Value { get; } = value;
}
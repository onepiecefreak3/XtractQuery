namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class TypeChoiceAttribute : Attribute
{
    public string FieldName { get; }
    public TypeChoiceComparer Comparer { get; }
    public ulong Value { get; }
    public Type InjectionType { get; }

    public TypeChoiceAttribute(string fieldName, TypeChoiceComparer comp, ulong value, Type injectionType)
    {
        FieldName = fieldName;
        Comparer = comp;
        Value = value;
        InjectionType = injectionType;
    }
}
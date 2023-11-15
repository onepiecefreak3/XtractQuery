namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract
{
    public interface IValueStorage
    {
        bool Exists(string fieldName);

        object Get(string fieldName);
        void Set(string fieldName, object value);

        IValueStorage CreateScope(string fieldName);
    }
}

namespace XtractQuery.Interfaces
{
    interface IStringWriter
    {
        long Write(string value);

        uint GetHash(string value);
    }
}

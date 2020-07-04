namespace XtractQuery.Interfaces
{
    interface IStringWriter
    {
        long Write(string value);

        uint GetCrc32(string value);

        ushort GetCrc16(string value);
    }
}

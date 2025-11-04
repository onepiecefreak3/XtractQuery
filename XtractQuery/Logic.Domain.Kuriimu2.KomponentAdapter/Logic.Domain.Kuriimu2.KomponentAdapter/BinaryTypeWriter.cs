using System.Collections;
using System.Reflection;
using Komponent.Contract.Exceptions;
using Komponent.IO;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter;

internal class BinaryTypeWriter : IBinaryTypeWriter
{
    private readonly IValueStorageFactory _storageFactory;
    private readonly IMemberInfoProvider _infoProvider;

    public BinaryTypeWriter(IValueStorageFactory storageFactory, IMemberInfoProvider infoProvider)
    {
        _storageFactory = storageFactory;
        _infoProvider = infoProvider;
    }

    public void Write(object value, IBinaryWriterX writer)
    {
        IValueStorage storage = _storageFactory.Create();
        WriteInternal(value, value.GetType(), writer, storage);
    }

    public void WriteMany<T>(IEnumerable<T> list, IBinaryWriterX writer)
    {
        foreach (T element in list)
            Write(element, writer);
    }

    private void WriteInternal(object writeValue, Type writeType, IBinaryWriterX writer, IValueStorage storage, FieldInfo? fieldInfo = null)
    {
        ByteOrder bkByteOrder = writer.ByteOrder;
        BitOrder bkBitOrder = writer.BitOrder;
        int bkBlockSize = writer.BlockSize;

        writer.ByteOrder = _infoProvider.GetByteOrder(fieldInfo, _infoProvider.GetByteOrder(writeType, writer.ByteOrder));

        if (writeType.IsPrimitive)
        {
            WritePrimitive(writeValue, writeType, writer);
        }
        else if (writeType == typeof(string))
        {
            LengthInfo? lengthInfo = _infoProvider.GetLengthInfo(fieldInfo, storage);
            WriteString((string)writeValue, writer, lengthInfo);
        }
        else if (writeType == typeof(decimal))
        {
            writer.Write((decimal)writeValue);
        }
        else if (IsList(writeType))
        {
            LengthInfo? lengthInfo = _infoProvider.GetLengthInfo(fieldInfo, storage);
            if (lengthInfo != null)
                WriteList((IList)writeValue, writer, lengthInfo, storage);
        }
        else if (writeType.IsClass || IsStruct(writeType))
        {
            WriteComplex(writeValue, writeType, writer, storage.CreateScope(fieldInfo?.Name));
        }
        else if (writeType.IsEnum)
        {
            FieldInfo? underlyingType = (writeType as TypeInfo)?.DeclaredFields.ToList()[0];
            if (underlyingType != null)
                WriteInternal(underlyingType.GetValue(writeValue)!, underlyingType.FieldType, writer, storage);
        }
        else throw new UnsupportedTypeException(writeType);

        writer.ByteOrder = bkByteOrder;
        writer.BitOrder = bkBitOrder;
        writer.BlockSize = bkBlockSize;
    }

    private void WritePrimitive(object writeValue, Type writeType, IBinaryWriterX writer)
    {
        switch (Type.GetTypeCode(writeType))
        {
            case TypeCode.Boolean: writer.Write((bool)writeValue); break;
            case TypeCode.Byte: writer.Write((byte)writeValue); break;
            case TypeCode.SByte: writer.Write((sbyte)writeValue); break;
            case TypeCode.Int16: writer.Write((short)writeValue); break;
            case TypeCode.UInt16: writer.Write((ushort)writeValue); break;
            case TypeCode.Char: writer.Write((char)writeValue); break;
            case TypeCode.Int32: writer.Write((int)writeValue); break;
            case TypeCode.UInt32: writer.Write((uint)writeValue); break;
            case TypeCode.Int64: writer.Write((long)writeValue); break;
            case TypeCode.UInt64: writer.Write((ulong)writeValue); break;
            case TypeCode.Single: writer.Write((float)writeValue); break;
            case TypeCode.Double: writer.Write((double)writeValue); break;
            default: throw new NotSupportedException($"Unsupported primitive {writeType.FullName}.");
        }
    }

    private void WriteString(string writeValue, IBinaryWriterX writer, LengthInfo? lengthInfo)
    {
        // If no length attributes are given, assume string with 7bit-encoded int length prefixing the string
        if (lengthInfo == null)
        {
            writer.Write(writeValue);
            return;
        }

        byte[] stringBytes = lengthInfo.Encoding.GetBytes(writeValue);
        stringBytes = ClampBuffer(stringBytes, lengthInfo.Length);

        writer.Write(stringBytes);
    }

    private void WriteList(IList writeValue, IBinaryWriterX writer, LengthInfo lengthInfo, IValueStorage storage)
    {
        if (writeValue.Count != lengthInfo.Length)
            throw new FieldLengthMismatchException(writeValue.Count, lengthInfo.Length);

        var listCounter = 0;
        foreach (object value in writeValue)
            WriteInternal(value, value.GetType(), writer, storage.CreateScope($"[{listCounter++}]"));
    }

    private void WriteComplex(object writeValue, Type writeType, IBinaryWriterX writer, IValueStorage storage)
    {
        BitFieldInfo? bitField = _infoProvider.GetBitFieldInfo(writeType);
        int? alignment = _infoProvider.GetAlignment(writeType);

        if (bitField != null)
            writer.Flush();

        writer.BitOrder = (bitField?.BitOrder != BitOrder.Default ? bitField?.BitOrder : writer.BitOrder) ?? writer.BitOrder;
        writer.BlockSize = bitField?.BlockSize ?? writer.BlockSize;

        if (writer.BlockSize != 8 && writer.BlockSize != 4 && writer.BlockSize != 2 && writer.BlockSize != 1)
            throw new InvalidBitFieldInfoException(writer.BlockSize);

        var fields = writeType.GetFields().OrderBy(fi => fi.MetadataToken);
        foreach (FieldInfo? field in fields)
        {
            // If field condition is false, write no value and ignore field
            ConditionInfo? condition = _infoProvider.GetConditionInfo(field);
            if (!ResolveCondition(condition, storage))
                continue;

            object? fieldValue = field.GetValue(writeValue);
            storage.Set(field.Name, fieldValue);

            int? bitLength = _infoProvider.GetBitLength(field);
            if (bitLength != null)
                writer.WriteBits(Convert.ToInt64(fieldValue), bitLength.Value);
            else
                WriteInternal(fieldValue, field.FieldType, writer, storage, field);
        }

        writer.Flush();

        // Apply alignment
        if (alignment != null)
            writer.WriteAlignment(alignment.Value);
    }

    private bool ResolveCondition(ConditionInfo? condition, IValueStorage storage)
    {
        // If no condition is given, resolve it to true so the field is read
        if (condition == null)
            return true;

        object value = storage.Get(condition.FieldName);
        switch (condition.Comparer)
        {
            case ConditionComparer.Equal:
                return Convert.ToUInt64(value) == condition.Value;

            case ConditionComparer.Greater:
                return Convert.ToUInt64(value) > condition.Value;

            case ConditionComparer.Smaller:
                return Convert.ToUInt64(value) < condition.Value;

            case ConditionComparer.GEqual:
                return Convert.ToUInt64(value) >= condition.Value;

            case ConditionComparer.SEqual:
                return Convert.ToUInt64(value) <= condition.Value;

            default:
                throw new InvalidOperationException($"Unknown comparer {condition.Comparer}.");
        }
    }

    private byte[] ClampBuffer(byte[] input, int length)
    {
        var buffer = new byte[length];

        Array.Copy(input, 0, buffer, 0, Math.Min(length, input.Length));

        return buffer;
    }

    private bool IsList(Type type)
    {
        return type.IsAssignableTo(typeof(IList));
    }

    private bool IsStruct(Type type)
    {
        return type is { IsValueType: true, IsEnum: false };
    }
}
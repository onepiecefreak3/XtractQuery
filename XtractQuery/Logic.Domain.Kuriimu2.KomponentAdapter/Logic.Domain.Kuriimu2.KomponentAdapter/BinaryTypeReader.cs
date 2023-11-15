using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter
{
    internal class BinaryTypeReader : IBinaryTypeReader
    {
        private readonly IValueStorageFactory _storageFactory;
        private readonly IMemberInfoProvider _infoProvider;

        public BinaryTypeReader(IValueStorageFactory storageFactory, IMemberInfoProvider infoProvider)
        {
            _storageFactory = storageFactory;
            _infoProvider = infoProvider;
        }

        public T? Read<T>(IBinaryReaderX reader)
        {
            return (T?)Read(reader, typeof(T));
        }

        public object? Read(IBinaryReaderX reader, Type type)
        {
            IValueStorage storage = _storageFactory.Create();
            return ReadInternal(reader, type, storage);
        }

        public IList<T?> ReadMany<T>(IBinaryReaderX reader, int length)
        {
            var result = new T?[length];
            for (var i = 0; i < length; i++)
                result[i] = Read<T>(reader);

            return result;
        }

        public IList<object?> ReadMany(IBinaryReaderX reader, Type type, int length)
        {
            var result = new object?[length];
            for (var i = 0; i < length; i++)
                result[i] = Read(reader, type);

            return result;
        }

        private object? ReadInternal(IBinaryReaderX reader, Type type, IValueStorage storage, FieldInfo? fieldInfo = null, bool isTypeChosen = false)
        {
            var bkByteOrder = reader.ByteOrder;
            var bkBitOrder = reader.BitOrder;
            var bkBlockSize = reader.BlockSize;

            reader.ByteOrder = _infoProvider.GetByteOrder(fieldInfo, _infoProvider.GetByteOrder(type, reader.ByteOrder));

            object? returnValue = null;
            if (IsTypeChoice(fieldInfo) && !isTypeChosen)
            {
                IList<TypeChoice> typeChoices = _infoProvider.GetTypeChoices(fieldInfo);
                Type chosenType = ChooseType(type, typeChoices, storage);

                returnValue = ReadInternal(reader, chosenType, storage, fieldInfo, true);
            }
            else if (type.IsPrimitive)
            {
                returnValue = ReadTypePrimitive(reader, type);
            }
            else if (type == typeof(string))
            {
                LengthInfo? lengthInfo = _infoProvider.GetLengthInfo(fieldInfo, storage);
                returnValue = ReadTypeString(reader, lengthInfo);
            }
            else if (type == typeof(decimal))
            {
                returnValue = reader.ReadDecimal();
            }
            else if (IsList(type))
            {
                LengthInfo? lengthInfo = _infoProvider.GetLengthInfo(fieldInfo, storage);
                if (lengthInfo != null)
                    returnValue = ReadList(reader, type, lengthInfo, storage, fieldInfo?.Name);
            }
            else if (type.IsClass || IsStruct(type))
            {
                returnValue = ReadComplex(reader, type, storage.CreateScope(fieldInfo?.Name));
            }
            else if (type.IsEnum)
            {
                returnValue = ReadInternal(reader, type.GetEnumUnderlyingType(), storage);
            }
            else throw new InvalidOperationException($"Type {type} is not supported.");

            reader.ByteOrder = bkByteOrder;
            reader.BitOrder = bkBitOrder;
            reader.BlockSize = bkBlockSize;

            return returnValue;
        }

        private object ReadTypePrimitive(IBinaryReaderX reader, Type readType)
        {
            switch (Type.GetTypeCode(readType))
            {
                case TypeCode.Boolean: return reader.ReadBoolean();
                case TypeCode.Byte: return reader.ReadByte();
                case TypeCode.SByte: return reader.ReadSByte();
                case TypeCode.Int16: return reader.ReadInt16();
                case TypeCode.UInt16: return reader.ReadUInt16();
                case TypeCode.Char: return reader.ReadChar();
                case TypeCode.Int32: return reader.ReadInt32();
                case TypeCode.UInt32: return reader.ReadUInt32();
                case TypeCode.Int64: return reader.ReadInt64();
                case TypeCode.UInt64: return reader.ReadUInt64();
                case TypeCode.Single: return reader.ReadSingle();
                case TypeCode.Double: return reader.ReadDouble();
                default: throw new NotSupportedException($"Unsupported primitive {readType.FullName}.");
            }
        }

        private object ReadTypeString(IBinaryReaderX reader, LengthInfo? lengthInfo)
        {
            // If no length attributes are given, assume string with 7bit-encoded int length prefixing the string
            if (lengthInfo == null)
                return reader.ReadString();

            return reader.ReadString(lengthInfo.Length, lengthInfo.Encoding);
        }

        private object ReadList(IBinaryReaderX reader, Type type, LengthInfo lengthInfo, IValueStorage storage, string? listFieldName)
        {
            IList list;
            Type elementType;

            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
                list = Array.CreateInstance(elementType, lengthInfo.Length);
            }
            else
            {
                elementType = type.GetGenericArguments()[0];
                list = (IList)Activator.CreateInstance(type)!;
            }

            for (var i = 0; i < lengthInfo.Length; i++)
            {
                object? elementValue = ReadInternal(reader, elementType, storage.CreateScope($"{listFieldName}[{i}]"));
                if (list.IsFixedSize)
                    list[i] = elementValue;
                else
                    list.Add(elementValue);
            }

            return list;
        }

        private object ReadComplex(IBinaryReaderX reader, Type type, IValueStorage storage)
        {
            BitFieldInfo? bitField = _infoProvider.GetBitFieldInfo(type);
            int? alignment = _infoProvider.GetAlignment(type);

            if (bitField != null)
                reader.ResetBitBuffer();

            reader.BitOrder = (bitField?.BitOrder != BitOrder.Default ? bitField?.BitOrder : reader.BitOrder) ?? reader.BitOrder;
            reader.BlockSize = bitField?.BlockSize ?? reader.BlockSize;

            if (reader.BlockSize != 8 && reader.BlockSize != 4 && reader.BlockSize != 2 && reader.BlockSize != 1)
                throw new InvalidBlockSizeException(reader.BlockSize);

            object item = Activator.CreateInstance(type)!;

            var fields = type.GetFields().OrderBy(fi => fi.MetadataToken);
            foreach (FieldInfo? field in fields)
            {
                // If field condition is false, read no value and leave field to default
                ConditionInfo? condition = _infoProvider.GetConditionInfo(field);
                if (!ResolveCondition(condition, storage))
                    continue;

                int? bitLength = _infoProvider.GetBitLength(field);

                object? fieldValue = bitLength.HasValue ?
                    reader.ReadBits(bitLength.Value) :
                    ReadInternal(reader, field.FieldType, storage, field);

                storage.Set(field.Name, fieldValue);
                field.SetValue(item, Convert.ChangeType(fieldValue, field.FieldType));
            }

            if (alignment != null)
                reader.SeekAlignment(alignment.Value);

            return item;
        }

        private bool IsTypeChoice(MemberInfo? field)
        {
            return _infoProvider.GetTypeChoices(field).Count > 0;
        }

        private Type ChooseType(Type readType, IList<TypeChoice> typeChoices, IValueStorage storage)
        {
            if (readType != typeof(object) && typeChoices.Any(x => !readType.IsAssignableFrom(x.InjectionType)))
                throw new InvalidOperationException($"Not all type choices are injectable to '{readType.Name}'.");

            foreach (var typeChoice in typeChoices)
            {
                if (!storage.Exists(typeChoice.FieldName))
                    throw new InvalidOperationException($"Field '{typeChoice.FieldName}' could not be found.");

                var value = storage.Get(typeChoice.FieldName);
                switch (typeChoice.Comparer)
                {
                    case TypeChoiceComparer.Equal:
                        if (Convert.ToUInt64(value) == Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.Greater:
                        if (Convert.ToUInt64(value) > Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.Smaller:
                        if (Convert.ToUInt64(value) < Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.GEqual:
                        if (Convert.ToUInt64(value) >= Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    case TypeChoiceComparer.SEqual:
                        if (Convert.ToUInt64(value) <= Convert.ToUInt64(typeChoice.Value))
                            return typeChoice.InjectionType;
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown comparer {typeChoice.Comparer}.");
                }
            }

            throw new InvalidOperationException("No choice matched the criteria for injection");
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

        private bool IsList(Type type)
        {
            return type.IsAssignableTo(typeof(IList));
        }

        private bool IsStruct(Type type)
        {
            return type is { IsValueType: true, IsEnum: false };
        }
    }
}

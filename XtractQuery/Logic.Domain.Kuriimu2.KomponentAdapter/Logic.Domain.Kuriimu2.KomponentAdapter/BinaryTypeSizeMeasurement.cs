using System.Collections;
using System.Reflection;
using Komponent.IO;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter
{
    internal class BinaryTypeSizeMeasurement : IBinaryTypeSizeMeasurement
    {
        private readonly IMemberInfoProvider _infoProvider;

        public BinaryTypeSizeMeasurement(IMemberInfoProvider infoProvider)
        {
            _infoProvider = infoProvider;
        }

        public int Measure<T>()
        {
            return Measure(typeof(T));
        }

        public int Measure(Type type)
        {
            return MeasureType(type, null);
        }

        private int MeasureType(Type type, MemberInfo? field)
        {
            if (_infoProvider.GetTypeChoices(field).Any())
                throw new InvalidOperationException("Type choice attributes are not supported for static measurement.");

            if (type.IsPrimitive)
                return MeasurePrimitive(type);

            if (type == typeof(decimal))
                return 16;

            if (type == typeof(string))
                return MeasureString(field, _infoProvider.GetLengthInfoSource(field));

            if (IsList(type))
                return MeasureList(field, type, _infoProvider.GetLengthInfoSource(field));

            if (type.IsClass || IsStruct(type))
                return MeasureComplex(type);

            if (type.IsEnum)
                return Measure(type.GetEnumUnderlyingType());

            throw new UnsupportedTypeException(type);
        }

        private int MeasurePrimitive(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: return 1;
                case TypeCode.Byte: return 1;
                case TypeCode.SByte: return 1;
                case TypeCode.Int16: return 2;
                case TypeCode.UInt16: return 2;
                case TypeCode.Char: return 2;
                case TypeCode.Int32: return 4;
                case TypeCode.UInt32: return 4;
                case TypeCode.Int64: return 8;
                case TypeCode.UInt64: return 8;
                case TypeCode.Single: return 4;
                case TypeCode.Double: return 8;
                default: throw new NotSupportedException($"Unsupported primitive type {type.Name}.");
            }
        }

        private int MeasureString(MemberInfo? field, LengthInfoSource? source)
        {
            if (source == LengthInfoSource.Variable)
                throw new InvalidOperationException("Variable size attributes are not supported for static measurement.");
            if (source == LengthInfoSource.Calculation)
                throw new InvalidOperationException("Calculated size attributes are not supported for static measurement.");
            if (source != LengthInfoSource.Fixed)
                throw new InvalidOperationException("Strings without set length are not supported for static measurement.");

            return _infoProvider.GetLengthInfo(field, null)!.Length;
        }

        private int MeasureList(MemberInfo? field, Type fieldType, LengthInfoSource? source)
        {
            if (source == LengthInfoSource.Variable)
                throw new InvalidOperationException("Variable size attributes are not supported for static measurement.");
            if (source == LengthInfoSource.Calculation)
                throw new InvalidOperationException("Calculated size attributes are not supported for static measurement.");
            if (source != LengthInfoSource.Fixed)
                throw new InvalidOperationException("Lists without set length are not supported for static measurement.");

            Type? elementType = fieldType.IsArray ? fieldType.GetElementType() : fieldType.GetGenericArguments()[0];

            return _infoProvider.GetLengthInfo(field, null)!.Length * Measure(elementType!);
        }

        private int MeasureComplex(Type type)
        {
            var totalLength = 0;
            foreach (FieldInfo? field in type.GetFields().OrderBy(fi => fi.MetadataToken))
                totalLength += MeasureType(field.FieldType, field.CustomAttributes.Any() ? field : null);
            
            return totalLength;
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

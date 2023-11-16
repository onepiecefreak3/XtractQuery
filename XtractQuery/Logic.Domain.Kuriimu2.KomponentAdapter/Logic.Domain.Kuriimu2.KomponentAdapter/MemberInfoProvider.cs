using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter
{
    internal class MemberInfoProvider : IMemberInfoProvider
    {
        public ByteOrder GetByteOrder(MemberInfo? member, ByteOrder defaultByteOrder)
        {
            return member?.GetCustomAttribute<EndiannessAttribute>()?.ByteOrder ?? defaultByteOrder;
        }

        public IList<TypeChoice> GetTypeChoices(MemberInfo? member)
        {
            if (member == null)
                return Array.Empty<TypeChoice>();

            var typeChoices = new List<TypeChoice>();
            foreach (var typeChoice in member.GetCustomAttributes<TypeChoiceAttribute>())
                typeChoices.Add(new TypeChoice(typeChoice.FieldName, typeChoice.Comparer, typeChoice.Value, typeChoice.InjectionType));

            return typeChoices;
        }

        public LengthInfoSource? GetLengthInfoSource(MemberInfo? member)
        {
            var fixedLengthAttribute = member?.GetCustomAttribute<FixedLengthAttribute>();
            var variableLengthAttribute = member?.GetCustomAttribute<VariableLengthAttribute>();
            var calculatedLengthAttribute = member?.GetCustomAttribute<CalculateLengthAttribute>();

            if (fixedLengthAttribute != null)
                return LengthInfoSource.Fixed;

            if (variableLengthAttribute != null)
                return LengthInfoSource.Variable;

            if (calculatedLengthAttribute != null)
                return LengthInfoSource.Calculation;

            return null;
        }

        public LengthInfo? GetLengthInfo(MemberInfo? member, IValueStorage? values)
        {
            var fixedLengthAttribute = member?.GetCustomAttribute<FixedLengthAttribute>();
            var variableLengthAttribute = member?.GetCustomAttribute<VariableLengthAttribute>();
            var calculatedLengthAttribute = member?.GetCustomAttribute<CalculateLengthAttribute>();

            Encoding encoding;
            int length;

            if (fixedLengthAttribute != null)
            {
                encoding = GetEncoding(fixedLengthAttribute.StringEncoding);
                length = fixedLengthAttribute.Length;
            }
            else if (variableLengthAttribute != null)
            {
                if (values == null)
                    throw new InvalidOperationException("Value storage is necessary to determine variable length.");

                encoding = GetEncoding(variableLengthAttribute.StringEncoding);
                length = Convert.ToInt32(values.Get(variableLengthAttribute.FieldName)) + variableLengthAttribute.Offset;
            }
            else if (calculatedLengthAttribute != null)
            {
                if (values == null)
                    throw new InvalidOperationException("Value storage is necessary to calculate length.");

                encoding = GetEncoding(calculatedLengthAttribute.StringEncoding);
                length = ResolveCalculateLengthAttributeMethod(calculatedLengthAttribute)(values);
            }
            else
            {
                return null;
            }

            return new LengthInfo(length, encoding);
        }

        public BitFieldInfo? GetBitFieldInfo(MemberInfo? member)
        {
            var bitFieldInfoAttribute = member?.GetCustomAttribute<BitFieldInfoAttribute>();
            if (bitFieldInfoAttribute == null)
                return null;

            return new BitFieldInfo
            {
                BitOrder = bitFieldInfoAttribute.BitOrder,
                BlockSize = bitFieldInfoAttribute.BlockSize
            };
        }

        public int? GetBitLength(MemberInfo? member)
        {
            return member?.GetCustomAttribute<BitFieldAttribute>()?.BitLength;
        }

        public int? GetAlignment(MemberInfo? member)
        {
            return member?.GetCustomAttribute<AlignmentAttribute>()?.Alignment;
        }

        public ConditionInfo? GetConditionInfo(MemberInfo? member)
        {
            var conditionAttribute = member?.GetCustomAttribute<ConditionAttribute>();
            if (conditionAttribute == null)
                return null;

            return new ConditionInfo(conditionAttribute.FieldName, conditionAttribute.Comparer,
                conditionAttribute.Value);
        }

        private Encoding GetEncoding(StringEncoding encoding)
        {
            switch (encoding)
            {
                case StringEncoding.ASCII:
                    return Encoding.ASCII;
                case StringEncoding.UTF7:
                    return Encoding.UTF7;
                case StringEncoding.UTF8:
                    return Encoding.UTF8;
                case StringEncoding.UTF16:
                    return Encoding.Unicode;
                case StringEncoding.Unicode:
                    return Encoding.Unicode;
                case StringEncoding.UTF32:
                    return Encoding.UTF32;
                case StringEncoding.SJIS:
                    return Encoding.GetEncoding("SJIS");
                default:
                    throw new InvalidOperationException($"Unknown string encoding {encoding}.");
            }
        }

        private readonly Dictionary<(Type, string), Func<IValueStorage, int>> _calculateMethodCache = new();

        private Func<IValueStorage, int> ResolveCalculateLengthAttributeMethod(CalculateLengthAttribute attribute)
        {
            (Type, string) cacheKey = (attribute.CalculationType, attribute.CalculationMethodName);
            if (_calculateMethodCache.TryGetValue(cacheKey, out Func<IValueStorage, int>? calculateMethod))
                return calculateMethod;

            if (!attribute.CalculationType.IsClass)
                throw new InvalidOperationException("Type needs to be a class.");

            MethodInfo? method = attribute.CalculationType.GetMethod(attribute.CalculationMethodName);
            if (method == null)
                throw new InvalidOperationException($"Class does not contain a method '{attribute.CalculationMethodName}'.");

            ParameterInfo[] methodParameters = method.GetParameters();
            if (method.ReturnType != typeof(int) ||
                methodParameters.Length != 1 ||
                !methodParameters[0].ParameterType.IsAssignableTo(typeof(IValueStorage)))
                throw new InvalidOperationException($"Method is not of form 'int {attribute.CalculationMethodName}({nameof(IValueStorage)})'.");

            if (attribute.CalculationType is { IsAbstract: true, IsSealed: true })
            {
                // If class is static
                return _calculateMethodCache[cacheKey] = storage => (int)method.Invoke(null, new object[] { storage })!;
            }
            else
            {
                // If class has to be instantiated
                if (attribute.CalculationType.GetConstructors().All(x => x.GetParameters().Length != 0))
                    throw new InvalidOperationException("Class needs to have an empty constructor.");

                object? classInstance = Activator.CreateInstance(attribute.CalculationType);
                return _calculateMethodCache[cacheKey] = storage => (int)method.Invoke(classInstance, new object[] { storage })!;
            }
        }
    }
}

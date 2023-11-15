using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.Exceptions;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract
{
    [MapException(typeof(MemberInfoProviderException))]
    public interface IMemberInfoProvider
    {
        ByteOrder GetByteOrder(MemberInfo? member, ByteOrder defaultByteOrder);

        IList<TypeChoice> GetTypeChoices(MemberInfo? member);

        LengthInfoSource? GetLengthInfoSource(MemberInfo? member);
        LengthInfo? GetLengthInfo(MemberInfo? member, IValueStorage? values);

        BitFieldInfo? GetBitFieldInfo(MemberInfo? member);
        int? GetBitLength(MemberInfo? member);
        int? GetAlignment(MemberInfo? member);

        ConditionInfo? GetConditionInfo(MemberInfo? member);
    }
}

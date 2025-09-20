using System.Runtime.Serialization;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.Exceptions;

internal class MemberInfoProviderException:Exception
{
    public MemberInfoProviderException()
    {
    }

    public MemberInfoProviderException(string message) : base(message)
    {
    }

    public MemberInfoProviderException(string message, Exception inner) : base(message, inner)
    {
    }

    protected MemberInfoProviderException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}
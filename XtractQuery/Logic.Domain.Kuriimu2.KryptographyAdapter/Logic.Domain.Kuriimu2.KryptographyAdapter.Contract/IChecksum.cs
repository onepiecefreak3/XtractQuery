using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Contract
{
    [MapException(typeof(ChecksumException))]
    public interface IChecksum
    {
        byte[] Compute(string input);
        byte[] Compute(string input, Encoding enc);
        byte[] Compute(Stream input);
        byte[] Compute(Span<byte> input);
    }

    [MapException(typeof(ChecksumException))]
    public interface IChecksum<out T> : IChecksum
    {
        T ComputeValue(string input);
        T ComputeValue(string input, Encoding enc);
        T ComputeValue(Stream input);
        T ComputeValue(Span<byte> input);
    }
}

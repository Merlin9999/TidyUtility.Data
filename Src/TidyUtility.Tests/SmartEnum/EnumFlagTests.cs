using FluentAssertions;
using TidyUtility.Core.Extensions;

namespace TidyUtility.Tests.SmartEnum;

public class EnumFlagTests
{
    [Flags]
    public enum EFlag : long
    {
        None = 0,
        One = 0x1,
        Two = 0x2,
        Four = 0x4,
        Eight = 0x8,
        Sixteen = 0x10,
    }

    [Fact]
    public void AsSeparatedFlagsReturnsRecognizedValues()
    {
        EFlag[] expected = new[] { EFlag.One, EFlag.Four };
        EFlag[] actual = (EFlag.One | EFlag.Four).AsSeparatedFlags().ToArray();
        
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AsSeparatedFlagsReturnsUnrecognizedValues()
    {
        EFlag[] expected = new[] { EFlag.One, EFlag.Four, (EFlag)0x20 };
        EFlag[] actual = (EFlag.One | EFlag.Four | (EFlag)0x20).AsSeparatedFlags().ToArray();
        
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AsSeparatedFlagsReturnsUnrecognizedMostSignificantBit()
    {
        unchecked
        {
            EFlag[] expected = new[] { EFlag.One, EFlag.Four, (EFlag)0x8000000000000000 };
            EFlag[] actual = (EFlag.One | EFlag.Four | (EFlag)0x8000000000000000).AsSeparatedFlags().ToArray();
            
            actual.Should().BeEquivalentTo(expected);

            //string asdf = $"{(ulong)(actual[2]):X}";
        }
    }
}
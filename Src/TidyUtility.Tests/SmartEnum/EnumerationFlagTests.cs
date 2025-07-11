using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TidyUtility.Data.Json;
using TidyUtility.Data.SmartEnum;
using static TidyUtility.Tests.SmartEnum.EnumerationTests;

namespace TidyUtility.Tests.SmartEnum
{
    public class EnumerationFlagTests
    {
        public class TestFlagEnum : EnumerationFlag<TestFlagEnum>
        {
            public static readonly TestFlagEnum None = new TestFlagEnum(0, nameof(None));
            public static readonly TestFlagEnum One = new TestFlagEnum(0x01, nameof(One));
            public static readonly TestFlagEnum Two = new TestFlagEnum(0x02, nameof(Two));
            public static readonly TestFlagEnum Four = new TestFlagEnum(0x04, nameof(Four));
            public static readonly TestFlagEnum Eight = new TestFlagEnum(0x08, nameof(Eight));
            public static readonly TestFlagEnum Sixteen = new TestFlagEnum(0x10, nameof(Sixteen));
            public static readonly TestFlagEnum MaxBit = new TestFlagEnum(0x8000000000000000, nameof(MaxBit));

            protected TestFlagEnum(ulong value, string name) : base(value, name) { }
            protected TestFlagEnum(ulong value) : base(value) { }
        }

        [Flags]
        public enum ETestFlagEnum
        {
            None = 0,
            One = 0x1,
            Two = 0x2,
            Four = 0x4,
            Eight = 0x8,
            Sixteen = 0x10,
        }

        public class ClassWithFlagEnumeration
        {
            public TestFlagEnum EnumValue { get; init; } = TestFlagEnum.One;
        }

        public class ClassWithFlagEnum
        {
            public ETestFlagEnum EnumValue { get; init; }
        }

        public class ClassWithFlagEnumerationAsString
        {
            [JsonConverter(typeof(EnumerationAsStringConverter))]
            public TestFlagEnum EnumValue { get; init; } = TestFlagEnum.One;
        }

        public class ClassWithFlagEnumAsString
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public ETestFlagEnum EnumValue { get; init; }
        }

        public class ClassWithEnumerationAndFlagEnumeration
        {
            public TestEnum EnumValue { get; init; } = TestEnum.One;
            public TestFlagEnum FlagEnumValue { get; init; } = TestFlagEnum.One;
        }

        [Fact]
        public void SerializeToValue()
        {
            var expected = new ClassWithFlagEnumeration() { EnumValue = TestFlagEnum.Four };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithFlagEnumeration>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().BeSameAs(expected.EnumValue);
            serialized.Should().Be(@"{""EnumValue"":0x4}");
        }

        [Fact]
        public void SerializeMaxBitToValue()
        {
            var expected = new ClassWithFlagEnumeration() { EnumValue = TestFlagEnum.MaxBit };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithFlagEnumeration>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().BeSameAs(expected.EnumValue);
            serialized.Should().Be(@"{""EnumValue"":0x8000000000000000}");
        }

        [Fact]
        public void SerializeMaxBitToString()
        {
            var expected = new ClassWithFlagEnumerationAsString() { EnumValue = TestFlagEnum.MaxBit };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithFlagEnumerationAsString>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().BeSameAs(expected.EnumValue);
            serialized.Should().Be(@"{""EnumValue"":""MaxBit""}");
        }

        [Fact]
        public void SerializeMaxBitWithAnotherFlagToString()
        {
            var expected = new ClassWithFlagEnumeration() { EnumValue = TestFlagEnum.One | TestFlagEnum.MaxBit };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithFlagEnumeration>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().Be(expected.EnumValue);
            serialized.Should().Be(@"{""EnumValue"":0x8000000000000001}");
        }

        [Fact]
        public void SerializeAsMultipleFlags()
        {
            var expected = new ClassWithFlagEnumeration() { EnumValue = TestFlagEnum.Four | TestFlagEnum.Sixteen | (TestFlagEnum)0x4000000000000000 };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithFlagEnumeration>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().BeEquivalentTo(expected.EnumValue);
            actual.EnumValue.AsSeparatedFlags().Should().BeEquivalentTo(expected.EnumValue.AsSeparatedFlags());
            serialized.Should().Be(@"{""EnumValue"":0x4000000000000014}");
        }
        [Fact]
        public void SerializeAsMultipleFlagsAsString()
        {
            var expected = new ClassWithFlagEnumerationAsString() { EnumValue = TestFlagEnum.Four | TestFlagEnum.Sixteen };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithFlagEnumerationAsString>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().BeEquivalentTo(expected.EnumValue);
            actual.EnumValue.AsSeparatedFlags().Should().BeEquivalentTo(expected.EnumValue.AsSeparatedFlags());
            serialized.Should().Be(@"{""EnumValue"":""Four, Sixteen""}");
        }

        [Fact]
        public void SerializeAsMultipleFlagsAsStringWithUnnamedFlag()
        {
            var expected = new ClassWithFlagEnumerationAsString() { EnumValue = TestFlagEnum.Four | TestFlagEnum.Sixteen | (TestFlagEnum)0x4000000000000000 };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithFlagEnumerationAsString>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().BeEquivalentTo(expected.EnumValue);
            actual.EnumValue.AsSeparatedFlags().Should().BeEquivalentTo(expected.EnumValue.AsSeparatedFlags());
            serialized.Should().Be(@"{""EnumValue"":0x4000000000000014}");
        }

        [Fact]
        public void SerializeUsingCachingInEnumerationConverter()
        {
            var expected = new List<ClassWithEnumerationAndFlagEnumeration>()
            {
                {new ClassWithEnumerationAndFlagEnumeration() {EnumValue = EnumerationTests.TestEnum.Two, FlagEnumValue = TestFlagEnum.Eight}},
                {new ClassWithEnumerationAndFlagEnumeration() {EnumValue = EnumerationTests.TestEnum.Three, FlagEnumValue = TestFlagEnum.Two | TestFlagEnum.Sixteen}},
            };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<List<ClassWithEnumerationAndFlagEnumeration>>(serialized);

            actual.Should().BeEquivalentTo(expected);
            serialized.Should().Be(@"[{""EnumValue"":""Two"",""FlagEnumValue"":0x8},{""EnumValue"":""Three"",""FlagEnumValue"":0x12}]");
        }

        [Fact]
        public void VerifyFlagEnumAndFlagEnumerationSerializesAsIntAreCompatible()
        {
            var expectedSingleValue = new ClassWithFlagEnumeration() { EnumValue = TestFlagEnum.Eight };
            var expectedOrdValue = new ClassWithFlagEnumeration() { EnumValue = TestFlagEnum.Four | TestFlagEnum.Sixteen };
            var expected = (Item1: expectedSingleValue, Item2: expectedOrdValue);

            var ser = new SafeJsonDotNetSerializer();
            string serializedEnumeration = ser.Serialize(expected);
            var deserializedEnum = ser.Deserialize<(ClassWithFlagEnum, ClassWithFlagEnum)>(serializedEnumeration);
            string serializedEnum = ser.Serialize(deserializedEnum);
            var deserializedEnumeration = ser.Deserialize<(ClassWithFlagEnumeration, ClassWithFlagEnumeration)>(serializedEnum);

            deserializedEnumeration.Should().BeEquivalentTo(expected);
            deserializedEnumeration.Item1.Should().BeEquivalentTo(expected.Item1);
            deserializedEnumeration.Item2.Should().BeEquivalentTo(expected.Item2);
        }

        [Fact]
        public void VerifyFlagEnumAndFlagEnumerationSerializesAsStringAreCompatible()
        {
            var expectedSingleValue = new ClassWithFlagEnumerationAsString() { EnumValue = TestFlagEnum.Eight };
            var expectedOrdValue = new ClassWithFlagEnumerationAsString() { EnumValue = TestFlagEnum.Four | TestFlagEnum.Sixteen };
            var expected = (Item1: expectedSingleValue, Item2: expectedOrdValue);

            var ser = new SafeJsonDotNetSerializer();
            string serializedEnumeration = ser.Serialize(expected);
            var deserializedEnum = ser.Deserialize<(ClassWithFlagEnumAsString, ClassWithFlagEnumAsString)>(serializedEnumeration);
            string serializedEnum = ser.Serialize(deserializedEnum);
            var deserializedEnumeration = ser.Deserialize<(ClassWithFlagEnumerationAsString, ClassWithFlagEnumerationAsString)>(serializedEnum);

            deserializedEnumeration.Should().BeEquivalentTo(expected);
            deserializedEnumeration.Item1.Should().BeEquivalentTo(expected.Item1);
            deserializedEnumeration.Item2.Should().BeEquivalentTo(expected.Item2);
        }
    }
}

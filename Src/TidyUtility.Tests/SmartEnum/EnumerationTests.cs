using System.Collections.Immutable;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TidyUtility.Data.Json;
using TidyUtility.Data.SmartEnum;

namespace TidyUtility.Tests.SmartEnum
{
    public class EnumerationTests
    {
        public class BadEnumTypeWithPublicConstructor : Enumeration<BadEnumTypeWithPublicConstructor>
        {
            public static readonly BadEnumTypeWithPublicConstructor One = new BadEnumTypeWithPublicConstructor(1, "One");

            public BadEnumTypeWithPublicConstructor(long value, string name) : base(value, name) { }
        }

        public class DupValueErrorEnum : Enumeration<DupValueErrorEnum>
        {
            public static readonly DupValueErrorEnum One = new DupValueErrorEnum(0, nameof(One));
            public static readonly DupValueErrorEnum Two = new DupValueErrorEnum(1, nameof(Two));
            public static readonly DupValueErrorEnum Three = new DupValueErrorEnum(1, nameof(Three));

            protected DupValueErrorEnum(long value, string name) : base(value, name) { }
        }

        public class DupNameErrorEnum : Enumeration<DupNameErrorEnum>
        {
            public static readonly DupNameErrorEnum One = new DupNameErrorEnum(0, nameof(One));
            public static readonly DupNameErrorEnum Two = new DupNameErrorEnum(1, nameof(Two));
            public static readonly DupNameErrorEnum Three = new DupNameErrorEnum(2, nameof(Two));

            protected DupNameErrorEnum(long value, string name) : base(value, name) { }
        }

        public class FieldNotReadonlyErrorEnum : Enumeration<FieldNotReadonlyErrorEnum>
        {
            public static FieldNotReadonlyErrorEnum One = new FieldNotReadonlyErrorEnum(0, nameof(One));
            public static readonly FieldNotReadonlyErrorEnum Two = new FieldNotReadonlyErrorEnum(1, nameof(Two));
            public static FieldNotReadonlyErrorEnum Three = new FieldNotReadonlyErrorEnum(2, nameof(Three));

            protected FieldNotReadonlyErrorEnum(long value, string name) : base(value, name) { }
        }

        [JsonConverter(typeof(EnumerationAsStringConverter))]
        public class TestEnum : Enumeration<TestEnum>
        {
            public static readonly TestEnum One = new TestEnum(1, nameof(One));
            public static readonly TestEnum Two = new TestEnum(2, nameof(Two));
            public static readonly TestEnum Three = new TestEnum(3, nameof(Three));

            protected TestEnum(long value, string name) : base(value, name) { }
        }

        public class ClassWithEnumerationAsString
        {
            [JsonConverter(typeof(EnumerationAsStringConverter))]
            public TestEnum? EnumValue { get; init; }
        }

        public class ClassWithEnumeration
        {
            public TestEnum? EnumValue { get; init; }
        }

        public class ClassWithEnumAsString
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public ETestEnum? EnumValue { get; init; }
        }

        public class ClassWithEnum
        {
            public ETestEnum? EnumValue { get; init; }
        }

        public enum ETestEnum
        {
            One = 1,
            Two = 2,
            Three = 3,
        }

        public class ClassWithBothEnumerations
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public ETestEnum ETestEnumAsString { get; set; }
            public ETestEnum ETestEnumAsInt { get; set; }

            [JsonConverter(typeof(EnumerationAsStringConverter))]
            public TestEnum TestEnumAsString { get; set; } = TestEnum.One;
            public TestEnum TestEnumAsInt { get; set; } = TestEnum.One;
        }

        //public record PropertyGetterReconstructsEnum : Enumeration<PropertyGetterReconstructsEnum>
        //{
        //    public static PropertyGetterReconstructsEnum One => new PropertyGetterReconstructsEnum(0, nameof(One));

        //    protected PropertyGetterReconstructsEnum(int value, string name) : base(value, name) { }
        //}

        [Fact]
        public void VerifyPublicConstructorThrowsException()
        {
            Func<IEnumerable<BadEnumTypeWithPublicConstructor>> func = () => BadEnumTypeWithPublicConstructor.GetAll();
            func.Should().Throw<EnumerationInitException>()
                .WithMessage("Constructors * must NOT be public! Protected or Private constructors ONLY to preserve design.");
        }

        [Fact]
        public void VerifyGetAllThrowsExceptionIfDupValue()
        {
            Func<IEnumerable<DupValueErrorEnum>> func = () => DupValueErrorEnum.GetAll();
            func.Should().Throw<EnumerationInitException>()
                .WithMessage("Found one or more enumeration instances for type * with the same value when initializing the *");
        }

        [Fact]
        public void VerifyGetAllThrowsExceptionIfDupName()
        {
            Func<IEnumerable<DupNameErrorEnum>> func = () => DupNameErrorEnum.GetAll();
            func.Should().Throw<EnumerationInitException>()
                .WithMessage("Found one or more enumeration instances for type * with the same name when initializing the *");
        }

        [Fact]
        public void VerifyCheckErrorIfEnumFieldIsNotReadonly()
        {
            Func<IEnumerable<FieldNotReadonlyErrorEnum>> func = () => FieldNotReadonlyErrorEnum.GetAll();
            func.Should().Throw<EnumerationInitException>()
                .WithMessage("Found enumeration static fields for type * NOT marked as readonly including: *");
        }

        //[Fact]
        //public void VerifyNoReconstruction()
        //{
        //    Func<IEnumerable<PropertyGetterReconstructsEnum>> func = () => PropertyGetterReconstructsEnum.GetAll();
        //    func.Should().Throw<EnumerationInitException>()
        //        .WithMessage("Found enumeration static properties for type * that reconstruct on each property get call including: *");
        //}

        //[Fact]
        //public void VerifyNoPropertySetter()
        //{
        //    Func<IEnumerable<PropertySetterErrorEnum>> func = () => PropertySetterErrorEnum.GetAll();
        //    func.Should().Throw<EnumerationInitException>()
        //        .WithMessage("Found enumeration static properties for type * that have a setter including: *");
        //}

        [Fact]
        public void VerifyCanLoadFields()
        {
            ImmutableList<TestEnum> dowValues = TestEnum.GetAll().ToImmutableList();

            dowValues.Should().HaveCount(3);
            dowValues.Should().Contain(TestEnum.One);
            dowValues.Should().Contain(TestEnum.Two);
            dowValues.Should().Contain(TestEnum.Three);
        }

        //[Fact]
        //public void VerifyCanLoadProperties()
        //{
        //    ImmutableList<PropertyTestEnum> dowValues = PropertyTestEnum.GetAll().ToImmutableList();

        //    dowValues.Should().HaveCount(7);
        //    dowValues.Should().Contain(PropertyTestEnum.Sunday);
        //    dowValues.Should().Contain(PropertyTestEnum.Monday);
        //    dowValues.Should().Contain(PropertyTestEnum.Tuesday);
        //    dowValues.Should().Contain(PropertyTestEnum.Wednesday);
        //    dowValues.Should().Contain(PropertyTestEnum.Thursday);
        //    dowValues.Should().Contain(PropertyTestEnum.Friday);
        //    dowValues.Should().Contain(PropertyTestEnum.Saturday);
        //}

        [Fact]
        public void VerifyFromValue()
        {
            TestEnum three = TestEnum.FromValue(3);
            three.Should().BeEquivalentTo(TestEnum.Three);
        }

        [Fact]
        public void VerifyFromName()
        {
            TestEnum two = TestEnum.FromName(nameof(TestEnum.Two));
            two.Should().BeEquivalentTo(TestEnum.Two);
        }

        [Fact]
        public void SerializeToName()
        {
            var expected = new ClassWithEnumeration() { EnumValue = TestEnum.Three };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithEnumeration>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.EnumValue.Should().BeSameAs(expected.EnumValue);
            serialized.Should().Be(@"{""EnumValue"":""Three""}");
        }

        [Fact]
        public void SerializingEnumVsEnumeration()
        {
            var expected = new ClassWithBothEnumerations()
            {
                ETestEnumAsString = ETestEnum.Two,
                ETestEnumAsInt = ETestEnum.Two,

                TestEnumAsString = TestEnum.Two,
                TestEnumAsInt = TestEnum.Two,
            };

            var ser = new SafeJsonDotNetSerializer();
            string serialized = ser.Serialize(expected);
            var actual = ser.Deserialize<ClassWithBothEnumerations>(serialized);

            actual.Should().BeEquivalentTo(expected);
            actual.ETestEnumAsString.Should().Be(expected.ETestEnumAsString);
            actual.ETestEnumAsInt.Should().Be(expected.ETestEnumAsInt);
            actual.TestEnumAsString.Should().BeSameAs(expected.TestEnumAsString);
            actual.TestEnumAsInt.Should().BeSameAs(expected.TestEnumAsInt);
        }

        [Fact]
        public void VerifyEnumAndEnumerationSerializesAsIntAreCompatible()
        {
            var expected = new ClassWithEnumeration() { EnumValue = TestEnum.Two };

            var ser = new SafeJsonDotNetSerializer();
            string serializedEnumeration = ser.Serialize(expected);
            var deserializedEnum = ser.Deserialize<ClassWithEnum>(serializedEnumeration);
            string serializedEnum = ser.Serialize(deserializedEnum);
            var deserializedEnumeration = ser.Deserialize<ClassWithEnumeration>(serializedEnum);

            deserializedEnumeration.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void VerifyEnumAndEnumerationSerializesAsStringAreCompatible()
        {
            var expected = new ClassWithEnumerationAsString() { EnumValue = TestEnum.Two };

            var ser = new SafeJsonDotNetSerializer();
            string serializedEnumeration = ser.Serialize(expected);
            var deserializedEnum = ser.Deserialize<ClassWithEnumAsString>(serializedEnumeration);
            string serializedEnum = ser.Serialize(deserializedEnum);
            var deserializedEnumeration = ser.Deserialize<ClassWithEnumerationAsString>(serializedEnum);

            deserializedEnumeration.Should().BeEquivalentTo(expected);
        }
    }
}

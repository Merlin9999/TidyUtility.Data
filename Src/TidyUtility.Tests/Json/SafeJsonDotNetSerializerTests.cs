 #nullable disable
 using FluentAssertions;
 using Newtonsoft.Json;
 using TidyUtility.Data.Json;

 namespace TidyUtility.Tests.Json
{
    public class SafeJsonDotNetSerializerTests
    {
        [Fact]
        public void Basic()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new DerivedRegistered()
            {
                BaseStr = "base",
                DerivedStr = "derived",
            };

            string serializedDerived = ser.Serialize(derived);
            DerivedRegistered deserializedDerived = ser.Deserialize<DerivedRegistered>(serializedDerived);

            deserializedDerived.Should().BeEquivalentTo(derived);
        }

        [Fact]
        public void DerivedRegistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new DerivedRegistered()
            {
                BaseStr = "base",
                DerivedStr = "derived",
            };

            string serializedBase = ser.Serialize<RootNoInclude>(derived);
            RootNoInclude deserializedBase = ser.Deserialize<RootNoInclude>(serializedBase);

            deserializedBase.Should().BeEquivalentTo(derived);
        }

        [Fact]
        public void DerivedUnregistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new DerivedUnregistered()
            {
                BaseStr = "base",
                DerivedStr = "derived",
            };

            string serializedBase = ser.Serialize<RootNoInclude>(derived);

            Action action = () => ser.Deserialize<RootNoInclude>(serializedBase);

            action.Should().Throw<JsonSerializationException>()
                .Where(exc =>
                    exc.Message.StartsWith("Type specified in JSON 'DerivedUnregistered' was not resolved."));
        }

        [Fact]
        public void DerivedAutoRegistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new RootDerivedAutoRegistered()
            {
                BaseStr = "base",
                DerivedStr = "derived",
                NestedBase = new NestedBase()
                {
                    NestedBaseStr = "nested base",
                },
            };

            string serializedBase = ser.Serialize<RootIncludeDerived>(derived);
            RootIncludeDerived deserializedBase = ser.Deserialize<RootIncludeDerived>(serializedBase);

            deserializedBase.Should().BeEquivalentTo(derived);
        }

        [Fact]
        public void NestedDerivedRegistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new RootDerivedAutoRegistered()
            {
                BaseStr = "base",
                DerivedStr = "derived",
                NestedBase = new NestedDerivedRegistered()
                {
                    NestedBaseStr = "nested base",
                    NestedDerivedStr = "nested derived",
                },
            };

            string serializedBase = ser.Serialize<RootIncludeDerived>(derived);
            RootIncludeDerived deserializedBase = ser.Deserialize<RootIncludeDerived>(serializedBase);

            deserializedBase.Should().BeEquivalentTo(derived);
        }

        [Fact]
        public void NestedDerivedUnregistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new RootDerivedAutoRegistered()
            {
                BaseStr = "base",
                DerivedStr = "derived",
                NestedBase = new NestedDerivedUnregistered()
                {
                    NestedBaseStr = "nested base",
                    NestedDerivedStr = "nested derived",
                },
            };

            string serializedBase = ser.Serialize<RootIncludeDerived>(derived);

            Action action = () => ser.Deserialize<RootIncludeDerived>(serializedBase);

            action.Should().Throw<JsonSerializationException>()
                .Where(exc =>
                    exc.Message.StartsWith("Type specified in JSON 'NestedDerivedUnregistered' was not resolved."));
        }

        [Fact]
        public void DerivedAndNestedDerivedRegistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new RootDerivedAutoRegistered2()
            {
                BaseStr = "base",
                DerivedStr = "derived",
                NestedBase = new NestedDerivedAutoRegistered()
                {
                    NestedBaseStr = "nested base",
                    NestedDerivedStr = "nested derived",
                },
                ExcludeDerived = null,
            };

            string serializedBase = ser.Serialize<RootIncludeNestedDerived>(derived);
            RootIncludeNestedDerived deserializedBase = ser.Deserialize<RootIncludeNestedDerived>(serializedBase);

            deserializedBase.Should().BeEquivalentTo(derived);
        }

        [Fact]
        public void ExcludedNestedDerived()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new RootDerivedAutoRegistered2()
            {
                BaseStr = "base",
                DerivedStr = "derived",
                NestedBase = null,
                ExcludeDerived = new ExcludedDerived()
                {
                    NestedBaseStr = "nested base",
                    NestedDerivedStr = "nested derived",
                    NestedBase = null,
                },
            };

            string serializedBase = ser.Serialize<RootIncludeNestedDerived>(derived);
            Action action = () => ser.Deserialize<RootIncludeNestedDerived>(serializedBase);

            action.Should().Throw<JsonSerializationException>()
                .Where(exc =>
                    exc.Message.StartsWith("Type specified in JSON 'ExcludedDerived' was not resolved."));
        }

        [Fact]
        public void BaseOfExcludedNestedDerivedStillRegistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new RootDerivedAutoRegistered2()
            {
                BaseStr = "base",
                DerivedStr = "derived",
                NestedBase = null,
                ExcludeDerived = new ExcludeDerivedBase()
                {
                    NestedBaseStr = "nested base",
                    NestedBase = new NestedBase3()
                    {
                        NestedBaseStr = "Another nested base!"
                    },
                },
            };

            string serializedBase = ser.Serialize<RootIncludeNestedDerived>(derived);
            RootIncludeNestedDerived deserializedBase = ser.Deserialize<RootIncludeNestedDerived>(serializedBase);

            deserializedBase.Should().BeEquivalentTo(derived);
        }

        [Fact]
        public void DerivedOfExcludedNestedDerivedNotRegistered()
        {
            var ser = new SafeJsonDotNetSerializer();

            var derived = new RootDerivedAutoRegistered2()
            {
                BaseStr = "base",
                DerivedStr = "derived",
                NestedBase = null,
                ExcludeDerived = new ExcludeDerivedBase()
                {
                    NestedBaseStr = "nested base",
                    NestedBase = new ExcludedNestedDerived()
                    {
                        NestedBaseStr = "Another nested base!",
                        NestedDerivedStr = "Another nested derived!",
                    },
                },
            };

            string serializedBase = ser.Serialize<RootIncludeNestedDerived>(derived);
            Action action = () => ser.Deserialize<RootIncludeNestedDerived>(serializedBase);

            action.Should().Throw<JsonSerializationException>()
                .Where(exc =>
                    exc.Message.StartsWith("Type specified in JSON 'ExcludedNestedDerived' was not resolved."));
        }
    }

    #region Test Types - RootNoInclude - All derived type are explicitely attributed

    [SafeToSerialize]
    public class RootNoInclude
    {
        public string BaseStr { get; set; }
    }

    [SafeToSerialize]
    public class DerivedRegistered : RootNoInclude
    {
        public string DerivedStr { get; set; }
    }

    public class DerivedUnregistered : RootNoInclude
    {
        public string DerivedStr { get; set; }
    }

    #endregion
    #region Test Types - RootIncludeDerived - Does NOT include derived types of nested properties

    [SafeToSerialize(IncludeDerived = true)]
    public class RootIncludeDerived
    {
        public string BaseStr { get; set; }
    }

    public class RootDerivedAutoRegistered : RootIncludeDerived
    {
        public string DerivedStr { get; set; }
        public NestedBase NestedBase { get; set; }
    }

    public class NestedBase
    {
        public string NestedBaseStr { get; set; }
    }

    [SafeToSerialize]
    public class NestedDerivedRegistered : NestedBase
    {
        public string NestedDerivedStr { get; set; }
    }

    public class NestedDerivedUnregistered : NestedBase
    {
        public string NestedDerivedStr { get; set; }
    }

    #endregion
    #region Test Types - RootIncludeNestedDerived - Auto registers derived and nested derived.

    [SafeToSerialize(IncludeNestedDerived = true)]
    public class RootIncludeNestedDerived
    {
        public string BaseStr { get; set; }
    }

    public class RootDerivedAutoRegistered2 : RootIncludeNestedDerived
    {
        public string DerivedStr { get; set; }
        public NestedBase2 NestedBase { get; set; }
        public ExcludeDerivedBase ExcludeDerived { get; set; }
    }

    public class NestedBase2
    {
        public string NestedBaseStr { get; set; }
    }

    public class NestedDerivedAutoRegistered : NestedBase2
    {
        public string NestedDerivedStr { get; set; }
    }

    [SafeToSerialize]
    public class ExcludeDerivedBase
    {
        public string NestedBaseStr { get; set; }
        public NestedBase3 NestedBase { get; set; }
    }

    public class ExcludedDerived : ExcludeDerivedBase
    {
        public string NestedDerivedStr { get; set; }
    }

    public class NestedBase3
    {
        public string NestedBaseStr { get; set; }
    }

    public class ExcludedNestedDerived : NestedBase3
    {
        public string NestedDerivedStr { get; set; }
    }

    #endregion
}

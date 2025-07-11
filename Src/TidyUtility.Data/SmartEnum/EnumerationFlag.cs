using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using TidyUtility.Core;

namespace TidyUtility.Data.SmartEnum;

public interface IEnumerationFlag : IEnumeration
{
    bool IsSingleBit { get; }
    bool IsNamed { get; }
    IEnumerable<IEnumerationFlag> AsSeparatedUntypedFlags { get; }
}

public abstract class EnumerationFlag<TSelf> : Enumeration<TSelf>, IEnumerationFlag
    where TSelf : EnumerationFlag<TSelf>
{
    #region Private static data memebers

    private static readonly FactoryMethod<TSelf> BuildAsUnnamedFactoryMethod = CreateFactoryMethod();
    private static ImmutableList<TSelf>? _allSeparatedFlags;
    private static ImmutableList<TSelf> AllSeparatedFlags => _allSeparatedFlags ??= GetAll()
        .Where(x => x.IsSingleBit)
        .ToImmutableList();

    private static FactoryMethod<TSelf> CreateFactoryMethod()
    {
        ConstructorInfo? ctorInfo = typeof(TSelf)
            .GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, new Type[] {typeof(ulong)});
        if (ctorInfo == null)
            throw new MissingMethodException(typeof(TSelf).Name, "Constructor");

        return Factory.MethodBuilder<TSelf>(ctorInfo);
    }

    #endregion

    // Adapted from answers on: https://stackoverflow.com/q/4171140/677612
    // Verified using: http://realtimecollisiondetection.net/blog/?p=78
    public bool IsSingleBit => this.Value != 0 && (this.Value & (this.Value - 1)) == 0;
    public bool IsNamed { get; }
    public IEnumerable<IEnumerationFlag> AsSeparatedUntypedFlags => this.AsSeparatedFlags();

    protected EnumerationFlag(ulong value, string name) : base(ConvertULongToLongUnchecked(value), name) { this.IsNamed = true; }
    protected EnumerationFlag(ulong value) : base(ConvertULongToLongUnchecked(value), $"0x{value:X}") { this.IsNamed = false; }

    protected static long ConvertULongToLongUnchecked(ulong value) { unchecked { return (long)value; } }

    private static TSelf CreateFromValue(ulong value) { unchecked { return CreateFromValue((long)value); } }
    private static TSelf CreateFromValue(long value)
    {
        TSelf? enumeration = TryFromValue(value);
        if (enumeration != null)
            return enumeration;

        unchecked
        {
            return BuildAsUnnamedFactoryMethod.Invoke((ulong)value);
        }
    }

    public static explicit operator ulong(EnumerationFlag<TSelf> e) { unchecked { return (ulong)e.Value; } }

    public static explicit operator EnumerationFlag<TSelf>(ulong i) { return CreateFromValue(i); }

    public static TSelf operator &(EnumerationFlag<TSelf> a, EnumerationFlag<TSelf> b)
    {
        return CreateFromValue(a.Value & b.Value);
    }

    public static TSelf operator |(EnumerationFlag<TSelf> a, EnumerationFlag<TSelf> b)
    {
        return CreateFromValue(a.Value | b.Value);
    }

    public static TSelf operator ^(EnumerationFlag<TSelf> a, EnumerationFlag<TSelf> b)
    {
        return CreateFromValue(a.Value ^ b.Value);
    }

    public static IEnumerable<TSelf> GetAllSeparatedFlags() => AllSeparatedFlags;

    // Adapted bit-fu from: http://realtimecollisiondetection.net/blog/?p=78
    public IEnumerable<TSelf> AsSeparatedFlags()
    {
        long valueLong = Convert.ToInt64(this.Value, CultureInfo.InvariantCulture);
        while (valueLong != 0)
        {
            yield return CreateFromValue(valueLong & -valueLong); // extract lowest bit set
            valueLong &= (valueLong - 1); // strip off lowest bit set
        }
    }

    //public IEnumerable<TSelf> AsSeparatedFlags()
    //{
    //    foreach (TSelf flag in GetAllSeparatedFlags())
    //        if ((flag.Value & this.Value) != 0)
    //            yield return flag;
    //}
}

using System.Collections.Immutable;
using System.Reflection;
using Newtonsoft.Json;
using TidyUtility.Core.Extensions;

namespace TidyUtility.Data.SmartEnum
{
    public interface IEnumeration
    {
        string Name { get; }
        long Value { get; }
    }

    [JsonConverter(typeof(EnumerationConverter))]
    public abstract class Enumeration<TSelf> : IEnumeration, IEquatable<Enumeration<TSelf>>
        where TSelf : Enumeration<TSelf>
    {
        #region Private static data members

        private static ImmutableList<TSelf>? _cachedInstances;
        private static ImmutableDictionary<long, TSelf>? _lookupByValue;
        private static ImmutableDictionary<string, TSelf>? _lookupByName;

        private static ImmutableList<TSelf> CachedInstances => _cachedInstances ??= GetAllImpl();
        private static ImmutableDictionary<long, TSelf> LookupByValue => _lookupByValue ??= CachedInstances.ToImmutableDictionary(e => e.Value);
        private static ImmutableDictionary<string, TSelf> LookupByName => _lookupByName ??= CachedInstances.ToImmutableDictionary(e => e.Name);

        #endregion

        [JsonIgnore]
        public string Name { get; }
        public long Value { get; }

        protected Enumeration(long value, string name)
        {
            this.Value = value;
            this.Name = name;
        }

        public static bool operator ==(Enumeration<TSelf>? a, Enumeration<TSelf>? b) => a?.Equals(b) ?? ReferenceEquals(null, b);
        public static bool operator !=(Enumeration<TSelf>? a, Enumeration<TSelf>? b) => !a?.Equals(b) ?? !ReferenceEquals(null, b);

        public bool Equals(Enumeration<TSelf>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((Enumeration<TSelf>)obj);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public static IEnumerable<TSelf> GetAll() => CachedInstances;

        public static TSelf FromValue(long value) => TryFromValue(value) ?? throw new EnumerationInitException($"The value {value} is NOT valid for the enumeration {typeof(TSelf).Name}!");

        public static TSelf? TryFromValue(long value) => LookupByValue.TryGetValue(value);

        public static TSelf FromName(string name) => TryFromName(name) ?? throw new EnumerationInitException($"The name {name} is NOT recognized for the enumeration {typeof(TSelf).Name}!");

        public static TSelf? TryFromName(string name) => LookupByName.TryGetValue(name);

        //public static explicit operator long(Enumeration<TSelf> e) => e.Value;
        //public static explicit operator Enumeration<TSelf>(long i) => FromValue(i);

        #region Protected and Private Methods

        private static ImmutableList<TSelf> GetAllImpl()
        {
            FieldInfo[] fields = typeof(TSelf)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f =>  f.FieldType.IsAssignableTo(typeof(TSelf))).ToArray();

            ImmutableList<TSelf> enumInstances = GetAllUnvalidated(fields);

            VerifyConstructorsAreNotPublic();
            VerifyFieldsAreReadonly(fields);
            VerifyNoDuplicates(enumInstances);

            return enumInstances;
        }

        private static ImmutableList<TSelf> GetAllUnvalidated(FieldInfo[] fields)
        {
            return fields
                .Select(f => f.GetValue(null))
                .Where(f => f != null)
                .OfType<TSelf>()
                .OrderBy(e => e.Value)
                .ToImmutableList();
        }

        private static void VerifyConstructorsAreNotPublic()
        {
            ConstructorInfo[] ctorInfoArray = typeof(TSelf)
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance);
            foreach (ConstructorInfo ctorInfo in ctorInfoArray)
                if (ctorInfo.IsPublic)
                    throw new EnumerationInitException($"Constructors for {typeof(TSelf).Name} must NOT be public! Protected or Private constructors ONLY to preserve design.");
        }

        private static void VerifyFieldsAreReadonly(FieldInfo[] fields)
        {
            ImmutableList<string> fieldsNotMarkedAsReadonly = fields
                .Where(f => !f.IsInitOnly)
                .Select(f => f.Name)
                .ToImmutableList();

            if (fieldsNotMarkedAsReadonly.Any())
                throw new EnumerationInitException($"Found enumeration static fields for type {typeof(TSelf).Name} NOT marked as readonly including: {string.Join(',', fieldsNotMarkedAsReadonly)}");
        }

        private static void VerifyNoDuplicates(ImmutableList<TSelf> enumInstances)
        {
            HashSet<long> uniqueValues = enumInstances.Select(e => e.Value).ToHashSet();
            if (uniqueValues.Count != enumInstances.Count)
                throw new EnumerationInitException(
                    $"Found one or more enumeration instances for type {typeof(TSelf).Name} with the same value when initializing the {typeof(TSelf).Name} enumeration!");

            HashSet<string> uniqueNames = Enumerable.ToHashSet(enumInstances.Select(e => e.Name));
            if (uniqueNames.Count != enumInstances.Count)
                throw new EnumerationInitException(
                    $"Found one or more enumeration instances for type {typeof(TSelf).Name} with the same name when initializing the {typeof(TSelf).Name} enumeration!");
        }

        #endregion
    }
}

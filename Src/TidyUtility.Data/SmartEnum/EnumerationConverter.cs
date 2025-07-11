using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Dynamitey;
using Newtonsoft.Json;
using TidyUtility.Core.Extensions;

namespace TidyUtility.Data.SmartEnum;


// TODO: Consider creating an Enumeration handler or converter compatible with the .Net Core JSON serializer

public class EnumerationAsStringConverter : EnumerationConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        switch (value)
        {
            case IEnumerationFlag flag when flag.AsSeparatedUntypedFlags.All(f => f.IsNamed):
                writer.WriteValue(string.Join(", ", flag.AsSeparatedUntypedFlags.Select(f => f.Name)));
                break;
            case IEnumerationFlag flag:
                unchecked
                {
                    writer.WriteRawValue($"0x{(ulong)flag.Value:X}");
                }
                break;
            default:
                writer.WriteValue(((IEnumeration)value).Name);
                break;
        }
    }
}

/// <summary>
/// Json.Net based JsonConverter for Enumeration<> derived types.
/// </summary>
public class EnumerationConverter : JsonConverter
{
    private static readonly Regex HexValueRegex = new Regex(@"^\s*(0[xX])?(?<HexValue>[0-9a-fA-F]{1,16})\s*$");

    private static ImmutableDictionary<Type, GenericTypeInfo> _objectTypeInfoCache = ImmutableDictionary<Type, GenericTypeInfo>.Empty;

    private record GenericTypeInfo
    {
        public bool IsEnumerationFlagType { get; init; }
        public Func<string, object?> TryFromNameFunc { get; init; } = _ => null;
        public Func<long, object?> TryFromValueFunc { get; init; } = _ => null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsAssignableTo(typeof(IEnumeration));
    }

    public override object? ReadJson(JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        try
        {
            if (reader.TokenType == JsonToken.String)
                return CreateFromStringToken();

            if (reader.TokenType == JsonToken.Integer)
                return CreateFromIntegerToken();
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Error converting value {reader.Value} to type '{objectType.Name}'.", ex);
        }

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing {typeof(Enumeration<>).Name}<>.");

        object? CreateFromIntegerToken()
        {
            GenericTypeInfo objectTypeInfo = GetObjectTypeInfo(objectType);

            long? longValue = (long?)reader.Value;
            object? objFromValue = longValue == null ? null : objectTypeInfo.TryFromValueFunc(longValue.Value);

            return objFromValue;
        }

        object? CreateFromStringToken()
        {
            GenericTypeInfo objectTypeInfo = GetObjectTypeInfo(objectType);

            string? enumText = reader.Value?.ToString();
            if (enumText == null)
                return null;

            if (!objectTypeInfo.IsEnumerationFlagType)
                return objectTypeInfo.TryFromNameFunc(enumText);

            string[] enumNames = enumText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            ulong flagValue = 0;

            foreach (string enumName in enumNames)
            {
                object? objFromName = objectTypeInfo.TryFromNameFunc(enumName);
                if (objFromName != null)
                {
                    unchecked
                    {
                        flagValue |= (ulong)((IEnumeration)objFromName).Value;
                    }
                }
                else
                {
                    Match hexMatch = HexValueRegex.Match(enumName);
                    if (!hexMatch.Success)
                        throw new EnumerationInitException(
                            $"Cannot deserialize an {objectType.Name} with a Name of \"{enumName}\"!");
                    unchecked
                    {
                        flagValue |= ulong.Parse(hexMatch.Groups["HexValue"].Value, NumberStyles.HexNumber);
                    }
                }
            }

            unchecked
            {
                return objectTypeInfo.TryFromValueFunc((long)flagValue);
            }
        }
    }

    private static GenericTypeInfo GetObjectTypeInfo(Type objectType)
    {
        // Apparently, this method can be called by different threads, leading to a possible race condition.
        // Dictionary modification must be thread safe.
        //
        // Using a temporary reference to the current cache ensures that there isn't an exception for a duplicate
        // key when adding an entry. While it is possible to lose cached entries if two threads are adding entries
        // at the same time, they should just get re-cached later if needed. It isn't perfect, but it avoids
        // multi-threaded locking & blocking.

        ImmutableDictionary<Type, GenericTypeInfo> objectTypeInfoCacheRef = _objectTypeInfoCache;

        if (!objectTypeInfoCacheRef.TryGetValue(objectType, out GenericTypeInfo? objectTypeInfo))
        {
            bool isEnumerationFlag = objectType.GetAllBaseTypes()
                .Where(t => t.IsClass)
                .Where(t => t.IsGenericType)
                .Select(t => t.GetGenericTypeDefinition())
                .Any(t => t == typeof(EnumerationFlag<>));

            objectTypeInfo = isEnumerationFlag ? BuildEnumerationFlagObjectTypeInfo() : BuildEnumerationObjectTypeInfo();

            _objectTypeInfoCache = objectTypeInfoCacheRef.Add(objectType, objectTypeInfo);
        }

        return objectTypeInfo;

        GenericTypeInfo BuildEnumerationObjectTypeInfo()
        {
            Func<Type, InvokeContext>? staticContext = InvokeContext.CreateStatic;
            Type closedEnumerationType = typeof(Enumeration<>).MakeGenericType(objectType);

            return new GenericTypeInfo()
            {
                IsEnumerationFlagType = false,
                TryFromNameFunc = name => Dynamic.InvokeMember(staticContext(closedEnumerationType), "TryFromName", name),
                TryFromValueFunc = enumValue => Dynamic.InvokeMember(staticContext(closedEnumerationType), "TryFromValue", enumValue),
            };
        }

        GenericTypeInfo BuildEnumerationFlagObjectTypeInfo()
        {
            Func<Type, InvokeContext>? staticContext = InvokeContext.CreateStatic;
            Type closedEnumerationType = typeof(Enumeration<>).MakeGenericType(objectType);
            Type closedEnumerationFlagType = typeof(EnumerationFlag<>).MakeGenericType(objectType);

            objectTypeInfo = new GenericTypeInfo
            {
                IsEnumerationFlagType = true,
                TryFromNameFunc = enumText => Dynamic.InvokeMember(staticContext(closedEnumerationType), "TryFromName", enumText),
                TryFromValueFunc = enumValue => Dynamic.InvokeMember(staticContext(closedEnumerationFlagType), "CreateFromValue", enumValue),
            };
            return objectTypeInfo;
        }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
            writer.WriteNull();
        else
        {
            GenericTypeInfo objectTypeInfo = GetObjectTypeInfo(value.GetType());
            unchecked
            {
                if (objectTypeInfo.IsEnumerationFlagType)
                    writer.WriteRawValue($"0x{(ulong)((IEnumeration)value).Value:X}");
                else
                    writer.WriteValue(((IEnumeration)value).Value);
            }
        }
    }
}
 #nullable disable
 using System.Collections;
 using System.Collections.Immutable;
 using System.Reflection;
 using Newtonsoft.Json;
 using Newtonsoft.Json.Serialization;
 using NodaTime;
 using NodaTime.Serialization.JsonNet;
 using TidyUtility.Core.Extensions;

 namespace TidyUtility.Data.Json
{
    // For an explanation on the security vulnerability this helps resolve, see: https://stackoverflow.com/a/8763745
    // Adapted from: https://www.newtonsoft.com/json/help/html/SerializeSerializationBinder.htm

    public class SafeJsonDotNetSerializer : ISerializer
    {
        private static readonly HashSet<Type> PrimitivesSupportedByJsonNet = new HashSet<Type>()
        {
            typeof(object),
            typeof(string),
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(short),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
            typeof(byte[]),
            typeof(Type),
            typeof(Guid),
        };

        private static readonly object LockObj = new object();

        private static volatile ImmutableHashSet<Assembly> _knownAssemblies =
            ImmutableHashSet<Assembly>.Empty.Add(typeof(SafeJsonDotNetSerializer).Assembly);

        private static volatile Lazy<ImmutableDictionary<string, Type>> _knownTypesByName =
            new Lazy<ImmutableDictionary<string, Type>>(() => ImmutableDictionary<string, Type>.Empty);

        public SafeJsonDotNetSerializer()
        {
        }

        public SafeJsonDotNetSerializer(params Type[] typesWithinAssemblies)
        {
            RegisterAssemblies(typesWithinAssemblies);
        }

        public SafeJsonDotNetSerializer(params Assembly[] assembliesWithSerializableTypes)
        {
            RegisterAssemblies(assembliesWithSerializableTypes);
        }

        public string Serialize<T>(T dataToSerialize)
        {
            return JsonConvert.SerializeObject(dataToSerialize, typeof(T), Formatting.None, GetJsonSerializerSettings());
        }

        public T Deserialize<T>(string serializedData)
        {
            return JsonConvert.DeserializeObject<T>(serializedData, GetJsonSerializerSettings());
        }

        public static void RegisterAssembly(Type typesWithinAssembly)
        {
            RegisterAssemblies(Enumerable.Empty<Assembly>().Append(typesWithinAssembly.Assembly));
        }

        public static void RegisterAssembly(Assembly assemblyWithSerializableTypes)
        {
            RegisterAssemblies(Enumerable.Empty<Assembly>().Append(assemblyWithSerializableTypes));
        }

        public static void RegisterAssemblies(params Type[] typesWithinAssemblies)
        {
            RegisterAssemblies(typesWithinAssemblies.Select(t => t.Assembly));
        }

        public static void RegisterAssemblies(params Assembly[] assembliesWithSerializableTypes)
        {
            RegisterAssemblies(assembliesWithSerializableTypes.AsEnumerable());
        }

        public static void RegisterAssemblies(IEnumerable<Type> typesWithinAssemblies)
        {
            RegisterAssemblies(typesWithinAssemblies.Select(t => t.Assembly));
        }

        public static void RegisterAssemblies(IEnumerable<Assembly> assembliesWithSerializableTypes)
        {
            lock (LockObj)
            {
                List<Assembly> newAssemblies = assembliesWithSerializableTypes.ToList();
                if (newAssemblies.All(asm => _knownAssemblies.Contains(asm)))
                    return;

                _knownAssemblies = _knownAssemblies.Union(newAssemblies);

                // When adding known assemblies, we reset known-types-by-name to be re-loaded and cached when serializing.
                _knownTypesByName =
                    new Lazy<ImmutableDictionary<string, Type>>(() =>
                        GetRegisteredTypes().ToImmutableDictionary(x => x.Name));
            }
        }

        private static JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new KnownTypesBinderImpl(),
                NullValueHandling = NullValueHandling.Ignore,
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            return settings;
        }

        private static IEnumerable<Tuple<Type, SafeToSerializeAttribute>> GetTypesTaggedWithAttribute(
            IEnumerable<Assembly> assemblies)
        {
            Type serializeSafelyAttributeType = typeof(SafeToSerializeAttribute);

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    SafeToSerializeAttribute safeToSerializeAttrib = type
                        .GetCustomAttributes(serializeSafelyAttributeType, false)
                        .Cast<SafeToSerializeAttribute>()
                        .FirstOrDefault();
                    if (safeToSerializeAttrib != null)
                    {
                        yield return new Tuple<Type, SafeToSerializeAttribute>(type, safeToSerializeAttrib);

                        foreach (Type relatedType in safeToSerializeAttrib.KnownTypes)
                            yield return new Tuple<Type, SafeToSerializeAttribute>(relatedType, null);
                    }
                }
            }
        }

        private static HashSet<Type> GetRegisteredTypes()
        {
            IList<JsonConverter> jsonConverters = GetJsonSerializerSettings().Converters;
            IEnumerable<Tuple<Type, SafeToSerializeAttribute>> typesToRegister = 
                GetTypesTaggedWithAttribute(_knownAssemblies);

            var registeredTypes = new HashSet<Type>();

            foreach (Tuple<Type, SafeToSerializeAttribute> tuple in typesToRegister)
                GetRegisteredTypes(new TypeRegInfo(tuple.Item1, tuple.Item2), registeredTypes, 
                    _knownAssemblies, jsonConverters);

            return registeredTypes;
        }

        private static IEnumerable<Type> GetDerivedTypes(ImmutableHashSet<Assembly> knownAssemblies, Type baseType)
        {
            // Everything inherits from object. Be more specific!
            if (baseType == typeof(object)) 
                yield break;

            // For safety, verify that baseType is from a known assembly.
            if (!knownAssemblies.Contains(baseType.Assembly)) 
                yield break;

            foreach (Assembly knownAssembly in knownAssemblies)
            {
                foreach (Type type in knownAssembly.GetTypes())
                {
                    if (baseType != type && baseType.IsAssignableFrom(type))
                        yield return type;
                }
            }
        }

        private static void GetRegisteredTypes(TypeRegInfo typeInfo, HashSet<Type> registeredTypes,
            ImmutableHashSet<Assembly> knownAssemblies, IList<JsonConverter> jsonConverters)
        {
            if (PrimitivesSupportedByJsonNet.Contains(typeInfo.Type))
                return;

            if (typeInfo.Type.IsEnum)
                return;

            if (JsonConverterSupportsType(typeInfo.Type, jsonConverters))
                return;

            if (typeInfo.IncludeDerived)
            {
                IEnumerable<Type> derivedTypes = GetDerivedTypes(knownAssemblies, typeInfo.Type);
                foreach (Type derivedType in derivedTypes)
                    GetRegisteredTypes(typeInfo.New(derivedType), registeredTypes, knownAssemblies, jsonConverters);
            }

            if (registeredTypes.Contains(typeInfo.Type))
                return;

            if (typeof(IEnumerable).IsAssignableFrom(typeInfo.Type))
            {
                Type implementedGenericIEnumerableType = typeInfo.Type.GetInterfaces()
                    .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (implementedGenericIEnumerableType == null)
                    return;
                Type genericTypeArg = implementedGenericIEnumerableType.GetGenericArguments().Single();
                if (genericTypeArg.IsGenericType && genericTypeArg.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    foreach (Type kvpGenericTypeArg in genericTypeArg.GetGenericArguments())
                    {
                        TypeRegInfo typeRegInfo = typeInfo.New(kvpGenericTypeArg);
                        GetRegisteredTypes(typeRegInfo, registeredTypes, knownAssemblies, jsonConverters);
                    }
                }
                else
                {
                    TypeRegInfo typeRegInfo = typeInfo.New(genericTypeArg);
                    GetRegisteredTypes(typeRegInfo, registeredTypes, knownAssemblies, jsonConverters);
                }

                return;
            }

            if (typeInfo.Type.IsGenericType && typeInfo.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                TypeRegInfo typeRegInfo = typeInfo.New(typeInfo.Type.GetGenericArguments()[0]);
                GetRegisteredTypes(typeRegInfo, registeredTypes, knownAssemblies, jsonConverters);
                return;
            }

            if (typeInfo.Type.IsAbstract || typeInfo.Type.IsInterface)
                return;
            
            registeredTypes.Add(typeInfo.Type);

            if (!JsonConverterSupportsType(typeInfo.Type, jsonConverters) && typeInfo.IncludeNestedDerived)
                foreach (PropertyInfo propertyInfo in typeInfo.Type.GetProperties())
                    GetRegisteredTypes(typeInfo.New(propertyInfo.PropertyType), registeredTypes, knownAssemblies, jsonConverters);
        }

        private static bool JsonConverterSupportsType(Type type, IList<JsonConverter> jsonConverters)
        {
            bool canRead = false;
            bool canWrite = false;

            foreach (JsonConverter converter in jsonConverters)
            {
                if (!converter.CanConvert(type))
                    continue;

                canRead = canRead || converter.CanRead;
                canWrite = canWrite || converter.CanWrite;
                if (canRead && canWrite)
                    return true;
            }

            return false;
        }

        private class TypeRegInfo
        {
            public TypeRegInfo(Type type, SafeToSerializeAttribute attrib)
            {
                this.Type = type;
                this.Attrib = attrib;
                this.IncludeDerived = attrib?.IncludeDerived ?? false;
                this.IncludeNestedDerived = attrib?.IncludeNestedDerived ?? false;
            }

            private TypeRegInfo(TypeRegInfo info, Type type)
            {
                this.Type = type;
                this.Attrib = null;
                this.IncludeDerived = info.IncludeDerived;
                this.IncludeNestedDerived = info.IncludeNestedDerived;
            }

            public TypeRegInfo New(Type type)
            {
                SafeToSerializeAttribute safeToSerializeAttrib = type
                    .GetCustomAttributes(typeof(SafeToSerializeAttribute), false)
                    .Cast<SafeToSerializeAttribute>()
                    .FirstOrDefault();

                if (safeToSerializeAttrib != null)
                    return new TypeRegInfo(type, safeToSerializeAttrib);

                return new TypeRegInfo(this, type);
            }

            public Type Type { get; set; }
            public SafeToSerializeAttribute Attrib { get; set; }
            public bool IncludeDerived { get; set; }
            public bool IncludeNestedDerived { get; set; }
        }

        private class KnownTypesBinderImpl : ISerializationBinder
        {
            private ImmutableDictionary<string, Type> _knownTypes;

            public Type BindToType(string assemblyName, string typeName)
            {
                if (this._knownTypes == null)
                    this._knownTypes = _knownTypesByName.Value;

                Type type = this._knownTypes.TryGetValue(typeName);
                if (type == null)
                {
                    this.CheckForNewlyLoadedAssemblies();
                    type = this._knownTypes.TryGetValue(typeName);
                }

                return type;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.Name;
            }

            private void CheckForNewlyLoadedAssemblies()
            {
                HashSet<Assembly> possibleNewAssemblies = typeof(SafeJsonDotNetSerializer).Assembly.GetAllReferencingAssemblies();
                SafeJsonDotNetSerializer.RegisterAssemblies(possibleNewAssemblies);
                this._knownTypes = _knownTypesByName.Value;
            }
        }
    }
}

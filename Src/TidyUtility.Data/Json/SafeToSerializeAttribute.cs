 #nullable disable
  namespace TidyUtility.Data.Json
{
    /// <summary>
    /// Apply to any type to be serialized by <see cref="SafeJsonDotNetSerializer"/>. Specifically used to control
    /// the serialization of derived types within the type being serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class SafeToSerializeAttribute : Attribute
    {
        private bool _includeNestedDerived;

        /// <summary>
        /// Set to true if this is a base type and you want to automatically search for
        /// derived types and consider them safe to serialize too.
        /// </summary>
        public bool IncludeDerived { get; set; }

        /// <summary>
        /// Set to true if this is a root type and you want to automatically search for all derived
        /// types and all nested property derived types and consider them safe to serialize too.
        /// </summary>
        public bool IncludeNestedDerived
        {
            get => this._includeNestedDerived;
            set
            {
                if (value)
                    this.IncludeDerived = true;
                this._includeNestedDerived = value;
            }
        }

        /// <summary>
        /// Optionally identify other types that can be serialized. Note: Derived type ds
        /// </summary>
        public Type[] KnownTypes { get; set; } = new Type[0];
    }
}
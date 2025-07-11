 #nullable disable
 using System.Security.Cryptography;
 using System.Text;
 using TidyUtility.Data.Json;

 namespace TidyUtility.Data.DataSafe
{
    //       Dotnet 5 being cross platform, this will only work on Windows and will otherwise throw a
    //       PlatformNotSupportedException exception. Documentation is unclear what will happen on mobile
    //       under Xamarin implementations. May not work for Xamarin either.

    public abstract class AbstractSecureDataSafe<TSerializable>
    {
        private readonly ISerializer _serializer;

        protected AbstractSecureDataSafe(ISerializer serializer)
        {
            this._serializer = serializer;
        }

        public abstract void Save(TSerializable dataToSecure);
        public abstract TSerializable Load();

        protected byte[] SerializeAndEncrypt(TSerializable dataToSecure)
        {
            if (OperatingSystem.IsWindows())
            {
                return ProtectedData.Protect(Encoding.UTF8.GetBytes(this._serializer.Serialize(dataToSecure)),
                null, DataProtectionScope.CurrentUser);
            }
            else
            {
                throw new NotSupportedException("Only implemented for Windows.");
            }
        }

        protected TSerializable DeserializeAndDecrypt(byte[] encryptedData)
        {
            if (OperatingSystem.IsWindows())
            {
                return this._serializer.Deserialize<TSerializable>(Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    encryptedData, null, DataProtectionScope.CurrentUser)));
            }
            else
            {
                throw new NotSupportedException("Only implemented for Windows.");
            }
        }
    }
}
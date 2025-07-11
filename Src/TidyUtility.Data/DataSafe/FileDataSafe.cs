 #nullable disable
 using TidyUtility.Core;
 using TidyUtility.Data.Json;

 namespace TidyUtility.Data.DataSafe
{
    public class FileDataSafe<TSerializable> : AbstractSecureDataSafe<TSerializable>
        where TSerializable : new()
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileDataSafe(string filePath, ISerializer serializer)
            : base(serializer)
        {
            this._filePath = filePath;
        }

        public override void Save(TSerializable dataToSecure)
        {
            lock (this._lock)
            {
                File.WriteAllBytes(this._filePath, this.SerializeAndEncrypt(dataToSecure));
            }
        }

        public override TSerializable Load()
        {
            lock (this._lock)
            {
                if (!File.Exists(this._filePath))
                    return Factory<TSerializable>.Create();
                return this.DeserializeAndDecrypt(File.ReadAllBytes(this._filePath));
            }
        }
    }
}
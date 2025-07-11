 #nullable disable
 using TidyUtility.Data.Json;

 namespace TidyUtility.Data.DataSafe
{
    public class MemoryDataSafe<TSerializable> : AbstractSecureDataSafe<TSerializable>
        where TSerializable : class
    {
        private readonly object _lock = new object();
        private byte[] _encryptedBytes;

        public MemoryDataSafe(ISerializer serializer)
            : base(serializer)
        {
        }

        public override void Save(TSerializable dataToSecure)
        {
            lock (this._lock)
            {
                this._encryptedBytes = this.SerializeAndEncrypt(dataToSecure);
            }
        }

        public override TSerializable Load()
        {
            lock (this._lock)
            {
                return this._encryptedBytes == null
                    ? null
                    : this.DeserializeAndDecrypt(this._encryptedBytes);
            }
        }
    }
}

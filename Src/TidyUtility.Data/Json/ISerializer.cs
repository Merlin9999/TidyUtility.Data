 #nullable disable
namespace TidyUtility.Data.Json
{
    public interface ISerializer
    {
        string Serialize<T>(T dataToSerialize);
        T Deserialize<T>(string serializedData);
    }
}
 #nullable disable
 using System.Reflection;
 using FluentAssertions;
 using TidyUtility.Data.DataSafe;
 using TidyUtility.Data.Json;

 namespace TidyUtility.Tests.DataSafe
{
    public class DataSafeTests
    {
        [Fact]
        public void TestMemoryDataSafe()
        {
            const string expectedSecureData = "QwertyAsdf";
            DataToSecure dataToSecure = new DataToSecure(){ProtectedInformation = expectedSecureData};

            var secure = new MemoryDataSafe<DataToSecure>(new SafeJsonDotNetSerializer());

            secure.Save(dataToSecure);
            DataToSecure decrypted = secure.Load();

            decrypted.ProtectedInformation.Should().Be(expectedSecureData);
        }

        [Fact]
        public void TestFileDataSafe()
        {
            const string expectedSecureData = "QwertyAsdf";
            DataToSecure dataToSecure = new DataToSecure() { ProtectedInformation = expectedSecureData };

            string fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FileDataSafe.bin3");

            var secure = new FileDataSafe<DataToSecure>(fileName, new SafeJsonDotNetSerializer());

            secure.Save(dataToSecure);
            DataToSecure decrypted = secure.Load();

            decrypted.ProtectedInformation.Should().Be(expectedSecureData);
        }
    }

    public class DataToSecure
    {
        public string ProtectedInformation { get; set; }
    }
}

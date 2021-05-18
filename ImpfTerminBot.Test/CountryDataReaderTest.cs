using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ImpfTerminBot.Test
{
    [TestClass]
    public class CountryDataReaderTest
    {
        [TestMethod]
        public void ReadFromUrl_Success()
        {
            var reader = new CountryDataReader();
            var countryData = reader.ReadFromUrl();

            Assert.IsTrue(countryData.Count > 0);
        }

        [TestMethod]
        public void ReadFromFile_Success()
        {
            var reader = new CountryDataReader();
            var fileName = @"../../../TestData/data.json";
            var countryData = reader.ReadFromFile(fileName);

            Assert.IsTrue(countryData.Count > 0);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ReadFromFile_Fail_FileNotFound()
        {
            var reader = new CountryDataReader();
            var fileName = "wrong.json";
            var countryData = reader.ReadFromFile(fileName);
        }
    }
}

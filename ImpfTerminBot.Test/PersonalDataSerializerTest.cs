using ImpfTerminBot.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ImpfTerminBot.Test
{
    [TestClass]
    public class PersonalDataSerializerTest
    {
        [TestMethod]
        public void Serialize_Success()
        {
            var fileName = @"personaldata.bin";
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            var data = new PersonalData()
            {
                Salutation = eSalutation.Lady,
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Susi",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            var serializer = new PersonalDataBinarySerializer();
            serializer.Serialize(data, fileName);

            Assert.IsTrue(File.Exists(fileName));
        }

        [TestMethod]
        public void Deserialize_Success()
        {
            var fileName = @"../../../TestData/personaldata.bin";

            var serializer = new PersonalDataBinarySerializer();
            var actualData = serializer.Deserialize(fileName);

            var expectedData = new PersonalData()
            {
                Salutation = eSalutation.Lady,
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Susi",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsNotNull(expectedData);
            Assert.AreEqual(expectedData, actualData);
        }
    }
}

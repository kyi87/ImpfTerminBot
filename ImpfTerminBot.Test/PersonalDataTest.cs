using ImpfTerminBot.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImpfTerminBot.Test
{
    [TestClass]
    public class PersonalDataTest
    {
        [TestMethod]
        public void IsComplete_Success()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Hans",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsTrue(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoCity()
        {
            var data = new PersonalData()
            {
                City = "",
                Email = "test@gmx.de",
                FirstName = "Hans",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoEmail()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "",
                FirstName = "Hans",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoFirstName()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoName()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Hans",
                Name = "",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoStreet()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Hans",
                Name = "Wurst",
                Street = "",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoHouseNumber()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Hans",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "",
                Phone = "0142457558",
                Postcode = "85555",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoPhone()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Hans",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "",
                Postcode = "85555",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoPostcode()
        {
            var data = new PersonalData()
            {
                City = "Stuttgart",
                Email = "test@gmx.de",
                FirstName = "Hans",
                Name = "Wurst",
                Street = "Schlossallee",
                HouseNumber = "10",
                Phone = "0142457558",
                Postcode = "",
            };

            Assert.IsFalse(data.IsComplete());
        }

        [TestMethod]
        public void IsComplete_Fail_NoData()
        {
            var data = new PersonalData();

            Assert.IsFalse(data.IsComplete());
        }
    }
}

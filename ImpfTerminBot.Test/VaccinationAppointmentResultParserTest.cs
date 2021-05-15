using ImpfTerminBot.GUI.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace ImpfTerminBot.Test
{
    [TestClass]
    public class VaccinationAppointmentResultParserTest
    {
        [TestMethod]
        public void Parse_AppointmentsAvailable_OneVaccine()
        {
            var content = "{\"termineVorhanden\":true,\"vorhandeneLeistungsmerkmale\":[\"L920\"]}";

            var parser = new VaccinationAppointmentResultParser();

            var result = parser.Parse(content);

            Assert.IsTrue(result.IsAvailable);
            Assert.AreEqual(result.Vaccines.Count, 1);
            Assert.AreEqual(result.Vaccines[0], eVaccines.Biontech);
        }

        [TestMethod]
        public void Parse_AppointmentsAvailable_MultipleVaccine()
        {
            var content = "{\"termineVorhanden\":true,\"vorhandeneLeistungsmerkmale\":[\"L920\",\"L921\",\"L922\"]}";

            var parser = new VaccinationAppointmentResultParser();

            var result = parser.Parse(content);

            Assert.IsTrue(result.IsAvailable);
            Assert.AreEqual(result.Vaccines.Count, 3);
            Assert.IsTrue(result.Vaccines.Contains(eVaccines.Biontech));
            Assert.IsTrue(result.Vaccines.Contains(eVaccines.AstraZeneca));
            Assert.IsTrue(result.Vaccines.Contains(eVaccines.Moderna));
        }

        [TestMethod]
        public void Parse_NoAppointments()
        {
            var content = "{\"termineVorhanden\":false,\"vorhandeneLeistungsmerkmale\":[]}";

            var parser = new VaccinationAppointmentResultParser();

            var result = parser.Parse(content);
            Assert.IsFalse(result.IsAvailable);
            Assert.AreEqual(result.Vaccines.Count, 0);
        }
    }
}

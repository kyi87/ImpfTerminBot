using ImpfTerminBot.GUI.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ImpfTerminBot
{
    public class VaccinationAppointmentResult
    {
        public bool IsAvailable { get; set; }
        public List<eVaccines> Vaccines { get; set; } = new List<eVaccines>();
    }

    public class VaccinationAppointmentResultParser
    {
        public VaccinationAppointmentResult Parse(string content)
        {
            dynamic json = JsonConvert.DeserializeObject(content);
            var appointmentAvailable = json.termineVorhanden == "true";

            var result = new VaccinationAppointmentResult()
            {
                IsAvailable = appointmentAvailable,
            };

            var vaccines = json.vorhandeneLeistungsmerkmale;
            foreach (var item in vaccines)
            {
                var vaccine = VaccinationAppointmentGlobals.VaccinesDict.FirstOrDefault(x => x.Value == item.Value);
                if (!vaccine.Equals(default(KeyValuePair<eVaccines, string>)))
                {
                    result.Vaccines.Add(vaccine.Key);
                }
            }
            return result;
        }
    }
}

using ImpfTerminBot.Model;
using System.Collections.Generic;

namespace ImpfTerminBot
{
    public static class VaccinationAppointmentGlobals
    {
        public static readonly Dictionary<eVaccines, string> VaccinesDict = new Dictionary<eVaccines, string>()
        {
            { eVaccines.Biontech, "L920" } ,
            { eVaccines.Moderna, "L921" } ,
            { eVaccines.AstraZeneca, "L922" } ,
        };
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ImpfTerminBot.Model
{
    public class CountryData
    {
        public string Country { get; set; }
        public List<CenterData> Centers { get; set; } = new List<CenterData>();
    }
}

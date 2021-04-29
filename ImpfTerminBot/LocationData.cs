using System;
using System.Collections.Generic;
using System.Text;

namespace ImpfBot
{
    public class CountryData
    {
        public string Country { get; set; }
        public List<CenterData> Centers { get; set; } = new List<CenterData>();
    }

    public class CenterData
    {
        public string CenterName { get; set; }
        public string Postcode { get; set; }
    }
}

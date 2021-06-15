namespace ImpfTerminBot.Model
{
    public class PersonalData
    {
        public eSalutation Salutation {get; set;}
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string Postcode { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public bool IsComplete()
        {
            return !string.IsNullOrEmpty(Name) &&
                !string.IsNullOrEmpty(FirstName) &&
                !string.IsNullOrEmpty(Postcode) &&
                !string.IsNullOrEmpty(City) &&
                !string.IsNullOrEmpty(Street) &&
                !string.IsNullOrEmpty(HouseNumber) &&
                !string.IsNullOrEmpty(Email) &&
                !string.IsNullOrEmpty(Phone) &&
                !string.IsNullOrEmpty(HouseNumber) ;
        }
    }
}
using System;

namespace ImpfTerminBot.Model
{
    [Serializable]
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

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                PersonalData p = (PersonalData)obj;
                return (Salutation == p.Salutation)
                    && (Name == p.Name)
                    && (FirstName == p.FirstName)
                    && (Postcode == p.Postcode)
                    && (City == p.City)
                    && (Street == p.Street)
                    && (HouseNumber == p.HouseNumber)
                    && (Phone == p.Phone)
                    && (Email == p.Email);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
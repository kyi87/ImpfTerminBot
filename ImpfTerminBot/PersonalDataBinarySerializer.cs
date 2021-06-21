using ImpfTerminBot.Model;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ImpfTerminBot
{
    public class PersonalDataBinarySerializer
    {
        public void Serialize(PersonalData personalData, string filename)
        {
            using (Stream ms = File.OpenWrite(filename))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, personalData);
            }
        }

        public PersonalData Deserialize(string filename)
        {
            using (FileStream fs = File.Open(filename, FileMode.Open))
            {
                var formatter = new BinaryFormatter();
                object obj = formatter.Deserialize(fs);
                PersonalData personalData = (PersonalData)obj;
                return personalData;
            }
        }
    }
}

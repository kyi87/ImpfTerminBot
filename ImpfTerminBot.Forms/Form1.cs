using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Media;

namespace ImpfBot.Forms
{
    public partial class Form1 : Form
    {
        private List<CountryData> m_LocationData;
        private string m_Code;

        public Form1()
        {
            InitializeComponent();

            var filename = "data.json";

            if(!File.Exists(filename))
            {
                MessageBox.Show($"Die Datei {filename} konnte nicht gefunden werden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            var jsonString = File.ReadAllText(filename);
            m_LocationData = JsonSerializer.Deserialize<List<CountryData>>(jsonString);

            var dict = new Dictionary<CountryData, string>();
            foreach (var location in m_LocationData)
            {
                dict.Add(location, location.Country);
            }
            cbCountry.DataSource = new BindingSource(dict, null);
            cbCountry.DisplayMember = "Value";
            cbCountry.ValueMember = "Key";
            cbCountry.SelectedIndex = 0;

            btnStart.Enabled = false;
        }

        private void OnCountrySelectionChanged(object sender, EventArgs e)
        {
            var country = ((KeyValuePair<CountryData, string>)cbCountry.SelectedItem).Key;

            var dict = new Dictionary<CenterData, string>();
            foreach (var center in country.Centers)
            {
                dict.Add(center, center.CenterName);
            }

            cbCenter.DataSource = new BindingSource(dict, null);
            cbCenter.DisplayMember = "Value";
            cbCenter.ValueMember = "Key";
            cbCenter.SelectedIndex = 0;
        }

        private void tbCode_TextChanged(object sender, EventArgs e)
        {
            if(tbCode.Text.Length == 14)
            {
                cbCenter.Enabled = true;
                cbCountry.Enabled = true;
                btnStart.Enabled = true;
                m_Code = tbCode.Text;
            }
            else
            {
                cbCenter.Enabled = false;
                cbCountry.Enabled = false;
                btnStart.Enabled = false;
                m_Code = "";
            }
        }

        private async void OnBtnStartClick(object sender, EventArgs e)
        {
            try
            {
                var country = ((KeyValuePair<CountryData, string>)cbCountry.SelectedItem).Key;
                var center = ((KeyValuePair<CenterData, string>)cbCenter.SelectedItem).Key;

                var worker = new VaccinationAppointmentFinder(m_Code, country.Country, center);

                EnableControls(false);
                await worker.Search();

                PlaySound();
                MessageBox.Show("Bitte Daten im Browser eingeben.", "Termin gefunden.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                EnableControls(true);
            }
            catch (Exception ex)
            {
                SystemSounds.Exclamation.Play();
                MessageBox.Show($"Es ist ein Fehler aufgetreten: {ex.Message}. Programm wird beendet.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private static void PlaySound()
        {
            var soundFile = @"C:\Windows\Media\Alarm01.wav";
            if (File.Exists(soundFile))
            {
                var player = new SoundPlayer(soundFile);
                player.Play();
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }

        private void EnableControls(bool b)
        {
            tbCode.Enabled = b;
            cbCenter.Enabled = b;
            cbCountry.Enabled = b;
            btnStart.Enabled = b;
        }
    }
}

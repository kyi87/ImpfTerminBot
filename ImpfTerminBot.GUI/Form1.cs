using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Media;
using ImpfTerminBot.Model;

namespace ImpfTerminBot.Forms
{
    public partial class Form1 : Form
    {
        private List<CountryData> m_LocationData;
        private VaccinationAppointmentFinder m_AppointmentFinder;
        private string m_Code;
        private bool m_IsError;

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

            m_AppointmentFinder = new VaccinationAppointmentFinder();

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
            btnStop.Enabled = false;
        }

        private void cbCountry_SelectionChanged(object sender, EventArgs e)
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

        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnStop.Enabled = true;
                var country = ((KeyValuePair<CountryData, string>)cbCountry.SelectedItem).Key;
                var center = ((KeyValuePair<CenterData, string>)cbCenter.SelectedItem).Key;

                EnableControls(false);
                var isSuccess = await m_AppointmentFinder.Search(m_Code, country.Country, center);

                if(isSuccess)
                {
                    PlaySound();
                    MessageBox.Show("Bitte Daten im Browser eingeben.", "Termin gefunden.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch(CodeNotValidException ex)
            {
                SystemSounds.Exclamation.Play();
                MessageBox.Show($"{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                m_AppointmentFinder.CloseBrowser();
            }
            catch (Exception ex)
            {
                m_IsError = true;
                SystemSounds.Exclamation.Play();
                MessageBox.Show($"Es ist ein Fehler aufgetreten: {ex.Message}. Programm wird beendet.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            finally
            {
                EnableControls(true);
                btnStop.Enabled = false;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            m_AppointmentFinder.StopSearch();
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(m_AppointmentFinder.IsSearching() && !m_IsError)
            {
                DialogResult result = MessageBox.Show("Soll der Browser geschlossen werden? Alle eingegebenen Daten gehen verloren.", "Browser schließen", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    m_AppointmentFinder.CloseBrowser();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}

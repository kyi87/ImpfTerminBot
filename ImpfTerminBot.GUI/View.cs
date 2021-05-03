using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Media;
using ImpfTerminBot.Model;

namespace ImpfTerminBot.Forms
{
    public partial class View : Form
    {
        private List<CountryData> m_LocationData;
        private VaccinationAppointmentFinder m_AppointmentFinder;
        private string m_Code;

        public View()
        {
            InitializeComponent();
            ReadJsonData();
            InitMaskedTextbox();
            InitDictionary();

            m_AppointmentFinder = new VaccinationAppointmentFinder();
            btnStart.Enabled = false;
            btnStop.Enabled = false;
        }

        private void InitDictionary()
        {
            var dict = new Dictionary<CountryData, string>();
            foreach (var location in m_LocationData)
            {
                dict.Add(location, location.Country);
            }
            cbCountry.DataSource = new BindingSource(dict, null);
            cbCountry.DisplayMember = "Value";
            cbCountry.ValueMember = "Key";
            cbCountry.SelectedIndex = 0;
        }

        private void ReadJsonData()
        {
            var filename = "data.json";
            if (!File.Exists(filename))
            {
                MessageBox.Show($"Die Datei {filename} konnte nicht gefunden werden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            var jsonString = File.ReadAllText(filename);
            m_LocationData = JsonSerializer.Deserialize<List<CountryData>>(jsonString);
        }

        private void InitMaskedTextbox()
        {
            mtbCode.Mask = ">AAAA-AAAA-AAAA";
            mtbCode.MaskInputRejected += new MaskInputRejectedEventHandler(mtb_Code_MaskInputRejected);
            mtbCode.KeyDown += new KeyEventHandler(mtb_Code_KeyDown);
        }

        void mtb_Code_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            if (mtbCode.MaskFull)
            {
            }
            else if (e.Position == mtbCode.Mask.Length)
            {
            }
            else
            {
                toolTip1.ToolTipTitle = "Eingabe ungültig";
                toolTip1.Show("Nur Buchstaben oder Zahlen sind zulässig.", mtbCode, 0, -20, 5000);
            }
        }

        void mtb_Code_KeyDown(object sender, KeyEventArgs e)
        {
            toolTip1.Hide(mtbCode);
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
            mtbCode.Text = mtbCode.Text.Trim();

            if (mtbCode.MaskCompleted)
            {
                cbCenter.Enabled = true;
                cbCountry.Enabled = true;
                btnStart.Enabled = true;
                m_Code = mtbCode.Text;
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
                SystemSounds.Exclamation.Play();
                MessageBox.Show($"Es ist ein Fehler aufgetreten: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            mtbCode.Enabled = b;
            cbCenter.Enabled = b;
            cbCountry.Enabled = b;
            btnStart.Enabled = b;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(m_AppointmentFinder.IsSearching())
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

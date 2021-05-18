using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Media;
using ImpfTerminBot.Model;
using ImpfTerminBot.ErrorHandling;
using System.Threading.Tasks;
using System.Threading;

namespace ImpfTerminBot.GUI
{
    public partial class View : Form
    {
        private List<CountryData> m_LocationData;
        private VaccinationAppointmentFinder m_AppointmentFinder;
        private string m_Code;
        private bool m_ShowAppointmentAvailable;

        public View()
        {
            m_AppointmentFinder = new VaccinationAppointmentFinder();
            m_AppointmentFinder.AppointmentFound += OnSuccess;
            m_AppointmentFinder.SearchCanceled += OnSearchCanceled;
            m_AppointmentFinder.SearchFailed += OnFail;

            InitializeComponent();
            ReadJsonData();
            InitCodeMaskedTextbox();
            InitDictionary();

            btnStart.Enabled = false;
            btnCancel.Enabled = false;
        }

        private void OnSuccess(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker((() => OnSuccess(sender, e))));
            }
            else
            {
                PlaySound();
                MessageBox.Show("Bitte Daten im Browser eingeben.", "Termin gefunden.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetControls();
            }
        }

        private void ResetControls()
        {
            EnableControls(true);
            btnCancel.Enabled = false;
            btnStart.Text = "Termin suchen";
            stlStatus.Text = "";
        }

        private void OnSearchCanceled(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker((() => OnSearchCanceled(sender, e))));
            }
            else
            {
                ResetControls();
            }
        }

        private void OnFail(object sender, FailEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker((() => OnFail(sender, e))));
            }
            else
            {
                SystemSounds.Exclamation.Play();
                switch (e.eErrorType)
                {
                    case eErrorType.Unknown:
                    case eErrorType.CodeNotValid:
                        {
                            MessageBox.Show($"{e.ErrorText}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
                    case eErrorType.ServerNotWorking:
                        {
                            MessageBox.Show($"{e.ErrorText}", "Server nicht aktiv", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        }
                    case eErrorType.Maintenance:
                        {
                            MessageBox.Show($"{e.ErrorText}", "Wartungsarbeiten", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        }
                    default:
                        break;
                }
                ResetControls();
            }
        }

        private void ReadJsonData()
        {
            try
            {
                var reader = new CountryDataReader();
                try
                {
                    m_LocationData = reader.ReadFromUrl();
                }
                catch (Exception e)
                {
                    var fileName = "data.json";
                    m_LocationData = reader.ReadFromFile(fileName);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Es ist ein Fehler aufgetreten: {e.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void InitCodeMaskedTextbox()
        {
            mtbCode.Mask = ">AAAA-AAAA-AAAA";
            mtbCode.MaskInputRejected += new MaskInputRejectedEventHandler(mtbCode_MaskInputRejected);
            mtbCode.KeyDown += new KeyEventHandler(mtbCode_KeyDown);
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

        void mtbCode_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            if(!mtbCode.Focused)
            {
                return;
            }

            if (mtbCode.MaskFull)
            {
            }
            else if (e.Position == mtbCode.Mask.Length)
            {
            }
            else
            {
                toolTip1.ToolTipTitle = "Eingabe ungültig";
                toolTip1.Show("Nur Buchstaben oder Zahlen sind zulässig.", mtbCode, 0, 20, 3000);
            }
        }

        void mtbCode_KeyDown(object sender, KeyEventArgs e)
        {
            toolTip1.Hide(mtbCode);
        }

        private void cbCountry_SelectionChanged(object sender, EventArgs e)
        {
            var country = ((KeyValuePair<CountryData, string>)cbCountry.SelectedItem).Key;

            var dict = new Dictionary<CenterData, string>();
            foreach (var center in country.Centers)
            {
                var displayName = $"{center.Postcode} {center.City}, {center.CenterName}";
                dict.Add(center, displayName);
            }

            cbCenter.DataSource = new BindingSource(dict, null);
            cbCenter.DisplayMember = "Value";
            cbCenter.ValueMember = "Key";
            cbCenter.SelectedIndex = 0;
        }

        private void mtbCode_TextChanged(object sender, EventArgs e)
        {
            if (mtbCode.MaskCompleted)
            {
                mtbCode.Text = mtbCode.Text.Trim();

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

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnCancel.Enabled = true;
                EnableControls(false);

                if(m_AppointmentFinder.IsSearching() && !m_AppointmentFinder.IsStopped())
                {
                    m_AppointmentFinder.StopSearch(true);
                    btnStart.Text = "Suche fortsetzen";
                    stlStatus.Text = "SUCHE GESTOPPT";
                }
                else if(m_AppointmentFinder.IsSearching() && m_AppointmentFinder.IsStopped())
                {
                    m_AppointmentFinder.StopSearch(false);
                    btnStart.Text = "Suche stoppen";
                    stlStatus.Text = "SUCHE LÄUFT";
                }
                else
                {
                    var browser = GetBrowserType();
                    var country = ((KeyValuePair<CountryData, string>)cbCountry.SelectedItem).Key;
                    var center = ((KeyValuePair<CenterData, string>)cbCenter.SelectedItem).Key;

                    m_AppointmentFinder.SearchAsync(browser, m_Code, center);
                    btnStart.Text = "Suche stoppen";
                    stlStatus.Text = "SUCHE LÄUFT";
                }
            }
            catch (Exception ex)
            {
                SystemSounds.Exclamation.Play();
                MessageBox.Show($"Es ist ein Fehler aufgetreten: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetControls();
            }
        }

        private eBrowserType GetBrowserType()
        {
            eBrowserType browser = eBrowserType.None;
            if (rbChrome.Checked)
            {
                browser = eBrowserType.Chrome;
            }
            else if (rbFirefox.Checked)
            {
                browser = eBrowserType.Firefox;
            }

            return browser;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            m_AppointmentFinder.CancelSearch();
        }

        private void PlaySound()
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
            rbChrome.Enabled = b;
            rbFirefox.Enabled = b;
            btnSoundTest.Enabled = b;
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

        private async void btnSoundTest_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                PlaySound();
            });
        }
    }
}

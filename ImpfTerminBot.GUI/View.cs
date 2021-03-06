using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Media;
using ImpfTerminBot.Model;
using ImpfTerminBot.ErrorHandling;
using System.Threading.Tasks;
using static System.Environment;

namespace ImpfTerminBot.GUI
{
    public partial class View : Form
    {
        private List<CountryData> m_LocationData;
        private VaccinationAppointmentFinder m_AppointmentFinder;
        private string m_Code;
        private string m_Path = "";
        private string m_PersonalDataFileName = "personalData.bin";

        public View()
        {
            m_AppointmentFinder = new VaccinationAppointmentFinder();
            m_AppointmentFinder.AppointmentFound += OnSuccess;
            m_AppointmentFinder.SearchCanceled += OnSearchCanceled;
            m_AppointmentFinder.SearchFailed += OnFail;

            CreateProgramDataDir();
            InitializeComponent();
            ReadCountryData();
            InitCodeMaskedTextbox();
            InitCountryDataComboBox();
            InitSalutationComboBox();
            LoadPersonalData();
            EnablePersonalData(false);

            btnStart.Enabled = false;
            btnCancel.Enabled = false;

            SubscribeEvents();
        }

        private void CreateProgramDataDir()
        {
            var commonpath = Environment.GetFolderPath(SpecialFolder.CommonApplicationData);
            m_Path = Path.Combine(commonpath, "ImpfTerminBot");

            if (!Directory.Exists(m_Path))
            {
                Directory.CreateDirectory(m_Path);
            }
        }

        private void SubscribeEvents()
        {
            cbSalutation.SelectedIndexChanged += new EventHandler(tbPersonalData_TextChanged);
            tbCity.TextChanged += new EventHandler(tbPersonalData_TextChanged);
            tbEmail.TextChanged += new EventHandler(tbPersonalData_TextChanged);
            tbName.TextChanged += new EventHandler(tbPersonalData_TextChanged);
            tbHouseNumber.TextChanged += new EventHandler(tbPersonalData_TextChanged);
            tbFirstname.TextChanged += new EventHandler(tbPersonalData_TextChanged);
            tbPhone.TextChanged += new EventHandler(tbPersonalData_TextChanged);
            tbPostcode.TextChanged += new EventHandler(tbPersonalData_TextChanged);
            tbStreet.TextChanged += new EventHandler(tbPersonalData_TextChanged);
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

        private void ReadCountryData()
        {
            try
            {
                var reader = new CountryDataReader();
                m_LocationData = reader.ReadFromUrl();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Es ist ein Fehler aufgetreten: {e.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void LoadPersonalData()
        {
            if (File.Exists(GetPersonalDataFilePath()))
            {
                var serializer = new PersonalDataBinarySerializer();
                var personalData = serializer.Deserialize(GetPersonalDataFilePath());
                SetPersonalData(personalData);
            }
        }

        private string GetPersonalDataFilePath()
        {
            return Path.Combine(m_Path, m_PersonalDataFileName);
        }

        private void SavePersonalData()
        {
            var personalData = GetPersonalData();
            var serializer = new PersonalDataBinarySerializer();
            serializer.Serialize(personalData, GetPersonalDataFilePath());
        }

        private void InitCodeMaskedTextbox()
        {
            mtbCode.Mask = ">AAAA-AAAA-AAAA";
            mtbCode.MaskInputRejected += new MaskInputRejectedEventHandler(mtbCode_MaskInputRejected);
            mtbCode.KeyDown += new KeyEventHandler(mtbCode_KeyDown);
        }

        private void InitCountryDataComboBox()
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

        private void InitSalutationComboBox()
        {
            var dict = new Dictionary<eSalutation, string>()
            {
                {eSalutation.Sir, "Herr" },
                {eSalutation.Lady, "Frau" },
                {eSalutation.Divers, "Divers" },
            };
            cbSalutation.DataSource = new BindingSource(dict, null);
            cbSalutation.DisplayMember = "Value";
            cbSalutation.ValueMember = "Key";
            cbSalutation.SelectedIndex = 0;
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
                m_Code = mtbCode.Text;
            }
            else
            {
                cbCenter.Enabled = false;
                cbCountry.Enabled = false;
                btnStart.Enabled = false;
                m_Code = "";
            }
            EnableStartButton();
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

                    m_AppointmentFinder.PersonalData = null;
                    if (cbAutoBook.Checked)
                    {
                        var personalData = GetPersonalData();
                        m_AppointmentFinder.PersonalData = personalData;
                    }

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
            cbAutoBook.Enabled = b;
            if(cbAutoBook.Checked)
            {
                EnablePersonalData(b);
            }
            else
            {
                EnablePersonalData(false);
            }
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

        private void cbAutoBook_CheckedChanged(object sender, EventArgs e)
        {
            var isAutoBook = cbAutoBook.Checked;
            EnablePersonalData(isAutoBook);
            EnableStartButton();
        }

        private void EnableStartButton()
        {
            if (cbAutoBook.Checked && mtbCode.MaskCompleted)
            {
                var personalData = GetPersonalData();
                btnStart.Enabled = personalData.IsComplete();
            }
            else
            {
                btnStart.Enabled = mtbCode.MaskCompleted;
            }
        }

        private PersonalData GetPersonalData()
        {
            eSalutation salutation = ((KeyValuePair<eSalutation, string>)cbSalutation.SelectedItem).Key;
            return new PersonalData()
            {
                Salutation = salutation,
                City = tbCity.Text,
                FirstName = tbFirstname.Text,
                Name = tbName.Text,
                Email = tbEmail.Text,
                HouseNumber = tbHouseNumber.Text,
                Phone = tbPhone.Text,
                Postcode = tbPostcode.Text,
                Street = tbStreet.Text
            };
        }

        private void SetPersonalData(PersonalData personalData)
        {
            cbSalutation.SelectedIndex = (int)personalData.Salutation;
            tbCity.Text = personalData.City;
            tbName.Text = personalData.Name;
            tbFirstname.Text = personalData.FirstName;
            tbEmail.Text = personalData.Email;
            tbHouseNumber.Text = personalData.HouseNumber;
            tbPhone.Text = personalData.Phone;
            tbPostcode.Text = personalData.Postcode;
            tbStreet.Text = personalData.Street;
        }

        private void EnablePersonalData(bool b)
        {
            cbSalutation.Enabled = b;
            tbFirstname.Enabled = b;
            tbName.Enabled = b;
            tbPostcode.Enabled = b;
            tbCity.Enabled = b;
            tbStreet.Enabled = b;
            tbHouseNumber.Enabled = b;
            tbPhone.Enabled = b;
            tbEmail.Enabled = b;
        }

        private void tbPersonalData_TextChanged(object sender, EventArgs e)
        {
            SavePersonalData();
            EnableStartButton();
        }
    }
}

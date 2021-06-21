using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ImpfTerminBot.ErrorHandling;
using ImpfTerminBot.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace ImpfTerminBot
{

    public class VaccinationAppointmentFinder
    {
        private string m_Code;
        private eBrowserType m_BrowserType;
        private string m_Country;
        private string m_StartUrl;
        private CenterData m_CenterData;
        private IWebDriver m_Driver;
        private bool m_IsSearching;
        private bool m_IsStop;

        public PersonalData PersonalData { get; set; }
        public int LoopWaitTime_ms { get; set; } = 1000;
        public int FindElementTimeout_ms { get; set; } = 15000;
        public int StopWaitTime_ms { get; set; } = 10;
        public int NavigateToStartWaitTime_ms { get; set; } = 2000;
        public int AfterClickWaitTime_ms { get; set; } = 2000;
        public int AfterSendKeysWaitTime_ms { get; set; } = 2000;
        public int m_WaitTimeTillDriverRestart_ms { get; set; } = 15000;

        public event EventHandler AppointmentFound;
        public event EventHandler SearchCanceled;
        public event EventHandler<FailEventArgs> SearchFailed;

        public VaccinationAppointmentFinder()
        {
            m_IsSearching = false;
            m_IsStop = false;
        }

        private void OnSuccess()
        {
            m_IsSearching = false;
            m_IsStop = false;
            AppointmentFound?.Invoke(this, null);
        }

        private void OnSearchCanceled()
        {
            SearchCanceled?.Invoke(this, null);
        }

        private void OnFail(FailEventArgs e)
        {
            m_IsSearching = false;
            m_IsStop = false;
            SearchFailed?.Invoke(this, e);
        }

        public void SearchAsync(eBrowserType browserType, string code,CenterData centerData)
        {
            try
            {
                m_BrowserType = browserType;
                m_Country = centerData.Country;
                m_Code = code;
                m_CenterData = centerData;

                StartDriver();

                m_IsSearching = true;
                m_IsStop = false;
                SearchAsync();
            }
            catch (Exception)
            {
                m_IsSearching = false;
                throw;
            }
        }

        private void StartDriver()
        {
            m_Driver = CreateBrowserDriver(m_BrowserType);
            m_StartUrl = $"{m_CenterData.Url}impftermine/";
            m_Driver.Navigate().GoToUrl(m_StartUrl);
        }

        private IWebDriver CreateBrowserDriver(eBrowserType browserType)
        {
            IWebDriver driver;
            switch (browserType)
            {
                case eBrowserType.Chrome:
                    {
                        var chromeDriverService = ChromeDriverService.CreateDefaultService();
                        chromeDriverService.HideCommandPromptWindow = true;
                        driver = new ChromeDriver(chromeDriverService);
                        break;
                    }
                case eBrowserType.Firefox:
                    {
                        var geckoService = FirefoxDriverService.CreateDefaultService();
                        geckoService.Host = "::1";
                        geckoService.HideCommandPromptWindow = true;
                        var firefoxOptions = new FirefoxOptions();
                        firefoxOptions.AcceptInsecureCertificates = true;
                        driver = new FirefoxDriver(geckoService, firefoxOptions);
                        break;
                    }
                default:
                    throw new Exception("Unbekannter Browser.");
            }
            return driver;
        }

        public void CancelSearch()
        {
            m_IsSearching = false;
            m_IsStop = false;
            OnSearchCanceled();
        }

        public void StopSearch(bool b)
        {
            m_IsStop = b;
        }

        public void SearchAsync()
        {
            Task.Factory.StartNew(() =>
                {
                    while (m_IsSearching)
                    {
                        try
                        {
                            CheckStop();
                            if (!m_IsSearching)
                            {
                                break;
                            }

                            if (IsError())
                            {
                                break;
                            }

                            if (PageContains("Cookie Hinweis"))
                            {
                                HandleAcceptCookies();
                            }

                            if (PageContains("Buchen Sie die Termine für Ihre Corona-Schutzimpfung"))
                            {
                                HandlePageBooking();
                            }
                            CheckStop();
                            if (!m_IsSearching)
                            {
                                break;
                            }

                            if (PageContains("Vermittlungscodes bereits vorhanden"))
                            {
                                HandlePageCodeExist();
                            }
                            CheckStop();
                            if (!m_IsSearching)
                            {
                                break;
                            }

                            if (PageContains("Onlinebuchung für Ihre Corona-Schutzimpfung"))
                            {
                                if (HandlePageAppointmentSearch())
                                {
                                    OnSuccess();
                                    return;
                                }
                                else
                                {
                                    NavigateToStart();
                                }
                            }
                            CheckStop();
                            if (!m_IsSearching)
                            {
                                break;
                            }

                            if (PageContains("Es ist ein interner Fehler aufgetreten"))
                            {
                                NavigateToStart();
                            }
                            CheckStop();
                            if (!m_IsSearching)
                            {
                                break;
                            }

                            if (PageContains("Es ist ein unerwarteter Fehler aufgetreten"))
                            {
                                //RestartDriverAndWait();
                                NavigateToStart();
                            }
                            CheckStop();
                            Thread.Sleep(LoopWaitTime_ms);
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                m_Driver.Navigate().GoToUrl(m_StartUrl);
                            }
                            catch (WebDriverException ex)
                            {
                                m_IsSearching = false;
                                OnFail(new FailEventArgs() { ErrorText = e.Message });
                                
                                break;
                            }
                        }
                    }
                    CloseBrowser();
                }      
            );
        }

        private void RestartDriverAndWait()
        {
            var pos = m_Driver.Manage().Window.Position;
            var size = m_Driver.Manage().Window.Size;
            CloseBrowser();
            Thread.Sleep(m_WaitTimeTillDriverRestart_ms);
            StartDriver();
            m_Driver.Manage().Window.Position = pos;
            m_Driver.Manage().Window.Size = size;
        }

        private bool IsError()
        {
            if (PageContains("Ungültiger Vermittlungscode"))
            {
                var errMsg = "Ungültiger Vermittlungscode.";
                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.CodeNotValid });
                return true;
            }

            if (PageContains("Anspruch abgelaufen"))
            {
                var errMsg = "Anspruch abgelaufen. Vermittlungscode ist nicht mehr gültig.";
                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.CodeNotValid });
                return true;
            }

            if (PageContains("Derzeit keine Onlinebuchung von Impfterminen"))
            {
                var errMsg = "Der gewählte Server bietet derzeit keine Onlinebuchung von Impfterminen. Bitte andere Server-Nummer wählen.";
                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.ServerNotWorking });
                return true;
            }

            if (PageContains("Wartungsarbeiten"))
            {
                var errMsg = "Es werden momentan Wartungsarbeiten durchgeführt. Bitte später erneut versuchen.";
                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.Maintenance });
                return true;
            }
            return false;
        }

        private void CheckStop()
        {
            while (m_IsStop)
            {
                Thread.Sleep(StopWaitTime_ms);
            }
        }

        private void NavigateToStart()
        {
            Thread.Sleep(NavigateToStartWaitTime_ms);
            m_Driver.Navigate().GoToUrl(m_StartUrl);
        }

        public void CloseBrowser()
        {
            m_Driver?.Quit();
        }

        public bool IsSearching()
        {
            return m_IsSearching;
        }

        public bool IsStopped()
        {
            return m_IsStop;
        }

        public bool PageContains(string text)
        {
            return Exists(By.XPath($"//*[contains(., '{text}')]"));
        }

        public bool Exists(By by)
        {
            if (m_Driver.FindElements(by).Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void HandleAcceptCookies()
        {
            var btnSelector = By.XPath("//a[(@class='cookies-info-close btn kv-btn btn-magenta')]");
            Click(btnSelector);
        }

        private void HandlePageBooking()
        {
            var select1 = By.XPath("//*[@data-select2-id=2]");
            Click(select1);

            var country = By.XPath($"//*[contains(@id, '{m_Country}')]");
            Click(country);

            var select2 = By.XPath("//*[@data-select2-id=5]");
            Click(select2);

            var center =By.XPath($"//li[contains(., '{m_CenterData.Postcode}')]");
            Click(center);

            var btnSelector = By.XPath("//button[@class='btn kv-btn btn-magenta text-uppercase d-inline-block']");
            Click(btnSelector);
        }

        private void Click(By btnSelector)
        {
            var wait = new WebDriverWait(m_Driver, TimeSpan.FromMilliseconds(FindElementTimeout_ms));
            wait.Until(ExpectedConditions.ElementExists(btnSelector));
            wait.Until(ExpectedConditions.ElementToBeClickable(btnSelector));

            var element = m_Driver.FindElement(btnSelector);
            PerformMouseMoveTo(element);

            element.Click();
            Thread.Sleep(AfterClickWaitTime_ms);
        }

        private void PerformMouseMoveTo(IWebElement element)
        {
            var action = new Actions(m_Driver);
            action.MoveToElement(element).Perform();
            Thread.Sleep(10);
        }

        private void PerformMouseMoveTo(By selector)
        {
            var element = m_Driver.FindElement(selector);
            var action = new Actions(m_Driver);
            action.MoveToElement(element).Perform();
            Thread.Sleep(10);
        }

        private bool HandlePageAppointmentSearch()
        {
            try
            {
                var btnSelector = By.XPath("//button[@class='btn btn-magenta kv-btn kv-btn-round search-filter-button']");
                Click(btnSelector);

                var isSuccess = Exists(By.XPath("//*[contains(., '1. Impftermin')]")) &&
                                !Exists(By.XPath("//*[contains(., 'Derzeit stehen leider keine Termine zur Verfügung.')]"));

                if(isSuccess && PersonalData != null)
                {
                    // Click 1. Termin
                    var inputFirstAppointment = By.XPath("(//div[@class='its-slot-pair-search-radio-btn'])[1]");
                    Click(inputFirstAppointment);

                    // Click Auswählen
                    var buttonChoose = By.XPath("//button[contains(.,'AUSWÄHLEN')]");
                    Click(buttonChoose);

                    // Click Daten erfassen
                    var buttonData = By.XPath("//button[contains(.,'Daten erfassen')]");
                    Click(buttonData);

                    // Persönliche Daten ausfüllen
                    FillPersonalData();

                    // Click Übernehmen
                    var buttonOk = By.XPath("//button[contains(.,'Übernehmen')]");
                    Click(buttonOk);

                    // Click Buchen
                    var buttonBook = By.XPath("//button[contains(.,'VERBINDLICH BUCHEN')]");
                    Click(buttonBook);
                }

                return isSuccess;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void FillPersonalData()
        {
            By salutation = null;
            switch (PersonalData.Salutation)
            {
                case eSalutation.Sir:
                    {
                        //inputSalutation = By.XPath("(//input[@value='Herr'])[1]");
                        salutation = By.XPath("//label[@class='ets-radio-control'][1]");
                        break;
                    }
                case eSalutation.Lady:
                    {
                        //inputSalutation = By.XPath("(//input[@value='Frau'])[1]");
                        salutation = By.XPath("//label[@class='ets-radio-control'][2]");
                        break;
                    }
                case eSalutation.Divers:
                    {
                        //inputSalutation = By.XPath("(//input[@value='Divers'])[1]");
                        salutation = By.XPath("//label[@class='ets-radio-control'][3]");
                        break;
                    }
            }
            Click(salutation);

            var firstName = By.XPath("//input[@formcontrolname='firstname']");
            SendKeys(firstName, PersonalData.FirstName);

            var name = By.XPath("//input[@formcontrolname='lastname']");
            SendKeys(name, PersonalData.Name);

            var postCode = By.XPath("//input[@formcontrolname='zip']");
            SendKeys(postCode, PersonalData.Postcode);

            var city = By.XPath("//input[@formcontrolname='city']");
            SendKeys(city, PersonalData.City);

            var street = By.XPath("//input[@formcontrolname='street']");
            SendKeys(street, PersonalData.Street);

            var housenumber = By.XPath("//input[@formcontrolname='housenumber']");
            SendKeys(housenumber, PersonalData.HouseNumber);

            var phone = By.XPath("//input[@formcontrolname='phone']");
            SendKeys(phone, PersonalData.Phone);

            var email = By.XPath("//input[@formcontrolname='notificationReceiver']");
            SendKeys(email, PersonalData.Email);
        }

        private void SendKeys(By selector, string content)
        {
            var wait = new WebDriverWait(m_Driver, TimeSpan.FromMilliseconds(FindElementTimeout_ms));
            wait.Until(ExpectedConditions.ElementExists(selector));
            wait.Until(ExpectedConditions.ElementToBeClickable(selector));

            var element = m_Driver.FindElement(selector);
            PerformMouseMoveTo(element);
            element.SendKeys(content);
            Thread.Sleep(AfterSendKeysWaitTime_ms);
        }

        private void HandlePageCodeExist()
        {
            var labelYes = By.XPath("//label[@class='ets-radio-control'][1]");
            var labelNo = By.XPath("//label[@class='ets-radio-control'][2]");
            Click(labelYes);

            var text1 = By.XPath("//input[@name='ets-input-code-0']");
            SendKeys(text1, m_Code);

            var btnSelector = By.XPath("//button[@class='btn kv-btn btn-magenta text-uppercase d-inline-block']");
            Click(btnSelector);

            if (Exists(By.XPath("//*[contains(., 'Es ist ein unerwarteter Fehler aufgetreten')]")))
            {
                //RestartDriverAndWait();
                NavigateToStart();
            }
        }

        public async Task<VaccinationAppointmentResult> IsAppointmentAvailable(CenterData centerData)
        {
            var postCode = centerData.Postcode;
            var url = $"https://005-iz.impfterminservice.de/rest/suche/termincheck";
            var urlParams = $"?plz={postCode}&leistungsmerkmale=" +
                $"{VaccinationAppointmentGlobals.VaccinesDict[eVaccines.Biontech]}," +
                $"{VaccinationAppointmentGlobals.VaccinesDict[eVaccines.Moderna]}," +
                $"{VaccinationAppointmentGlobals.VaccinesDict[eVaccines.AstraZeneca]}";

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            var response = await client.GetAsync(urlParams);
            if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
            {
                var content = await response.Content.ReadAsStringAsync();

                var parser = new VaccinationAppointmentResultParser();
                return  parser.Parse(content);
            }
            return null;
        }
    }
}

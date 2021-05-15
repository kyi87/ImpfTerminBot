using System;
using System.Threading;
using System.Threading.Tasks;
using ImpfTerminBot.ErrorHandling;
using ImpfTerminBot.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace ImpfTerminBot
{
    public class VaccinationAppointmentFinder
    {
        private string m_Code;
        private string m_Country;
        private string m_StartUrl;
        private CenterData m_CenterData;
        private IWebDriver m_Driver;
        private bool m_IsSearching;
        private bool m_IsStop;
        private int m_SuccessWaitTime_ms;

        public event EventHandler AppointmentFound;
        public event EventHandler SearchCanceled;
        public event EventHandler<FailEventArgs> SearchFailed;

        public VaccinationAppointmentFinder(int successWaitTime_Ms = 5000)
        {
            m_IsSearching = false;
            m_IsStop = false;
            m_SuccessWaitTime_ms = successWaitTime_Ms;
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

        public void SearchAsync(eBrowserType browserType, int server, string code, string country, CenterData centerData)
        {
            try
            {
                m_Driver = CreateBrowserDriver(browserType);

                m_Country = country;
                m_Code = code;
                m_CenterData = centerData;

                var postcode = m_CenterData.Postcode;
                m_StartUrl = $"https://{server:000}-iz.impfterminservice.de/impftermine/suche/{code}/{postcode}";
                m_Driver.Navigate().GoToUrl(m_StartUrl);

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

        private IWebDriver CreateBrowserDriver(eBrowserType browserType)
        {
            IWebDriver driver;
            switch (browserType)
            {
                case eBrowserType.Chrome:
                    {
                        driver = new ChromeDriver();
                        break;
                    }
                case eBrowserType.Firefox:
                    {
                        FirefoxDriverService geckoService = FirefoxDriverService.CreateDefaultService();
                        geckoService.Host = "::1";
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
                            Thread.Sleep(1000);

                            if (PageContains("Ungültiger Vermittlungscode"))
                            {
                                var errMsg = "Ungültiger Vermittlungscode.";
                                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.CodeNotValid });
                                break;
                            }

                            if (PageContains("Anspruch abgelaufen"))
                            {
                                var errMsg = "Anspruch abgelaufen. Vermittlungscode ist nicht mehr gültig.";
                                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.CodeNotValid });
                                break;
                            }

                            if(PageContains("Derzeit keine Onlinebuchung von Impfterminen"))
                            {
                                var errMsg = "Der gewählte Server bietet derzeit keine Onlinebuchung von Impfterminen. Bitte andere Server-Nummer wählen.";
                                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.ServerNotWorking });
                                break;
                            }

                            if (PageContains("Wartungsarbeiten"))
                            {
                                var errMsg = "Es werden momentan Wartungsarbeiten durchgeführt. Bitte später erneut versuchen.";
                                OnFail(new FailEventArgs() { ErrorText = errMsg, eErrorType = eErrorType.Maintenance });
                                break;
                            }

                            if (PageContains("Buchen Sie die Termine für Ihre Corona-Schutzimpfung"))
                            {
                                HandlePageBooking();
                            }

                            if (PageContains("Vermittlungscodes bereits vorhanden"))
                            {
                                HandlePageCodeExist();
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
                                    m_Driver.Navigate().GoToUrl(m_StartUrl);
                                }
                            }

                            if (PageContains("Es ist ein interner Fehler aufgetreten"))
                            {
                                m_Driver.Navigate().GoToUrl(m_StartUrl);
                            }

                            if (PageContains("Es ist ein unerwarteter Fehler aufgetreten"))
                            {
                                m_Driver.Navigate().GoToUrl(m_StartUrl);
                            }

                            while (m_IsStop)
                            {
                                Thread.Sleep(100);
                            }
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

        private void HandlePageBooking()
        {
            var select1 = m_Driver.FindElement(By.XPath("//*[@data-select2-id=2]"));
            select1.Click();
            Thread.Sleep(500);
            var country = m_Driver.FindElement(By.XPath($"//*[contains(@id, '{m_Country}')]"));
            country.Click();
            Thread.Sleep(500);

            var select2 = m_Driver.FindElement(By.XPath("//*[@data-select2-id=5]"));
            select2.Click();
            Thread.Sleep(500);
            var center = m_Driver.FindElement(By.XPath($"//li[contains(., '{m_CenterData.Postcode}')]"));
            center.Click();
            Thread.Sleep(500);

            var btnSelector = By.CssSelector("button[class='btn kv-btn btn-magenta text-uppercase d-inline-block']");
            ClickButton(btnSelector);
        }

        private void ClickButton(By btnSelector)
        {
            var wait = new WebDriverWait(m_Driver, TimeSpan.FromSeconds(15));
            wait.Until(ExpectedConditions.ElementExists(btnSelector));
            wait.Until(ExpectedConditions.ElementToBeClickable(btnSelector));

            var btn = m_Driver.FindElement(btnSelector);
            btn.Click();
        }

        private bool HandlePageAppointmentSearch()
        {
            try
            {
                var btnSelector = By.CssSelector("button[class='btn btn-magenta kv-btn kv-btn-round search-filter-button']");
                ClickButton(btnSelector);

                Thread.Sleep(m_SuccessWaitTime_ms);
                var isSuccess = Exists(By.XPath("//*[contains(., '1. Impftermin')]")) &&
                                !Exists(By.XPath("//*[contains(., 'Derzeit stehen leider keine Termine zur Verfügung.')]"));
                return isSuccess;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void HandlePageCodeExist()
        {
            var labels = m_Driver.FindElements(By.CssSelector("label[class='ets-radio-control']"));
            var labelYes = labels[0];
            labelYes.Click();
            Thread.Sleep(500);

            var text1 = m_Driver.FindElement(By.CssSelector("input[name='ets-input-code-0']"));
            text1.SendKeys(m_Code);
            Thread.Sleep(500);

            var btnSelector = By.CssSelector("button[class='btn kv-btn btn-magenta text-uppercase d-inline-block']");
            ClickButton(btnSelector);

            Thread.Sleep(500);
            if (Exists(By.XPath("//*[contains(., 'Es ist ein unerwarteter Fehler aufgetreten')]")))
            {
                Thread.Sleep(2000);
                m_Driver.Navigate().GoToUrl(m_StartUrl);
            }
        }
    }
}

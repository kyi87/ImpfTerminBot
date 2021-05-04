using ImpfTerminBot.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using Microsoft.Edge.SeleniumTools;
using ImpfTerminBot.GUI.Model;

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

        public VaccinationAppointmentFinder()
        {
            m_IsSearching = false;
        }

        public async Task<bool> Search(eBrowserType browserType, int server, string code, string country, CenterData centerData)
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
                return await SelectPage();
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
                case eBrowserType.Edge:
                    {
                        var options = new EdgeOptions();
                        options.UseChromium = true;
                        driver = new EdgeDriver(options);
                        break;
                    }
                default:
                    throw new Exception("Unbekannter Browser.");
            }
            return driver;
        }

        public void StopSearch()
        {
            m_IsSearching = false;
        }

        public async Task<bool> SelectPage()
        {
            return await Task.Factory.StartNew(() =>
                {
                    while (m_IsSearching)
                    {
                        Thread.Sleep(1000);

                        if (PageContains("Ungültiger Vermittlungscode"))
                        {
                            throw new CodeNotValidException("Ungültiger Vermittlungscode.");
                        }

                        if (PageContains("Anspruch abgelaufen"))
                        {
                            throw new CodeNotValidException("Anspruch abgelaufen. Vermittlungscode ist nicht mehr gültig.");
                        }

                        if(PageContains("Derzeit keine Onlinebuchung von Impfterminen"))
                        {
                            throw new ServerNotWorkingException("Der gewählte Server bietet derzeit keine Onlinebuchung von Impfterminen. Bitte andere Server-Nummer wählen.");
                        }

                        try
                        {
                            if (PageContains("Buchen Sie die Termine für Ihre Corona-Schutzimpfung"))
                            {
                                HandlePageBooking();
                            }
                            if (PageContains("Vermittlungscodes bereits vorhanden"))
                            {
                                HandlePageCodeExist();
                            }
                            if (PageContains("Termine suchen"))
                            {
                                if (HandlePageAppointmentSearch())
                                {
                                    return true;
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
                        }
                        catch (Exception e)
                        {
                            m_Driver.Navigate().GoToUrl(m_StartUrl);
                        }
                    }

                    CloseBrowser();
                    return false;
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

            var btn = m_Driver.FindElement(By.CssSelector("button[class='btn kv-btn btn-magenta text-uppercase d-inline-block']"));
            btn.Click();
        }

        private bool HandlePageAppointmentSearch()
        {
            var btn = m_Driver.FindElement(By.CssSelector("button[class='btn btn-magenta kv-btn kv-btn-round search-filter-button']"));
            btn.Click();
            Thread.Sleep(2000);

            var isSuccess = Exists(By.XPath("//*[contains(., '1. Impftermin')]")) && 
                            !Exists(By.XPath("//*[contains(., 'Derzeit stehen leider keine Termine zur Verfügung.')]"));
            return isSuccess;
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

            var btn = m_Driver.FindElement(By.CssSelector("button[class='btn kv-btn btn-magenta text-uppercase d-inline-block']"));
            btn.Click();
            Thread.Sleep(500);

            if (Exists(By.XPath("//*[contains(., 'Es ist ein unerwarteter Fehler aufgetreten')]")))
            {
                Thread.Sleep(2000);
                m_Driver.Navigate().GoToUrl(m_StartUrl);
            }
        }
    }
}

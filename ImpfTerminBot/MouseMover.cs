using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ImpfTerminBot
{
    public class MouseMover
    {
        private IWebDriver m_Driver;

        public MouseMover(IWebDriver driver)
        {
            m_Driver = driver;
        }

        public void ResetMousePosition()
        {
            var element = m_Driver.FindElement(By.XPath("//div[@class='app-wrapper']"));
            var action = new Actions(m_Driver);
            ScrollToView(element);
            action.MoveToElement(element).Perform();
        }

        public void ScrollTo(int xPosition = 0, int yPosition = 0)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)m_Driver; 
            js.ExecuteScript(String.Format("window.scrollTo({0}, {1})", xPosition, yPosition));
        }

        public IWebElement ScrollToView(By selector)
        {
            var element = m_Driver.FindElement(selector);
            ScrollToView(element);
            return element;
        }

        public void ScrollToView(IWebElement element)
        {
            if (element.Location.Y > 200)
            {
                ScrollTo(0, element.Location.Y - 100); // Make sure element is in the view but below the top navigation pane
            }

        }


        public Point MoveMouseToPosition(int x, int y)
        {
            var winSize = GetWindowSize();
            if (x >= winSize.Width)
            {
                x = winSize.Width;
            }
            if (x <= 0)
            {
                x = 0;
            }
            if (y >= winSize.Height)
            {
                y = winSize.Height;
            }
            if (y <= 0)
            {
                y = 0;
            }

            var action = new Actions(m_Driver);
            action.MoveByOffset(x, y).Perform();

            return new Point(x, y);
        }

        private Size GetWindowSize()
        {
            return m_Driver.Manage().Window.Size;
        }
    }
}

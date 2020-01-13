using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.DevTools.Console;
using OpenQA.Selenium.Support.UI;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace RelativeLocators
{
    [TestFixture]
    public class Chrome_Sample_test
    {
        private IWebDriver driver;
        public string homeURL;

        [Test(Description = "Use Relative Locators")]
        public void GetBookByRelativeLocator()
        {
            driver.Navigate().GoToUrl(homeURL);
            driver.Manage().Window.Maximize();
            WebDriverWait wait = new WebDriverWait(driver,
                System.TimeSpan.FromSeconds(15));
            wait.Until(driver =>
                driver.FindElement(By.CssSelector("#demo-page")));

            var javaForTestersBook =
                driver.FindElement(
                    RelativeBy.WithTagName("li")
                        // To the left of "Advanced Selenium in Java"
                        .LeftOf(By.Id("pid6"))
                        // And Below "Test Automation in the Real World"
                        .Below(By.Id("pid1")))
                    .GetAttribute("id");

            Assert.AreEqual(javaForTestersBook, "pid5");
        }

        [TearDown]
        public void TearDownTest()
        {
            driver.Close();
        }

        [SetUp]
        public async Task SetupTestAsync()
        {
            homeURL = "https://automationbookstore.dev/";
            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("--start-maximized");
            driver = new ChromeDriver(chromeOptions);

            await SetupCdpAsync();
        }

        /// <summary>
        /// Ignoring relative locators for a moment - use chrome devtools protocol to interact with the chrome console.
        /// </summary>
        /// <returns></returns>
        public async Task SetupCdpAsync()
        {
            // Cast to role-based interface without throwing exception.
            IDevTools devTools = driver as IDevTools;
            if (devTools == null)
            {
                throw new Exception("Driver instance does not support CDP integration");
            }

            DevToolsSession session = devTools.CreateDevToolsSession();

            string expected = "Hello Selenium";
            string actual = string.Empty;

            // Two things to note here. First, use a synchronization
            // object allow us to wait for the event to fire. Second,
            // we set up an EventHandler<T> instance so we can properly
            // detach from the event once we're done. "Console" here
            // is "OpenQA.Selenium.DevTools.Console".
            ManualResetEventSlim sync = new ManualResetEventSlim(false);
            EventHandler<MessageAddedEventArgs> messageAddedHandler = (sender, e) =>
            {
                actual = e.Message.Text;
                sync.Set();
            };

            // Attach the event handler
            session.Console.MessageAdded += messageAddedHandler;

            // Enable the Console CDP domain. Note this is an async API.
            await session.Console.Enable();

            // Navigate to a page, and execute JavaScript to write to the console.
            driver.Url = "https://automationbookstore.dev/";
            ((IJavaScriptExecutor)driver).ExecuteScript("console.log('" + expected + "');");

            // Wait up to five seconds for the event to have fired.
            sync.Wait(TimeSpan.FromSeconds(5));

            // Detach the event handler, and disable the Console domain.
            session.Console.MessageAdded -= messageAddedHandler;
            await session.Console.Disable();

            Console.WriteLine("Expected message: {0}", expected);
            Console.WriteLine("Actual message: {0}", actual);
            Console.WriteLine("Messages are equal: {0}", expected == actual);
        }
    }
}
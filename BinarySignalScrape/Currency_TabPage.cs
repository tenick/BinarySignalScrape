using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NodaTime;

namespace BinarySignalScrape
{
    /// <summary>
    /// A TabPage with URL field and asynchronous Start/Stop methods for getting data in its specified URL.
    /// </summary>
    class Currency_TabPage : TabPage
    {
        #region FIELDS
        private string URL;
        private string Symbol;
        private bool Terminate;
        private int ChildNo;
        private string CurrentPrice_Direction;
        private string CurrentExpiryTime;
        private string FileName;
        private bool Repeat;
        private bool StopRequested;
        public string FilePath { private get; set; }
        public bool Stopped { get; private set; }

        private bool SyncVisible = false;
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Creates an instance of Currency_TabPage and sets its URL field.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="path"></param>
        public Currency_TabPage(string url, int childNo)
        {
            ChildNo = childNo;
            Stopped = true;
            URL = url;
            string currencyPair = url.Substring(url.Length - 6);
            FileName = currencyPair + ".txt";
            Symbol = FileName.Substring(0, FileName.Length - 4).ToUpper().Insert(3, "/");
            InitializeComponent();
        }
        #endregion

        #region METHODS
        public async void Start()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    StopRequested = false;
                    Terminate = false;
                    Repeat = false;
                    Stopped = false;
                    IWebDriver driver;
                    ChromeOptions options = new ChromeOptions();
                    // ChromeDriver is just AWFUL because every version or two it breaks unless you pass cryptic arguments
                    ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();

                    driver = new ChromeDriver(chromeDriverService, options);   // initializes driver
                    LogIn(driver);

                    // HOLY THIS IS IT, READ THE 2ND ANSWER IN THIS LINK https://stackoverflow.com/questions/26937141/how-to-stop-selenium-webdriver-from-waiting-for-page-load
                    // BASICALLY IT SAYS "if the exception is thrown because of timeout then we can't restore same session so need to create new instance." THAT'S WHY WE GET THE FUCKING BLANK PAGE WHEN REFRESHING
                    // THE PROBLEM IS IN Refresh() METHOD, FIX ITTTTTT HOOOLY
                    driver.Manage().Timeouts().PageLoad.Add(TimeSpan.FromMinutes(1));

                    try // try if gotourl works
                    {
                        driver.Navigate().GoToUrl(URL); // setting URL property invokes navigating to the URL (loads the document, replacing the previous document. (even if it's the same URL))
                        new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitForListActive(driver)); // waits for Navigate().GoToUrl(URL) to load         

                        Console.WriteLine(Symbol + ": " + URL);
                        while (!Terminate) // an exception occurs when timeout expires
                        {
                            while (!Terminate) // Waits for signal to appear, refresh every 15 minutes of waiting
                            {
                                try
                                {
                                    new WebDriverWait(driver, TimeSpan.FromMinutes(10)).Until(condition => WaitResultVisibility(driver)); // waits for lightning icon
                                    Thread.Sleep(1000);
                                    break;
                                }
                                catch { Refresh(driver); }
                            }
                            Refresh(driver);

                            if (Terminate)
                                break;

                            try // getting data
                            {
                                GetData(driver);
                            }
                            catch { continue; }

                            if (Terminate)
                                break;

                            try // Waits for signal to disappear
                            {
                                new WebDriverWait(driver, TimeSpan.FromMinutes(16)).Until(condition => WaitProfitLossVisibility(driver)); // waits for PROFIT/LOSS to appear
                                Thread.Sleep(5000);
                                Console.WriteLine("Loop3");
                                Refresh(driver);
                            }
                            catch { }

                            if (Terminate)
                                break;

                            while (GetExpiryTime(driver) == CurrentExpiryTime | IsProfitLossVisible(driver)) // because sometimes even after the PROFIT/LOSS appears, when refreshing the signals didn't update OR the PROFIT/LOSS is still there.
                            {
                                if (Terminate)
                                    break;
                                try
                                {
                                    Console.WriteLine("Loop4"); // i think this is the one causing rapid reload
                                    Refresh(driver);
                                    new WebDriverWait(driver, TimeSpan.FromSeconds(16)).Until(condition => WaitH1Visiblity(driver));
                                }
                                catch { }
                                Thread.Sleep(5000);
                            }

                            if (Terminate)
                                break;

                            Refresh(driver);
                            Thread.Sleep(2000);
                        }
                    }
                    catch { Repeat = true; }
                    driver.Quit(); // driver.Close(); will close the current browser, and driver.Quit(); will terminate pretty much everything associated with it
                    Stopped = true;
                    Terminate = false;
                    if (Repeat & !StopRequested)
                        continue;
                    break;
                }
            });
        }
        public void Stop()
        {
            Terminate = true; // stops the asynchronous task in Start();
            StopRequested = true; // prevents the repeat
        }
        public void StartSync(IWebDriver driver)
        {
            if (!SyncVisible) // check if signal is visible
            {
                try
                {
                    var e = driver.FindElement(By.XPath(string.Format("//*[@id='tickers_nav']/li[{0}]/a/span[contains(@style,'inline')]", ChildNo)));
                    if (!SyncVisible) // check if signal is visible
                    {
                        driver.Navigate().GoToUrl(URL); // setting URL property invokes navigating to the URL (loads the document, replacing the previous document. (even if it's the same URL))
                        new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitForListActive(driver)); // waits for Navigate().GoToUrl(URL) to load 
                        GetData(driver);
                        SyncVisible = true;
                        Console.WriteLine(Symbol + " visible.");
                    }
                }
                catch
                {
                    if (SyncVisible)
                        SyncVisible = false;
                }
            }
        }
        #region HELPER METHODS
        private void GetData(IWebDriver driver)
        {
            if (Terminate)
                return;
            Regex expiryTime_Pattern = new Regex("\\d{1,2}:\\d\\d");
            IWebElement h1 = driver.FindElement(By.TagName("h1"));
            IWebElement texts = driver.FindElement(By.XPath("//*[@id='chart']/*[local-name()='svg']/*[local-name()='g']/*[local-name()='g'][7]/*[local-name()='text'][contains(text(), 'CALL') or contains(text(), 'PUT')]"));
            // profit/loss xPath //*[@id="chart"]/*/*/*[local-name()='g'][8]/*[local-name()='text'][contains(text(), 'PROFIT') or contains(text(), 'LOSS')]
            string[] price_directionArr = texts.Text.Split();

            // binary-signal data
            string symbol = Symbol;
            string price = price_directionArr[0];
            string direction = price_directionArr[1];
            string expirationTime = expiryTime_Pattern.Match(h1.Text).Value;

            string Price_Direction = texts.Text;
            if (direction != "WAIT" & CurrentPrice_Direction != Price_Direction)
            {
                string data = symbol + ";" + direction + ";" + price + ";" + expirationTime + ";";
                CurrentPrice_Direction = Price_Direction;
                CurrentExpiryTime = expirationTime;
                WriteToFile(data);
                Invoke((MethodInvoker)delegate
                {
                    TabControl ParentTabControl = (TabControl)(Parent);
                    ParentTabControl.SelectedTab = this;
                    currency_txtBox.AppendText(DateTime.Now.ToString() + " | " + data + Environment.NewLine);
                });
            }
        }
        private bool IsProfitLossVisible(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var e = driver.FindElement(By.XPath("//*[@id='chart']/*/*/*[local-name()='g'][8]/*[local-name()='text'][contains(text(), 'PROFIT') or contains(text(), 'LOSS')]"));
                return e.Displayed;
            }
            catch { return false; }
        }
        private string GetExpiryTime(IWebDriver driver)
        {
            while (!Terminate)
            {
                try
                {
                    new WebDriverWait(driver, TimeSpan.FromSeconds(15)).Until(condition => WaitH1Visiblity(driver));
                    Regex expiryTime_Pattern = new Regex("\\d{1,2}:(\\d\\d)");
                    IWebElement h1 = driver.FindElement(By.TagName("h1"));
                    string expiryTime = expiryTime_Pattern.Match(h1.Text).Value;
                    Console.WriteLine(Symbol + ": " + expiryTime + " = " + CurrentExpiryTime + "?");
                    return expiryTime;
                }
                catch (WebDriverTimeoutException)
                {
                    Console.WriteLine("Loop5");
                    //driver.Navigate().Refresh();
                    //driver.Url = URL;
                    Refresh(driver);
                } catch { }
            }
            return CurrentExpiryTime;
        }
        private void LogIn(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://binary-signal.com/en/login/index");
            new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitForLoginElementsVisibility(driver));
            string[] accInfo = File.ReadAllLines(@"account.txt");
            IWebElement usernameInputBox = driver.FindElement(By.CssSelector("input[name='user_name']"));
            IWebElement passwordInputBox = driver.FindElement(By.CssSelector("input[name='user_password']"));
            IWebElement rememberMe = driver.FindElement(By.CssSelector("input[name='set_remember_me_cookie']"));
            IWebElement loginButton = driver.FindElement(By.CssSelector("body > div.container > form > button"));
            usernameInputBox.SendKeys(accInfo[0]);
            Thread.Sleep(300);
            passwordInputBox.SendKeys(accInfo[1]);
            Thread.Sleep(300);
            rememberMe.Click();
            Thread.Sleep(300);
            loginButton.Click();
            new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitForLogin(driver));
        }
        private bool WaitProfitLossVisibility(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var e = driver.FindElement(By.XPath("//*[@id='chart']/*/*/*[local-name()='g'][8]/*[local-name()='text'][contains(text(), 'PROFIT') or contains(text(), 'LOSS')]"));
                return e.Displayed;
            }
            catch { return false; }
        }
        private bool WaitResultVisibility(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var e = driver.FindElement(By.XPath(string.Format("//*[@id='tickers_nav']/li[{0}]/a/span[contains(@style,'inline')]", ChildNo)));
                return e.Displayed;
            }
            catch { return false; }
        }
        private bool WaitResultInvisibility(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var e = driver.FindElement(By.XPath(string.Format("//*[@id='tickers_nav']/li[{0}]/a/span[contains(@style,'none')]", ChildNo)));
                return e.Displayed;
            }
            catch { return false; }
            
        }
        private bool WaitDirectionVisiblity(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var e = driver.FindElement(By.XPath("//*[@id='chart']/*[local-name()='svg']/*[local-name()='g']/*[local-name()='g'][7]/*[local-name()='text'][contains(text(), 'CALL') or contains(text(), 'PUT')]"));
                return e.Displayed;
            }
            catch { return false; }
        }
        private bool WaitH1Visiblity(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var e = driver.FindElement(By.TagName("h1"));
                return e.Displayed;
            }
            catch { return false; }
        }
        private bool WaitForLoginElementsVisibility(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var usernameInputBox = driver.FindElement(By.CssSelector("input[name='user_name']"));
                var passwordInputBox = driver.FindElement(By.CssSelector("input[name='user_password']"));
                var loginButton = driver.FindElement(By.CssSelector("body > div.container > form > button"));
                return usernameInputBox.Displayed & passwordInputBox.Displayed & (loginButton.Displayed & loginButton.Enabled);
            }
            catch { return false; }
        }
        private void IsLoginRequired(IWebDriver driver)
        {
            try
            {
                IWebElement logIn = driver.FindElement(By.XPath("//a[contains(@class, 'btn btn-primary btn-lg') and contains(text(), 'Login')]"));
                logIn.Click();
                LogIn(driver);
                driver.Navigate().GoToUrl(URL); // setting URL property invokes navigating to the URL (loads the document, replacing the previous document. (even if it's the same URL))
            }
            catch { return; }
        }
        private bool WaitForLogin(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var logIn = driver.FindElement(By.XPath("//*[@id='navbar']/ul/li[4]/a[contains(text(), 'account')]"));
                return logIn.Displayed;
            }
            catch { return false; }
        }
        private bool WaitForListActive(IWebDriver driver)
        {
            try
            {
                if (Terminate)
                    return true;
                var listActivity = driver.FindElement(By.XPath(string.Format("//*[@id='tickers_nav']/li[{0}][@class='active']", ChildNo)));
                return listActivity.Displayed;
            }
            catch { return false; }
        }
        private void WriteToFile(string appendText)
        {
            string path = FilePath + "\\" + FileName;
            try
            {
                if (!File.Exists(path))
                {
                    var file = File.Create(path);
                    file.Close();
                }

                bool doneWriting = false;
                while (!doneWriting)
                {
                    try
                    {
                        File.AppendAllText(path, appendText + Environment.NewLine);
                        doneWriting = true;
                    }
                    catch { }
                    Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("File path doesn't exist.\n " + e.Message);
            }
        }
        public DateTime GetRealTimeInZone()
        {
            var clock = NetworkClock.Instance;
            var now = clock.GetCurrentInstant();
            var tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            return now.InZone(tz).ToDateTimeUnspecified();
        }
        public int GetTimeRemaining()
        {
            int localMinute = GetRealTimeInZone().Minute % 15;
            Regex expiryTime_Pattern = new Regex("\\d{1,2}:(\\d\\d)");
            int expiryMinute = Convert.ToInt32(expiryTime_Pattern.Match(CurrentExpiryTime).Groups[1].Value) % 15;
            int timeRemaining = Math.Abs((15 - localMinute) - expiryMinute);
            Console.WriteLine("----- " + Symbol + " ----- | " + DateTime.Now + "\nWebsite minute: " + expiryMinute + "\n" + "Local minute: " + localMinute + "\n" + "Remaining minute: " + timeRemaining);
            return timeRemaining;
        }
        public void Refresh(IWebDriver driver)
        {
            while (!Terminate)
            {
                try
                {
                    driver.Navigate().Refresh();
                    break;
                }
                catch (WebDriverException e){ Console.WriteLine(Symbol + " | " + DateTime.Now + "\n" + e.Message); Terminate = true; Repeat = true; }
                Thread.Sleep(5000);
            }
        }
        #endregion

        #endregion

        #region DESIGNER 
        private void InitializeComponent()
        {
            //
            // Currency_TabPage
            //
            this.SuspendLayout();
            this.Controls.Add(this.currency_txtBox);
            this.Location = new System.Drawing.Point(4, 28);
            //this.Name = "eur_usd_tabPage";
            this.Padding = new Padding(3);
            this.Size = new System.Drawing.Size(768, 339);
            //this.TabIndex = Index;
            this.Text = Symbol;
            this.UseVisualStyleBackColor = true;
            //
            // currency_txtBox
            //
            this.currency_txtBox.Location = new System.Drawing.Point(0, 0);
            this.currency_txtBox.Multiline = true;
            //this.currency_txtBox.Name = "usd_jpy_txtBox";
            this.currency_txtBox.ScrollBars = ScrollBars.Vertical;
            this.currency_txtBox.Size = new System.Drawing.Size(772, 343);
            //this.currency_txtBox.TabIndex = 2;

            this.PerformLayout();
            this.ResumeLayout(false);
        }
        private TextBox currency_txtBox = new TextBox();
        #endregion
    }
}

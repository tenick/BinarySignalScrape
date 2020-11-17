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
                Stopped = false;
                IWebDriver driver;
                ChromeOptions options = new ChromeOptions();
                // ChromeDriver is just AWFUL because every version or two it breaks unless you pass cryptic arguments
                options.PageLoadStrategy = PageLoadStrategy.None; // https://www.skptricks.com/2018/08/timed-out-receiving-message-from-renderer-selenium.html //AGRESSIVE
                options.AddArguments("start-maximized"); // https://stackoverflow.com/a/26283818/1689770
                options.AddArguments("enable-automation"); // https://stackoverflow.com/a/43840128/1689770
                //options.AddArguments("--headless"); // only if you are ACTUALLY running headless
                options.AddArgument("--ignore-certificate-errors");
                options.AddArgument("--ignore-ssl-errors");
                options.AddArguments("--no-sandbox"); //https://stackoverflow.com/a/50725918/1689770
                options.AddArguments("--disable-infobars"); //https://stackoverflow.com/a/43840128/1689770
                options.AddArguments("--disable-dev-shm-usage"); //https://stackoverflow.com/a/50725918/1689770
                options.AddArguments("--disable-browser-side-navigation"); //https://stackoverflow.com/a/49123152/1689770
                options.AddArguments("--disable-gpu"); //https://stackoverflow.com/questions/51959986/how-to-solve-selenium-chromedriver-timed-out-receiving-message-from-renderer-exc
                options.AddArguments("--log-level=3");
                options.AddArguments("--silent");
                ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                //chromeDriverService.HideCommandPromptWindow = true;
                chromeDriverService.SuppressInitialDiagnosticInformation = true;
                
                driver = new ChromeDriver(chromeDriverService, options);   // initializes driver
                LogIn(driver);

                driver.Navigate().GoToUrl(URL); // setting URL property invokes navigating to the URL (loads the document, replacing the previous document. (even if it's the same URL))
                new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitForListActive(driver)); // waits for Navigate().GoToUrl(URL) to load         
            
                Console.WriteLine(Symbol + ": " + URL);
                while (!Terminate)
                {
                    if (Terminate)
                        break;
                    new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitResultVisibility(driver)); // Waits for signal to appear

                    try {
                        driver.Navigate().Refresh();
                        new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitH1Visiblity(driver)); // Waits for H1 tag to appear
                        new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitDirectionVisiblity(driver)); } // Waits for direction (PUT/CALL) to appear
                    catch { continue; }
                    GetData(driver);
                    if (Terminate)
                        break;
                    // new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitResultInvisibility(driver)); // Waits for signal to disappear
                    new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitProfitLossVisibility(driver));
                    driver.Navigate().Refresh();
                    new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitH1Visiblity(driver));
                    while ((GetExpiryTime(driver) == CurrentExpiryTime | IsProfitLossVisible(driver)) & !Terminate) // because sometimes even after the PROFIT/LOSS appears, when refreshing the signals didn't update.
                    {
                        driver.Navigate().Refresh();
                        new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitH1Visiblity(driver));
                        Thread.Sleep(2000);
                    }
                }
                driver.Quit(); // driver.Close(); will close the current browser, and driver.Quit(); will terminate pretty much everything associated with it
                Stopped = true;
                Terminate = false;
            });
        }
        public void Stop()
        {
            Terminate = true; // stops the asynchronous task in Start();
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
            Regex expiryTime_Pattern = new Regex("\\d{1,2}:(\\d\\d)");
            IWebElement h1 = driver.FindElement(By.TagName("h1"));
            string expiryTime = expiryTime_Pattern.Match(h1.Text).Value;
            Console.WriteLine(Symbol + ": " + expiryTime + " = " + CurrentExpiryTime + "?");
            return expiryTime;
        }
        private void LogIn(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://binary-signal.com/en/login/index");
            new WebDriverWait(driver, TimeSpan.FromDays(1)).Until(condition => WaitForLoginElementsVisibility(driver));
            string[] accInfo = File.ReadAllLines(@"account.txt");
            IWebElement usernameInputBox = driver.FindElement(By.CssSelector("input[name='user_name']"));
            IWebElement passwordInputBox = driver.FindElement(By.CssSelector("input[name='user_password']"));
            IWebElement loginButton = driver.FindElement(By.CssSelector("body > div.container > form > button"));
            usernameInputBox.SendKeys(accInfo[0]);
            Thread.Sleep(300);
            passwordInputBox.SendKeys(accInfo[1]);
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

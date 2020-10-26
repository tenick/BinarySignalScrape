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
        private string FileName;
        public string FilePath { private get; set; }
        public bool Stopped { get; private set; }

        
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
                ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                
                driver = new ChromeDriver(chromeDriverService, options, TimeSpan.FromHours(5));   // initializes driver
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromHours(5); // will wait for elements to appear

                LogIn(driver);
                driver.Navigate().GoToUrl(URL); // setting URL property invokes navigating to the URL (loads the document, replacing the previous document. (even if it's the same URL))

                while (!Terminate)
                {
                    WaitResultVisibility(driver); // Waits for signal to appear
                    driver.Navigate().Refresh();

                    if (Terminate)
                        break;
                    GetData(driver);

                    WaitResultInvisibility(driver); // Waits for signal to disappear
                    driver.Navigate().Refresh(); 
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

        #region HELPER METHODS
        private void GetData(IWebDriver driver)
        {
            Regex expiryTime_Pattern = new Regex("\\d{1,2}:\\d\\d");
            IWebElement h1 = driver.FindElement(By.TagName("h1"));
            IWebElement texts = driver.FindElement(By.CssSelector("#chart > svg > g > g:nth-child(14) > text.y2"));
            string[] price_directionArr = texts.Text.Split();

            // binary-signal data
            string symbol = Symbol;
            string price = price_directionArr[0];
            string direction = price_directionArr[1];
            string expirationTime = expiryTime_Pattern.Match(h1.Text).Value;

            if (direction != "WAIT")
            {
                string data = symbol + ";" + direction + ";" + price + ";" + expirationTime + ";";
                WriteToFile(data);
                Invoke((MethodInvoker)delegate
                {
                    
                    currency_txtBox.AppendText(DateTime.Now.ToString() + " | " + data + Environment.NewLine); // lets check
                });
            }
        }
        private void LogIn(IWebDriver driver)
        {
            string[] accInfo = File.ReadAllLines(@"account.txt");
            driver.Navigate().GoToUrl("https://binary-signal.com/en/login/index");
            IWebElement usernameInputBox = driver.FindElement(By.CssSelector("input[name='user_name']"));
            IWebElement passwordInputBox = driver.FindElement(By.CssSelector("input[name='user_password']"));
            IWebElement loginButton = driver.FindElement(By.CssSelector("body > div.container > form > button"));
            usernameInputBox.SendKeys(accInfo[0]);
            Thread.Sleep(300);
            passwordInputBox.SendKeys(accInfo[1]);
            Thread.Sleep(300);
            loginButton.Click();
        }
        private void WaitResultVisibility(IWebDriver driver)
        {
            while (true & !Terminate)
            {
                IWebElement result = driver.FindElement(By.CssSelector(string.Format("#tickers_nav > li:nth-child({0}) > a > span", ChildNo)));
                string styleAttrib = result.GetAttribute("style");
                if (!styleAttrib.Contains("none"))
                    break;
                Thread.Sleep(50);
            }
        }
        private void WaitResultInvisibility(IWebDriver driver)
        {
            while (true & !Terminate)
            {
                IWebElement result = driver.FindElement(By.CssSelector(string.Format("#tickers_nav > li:nth-child({0}) > a > span", ChildNo)));
                string styleAttrib = result.GetAttribute("style");
                if (styleAttrib.Contains("none"))
                    break;
                Thread.Sleep(50);
            }
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

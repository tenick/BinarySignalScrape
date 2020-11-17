using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.IO;

namespace BinarySignalScrape
{
    public partial class MainForm : Form
    {
        #region FIELDS

        private string URL = "https://binary-signal.com/";

        #endregion

        #region CONSTRUCTOR
        public MainForm()
        {
            InitializeComponent();
        }
        #endregion

        #region METHODS

        #region FORM CONTROLS EVENT METHODS

        /// <summary>
        /// Starts all the Currency_TabPage in the TabControl. Disables the necessary controls and sets the file path for each tabpage.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void start_btn_Click(object sender, EventArgs e)
        {
            if (!StartSuccessful())
                return;

            foreach (Currency_TabPage currency_TabPage in signals_data_tabCtrl.Controls)
            {
                currency_TabPage.Start();
            }
        }
        /// <summary>
        /// Stops all the Currency_TabPage in the TabControl.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stop_btn_Click(object sender, EventArgs e)
        {
            SetControlsStateOnStart(false);

            foreach (Currency_TabPage currency_TabPage in signals_data_tabCtrl.Controls)
            {
                currency_TabPage.Stop();
            }
        }
        /// <summary>
        /// Opens a FolderBrowserDialog box to select a path to save binary-signal data for each currency pair.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void filePath_btn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select path to save binary-signal data for each currency pair.";

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                filePath_txtBox.Text = fbd.SelectedPath;
            }
            fbd.Dispose();
        }

        /// <summary>
        /// Starts with only 1 thread looping through currency pairs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void startSync_Click(object sender, EventArgs e)
        {
            if (!StartSuccessful())
                return;
            await Task.Run(() =>
            {
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
                while (true)
                {
                    foreach (Currency_TabPage currency_TabPage in signals_data_tabCtrl.Controls)
                    {
                        currency_TabPage.StartSync(driver);
                    }
                }
            });
        }
        #endregion

        #region FORM EVENT METHODS

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeTabCotrol();

            // loading file path
            if (File.Exists(@"lastSettings.txt"))
            {
                string[] lines = File.ReadAllLines(@"lastSettings.txt");
                filePath_txtBox.Text = lines[0];
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // performs a 'click' on Stop button
            if (stop_btn.Enabled)
            {
                stop_btn_Click(new object(), new EventArgs());
            }

            // waits for all task threads to exit and dispose all chromedrivers
            bool exit = false;
            while (!exit)
            {
                foreach (Currency_TabPage currency_TabPage in signals_data_tabCtrl.Controls)
                {
                    if (currency_TabPage.Stopped)
                        exit = true;
                    else
                    {
                        exit = false;
                        break;
                    }
                }
                Thread.Sleep(100);
            }

            // saving file path
            File.WriteAllText(@"lastSettings.txt", String.Empty);
            string[] lines = { filePath_txtBox.Text };
            File.WriteAllLines(@"lastSettings.txt", lines);
        }

        #endregion

        #region HELPER METHODS
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
        private bool WaitForLoginElementsVisibility(IWebDriver driver)
        {
            try
            {
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
                var logIn = driver.FindElement(By.XPath("//*[@id='navbar']/ul/li[4]/a[contains(text(), 'account')]"));
                return logIn.Displayed;
            }
            catch { return false; }
        }
        /// <summary>
        /// Sets the FilePath field of all modified tab pages in TabControl
        /// </summary>
        private void SetTabPagesFilePath()
        {
            foreach (Currency_TabPage currency_TabPage in signals_data_tabCtrl.Controls)
            {
                currency_TabPage.FilePath = filePath_txtBox.Text;
            }
        }
        /// <summary>
        /// Checks FilePath textbox for input validity and checks accounts.txt for existence and validity.
        /// </summary>
        /// <returns></returns>
        private bool IsSafeToStart()
        {
            bool safe = true;
            if (!Directory.Exists(filePath_txtBox.Text) | filePath_txtBox.Text == "") // check if a valid file path is provided
                return false;
            if (File.Exists(@"account.txt")) // check if accounts.txt exist
            {
                string[] accInfo = File.ReadAllLines(@"account.txt");
                if (!(accInfo[0].Length > 0) | !(accInfo[1].Length > 0)) // check if valid credentials
                    return false;
            }
            else
                return false;
            return safe;
        }
        /// <summary>
        /// Sets the Enable property of necessary controls when Start button is pressed based on the passed boolean argument.
        /// </summary>
        /// <param name="isStart"></param>
        private void SetControlsStateOnStart(bool isStart)
        {
            start_btn.Enabled = !(false ^ isStart);
            stop_btn.Enabled = !(true ^ isStart);
            filePath_btn.Enabled = !(false ^ isStart);
            filePath_txtBox.Enabled = !(false ^ isStart);
        }
        /// <summary>
        /// Gets all of currency pair currently available on https://binary-signal.com/ and makes separate modified tab pages (Currency_TabPage) for each currency pair.
        /// </summary>
        private void InitializeTabCotrol()
        {
            HtmlWeb web = new HtmlWeb(); // for getting HTML document
            HtmlAgilityPack.HtmlDocument doc = web.Load(URL); // getting HTML document

            int index = 0;
            var As = doc.DocumentNode.SelectNodes(@"//ul[@id='tickers_nav']/li/a");
            foreach (var a in As)
            {
                string url = a.Attributes["href"].Value;

                Currency_TabPage currency_TabPage = new Currency_TabPage(url, index + 1);
                signals_data_tabCtrl.Controls.Add(currency_TabPage);
                index += 1;
            }
        }
        /// <summary>
        /// Does all necessary things before starting.
        /// </summary>
        /// <returns></returns>
        private bool StartSuccessful()
        {
            if (!IsSafeToStart())
            {
                MessageBox.Show("Enter a valid file path.\nFile path can't be blank.\naccount.txt is missing or empty");
                return false;
            }
            SetControlsStateOnStart(true);
            SetTabPagesFilePath();
            return true;
        }
        #endregion

        #endregion
    }
}

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
            if (!IsSafeToStart())
            {
                MessageBox.Show("Enter a valid file path.\nFile path can't be blank.\naccount.txt is missing or empty");
                return;
            }
            SetControlsStateOnStart(true);
            SetTabPagesFilePath();

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

        #endregion

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpreadsheetGUI
{
    public partial class AboutMenuPopUp : Form
    {
        public AboutMenuPopUp()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This just closes the popup when Exit button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitPopUpButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

using SpreadsheetUtilities;
using SS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpreadsheetGUI
{
    public partial class SpreadsheetForm : Form
    {
        private Spreadsheet spreadsheet;
        private string FileName;

        public SpreadsheetForm() : this("")
        {

        }

        public SpreadsheetForm(string filename)
        {
            // Initialize.
            InitializeComponent();
            FileName = "";
            if (filename == "")
            {
                spreadsheet = new Spreadsheet(validCellName, s => s.ToUpper(), "ps6");
            }
            else
            {
                FileName = filename;
                spreadsheet = new Spreadsheet(FileName, validCellName, s => s.ToUpper(), "ps6");
                foreach (string s in spreadsheet.GetNamesOfAllNonemptyCells())
                {
                    Coord cell = cellNameToCord(s);
                    object cellValue = spreadsheet.GetCellValue(s).ToString();
                    spreadsheetPanel.SetValue(cell.col, cell.row, cellValue.ToString());
                }
                UpdateFileName();
            }
            spreadsheetPanel.SelectionChanged += displaySelection;
            this.FormClosing += (CheckSaved);
        }

        /// <summary>
        /// Shortens and displays the filename displayed at the top of the form
        /// </summary>
        private void UpdateFileName()
        {
            // Loops through the string and cuts off anything before a \ to shorten the path
            string shortenedFileName = FileName;
            for (int i = 0; i < FileName.Length; i++)
            {
                if (FileName[i] == '\\')
                {
                    shortenedFileName = FileName.Substring(i + 1) + " - Spreadsheet";
                }
            }
            // Displays name at top of form
            this.Text = shortenedFileName;
        }

        /// <summary>
        /// Checks if user wants to save unsaved data before closing the spreadsheet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckSaved(object sender, FormClosingEventArgs e)
        {
            if (spreadsheet.Changed)
            {
                string message = "You have unsaved changes, do you want to save them?";
                string caption = "Unsaved Data";
                MessageBoxButtons button = MessageBoxButtons.YesNoCancel;
                // Displays the MessageBox
                var closing = MessageBox.Show(message, caption, button);
                if (closing == DialogResult.Yes)
                {
                    saveToolStripMenuItem_Click(sender, e);
                    // If cancel is hit while saving this function calls itself again
                    if (spreadsheet.Changed)
                    {
                        CheckSaved(sender, e);
                    }
                }
                else if (closing == DialogResult.Cancel)
                {
                    // This keeps the form from closing
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Every time the selection changes, this method is called with the
        //  SpreadsheetPanel as its parameter.
        /// </summary>
        /// <param name="SSPanel"></param>
        private void displaySelection (SpreadsheetPanel SSPanel)
        {
            // update cellName.
            Coord cellCoord;
            SSPanel.GetSelection(out cellCoord.col, out cellCoord.row);
            string nameOfCell = cellCordToName(cellCoord);
            cellName.Text = nameOfCell;
            // update cellValue.
            String value;
            SSPanel.GetValue(cellCoord.col, cellCoord.row, out value);
            cellValue.Text = value;
            // update cellContent.
            Object contents = spreadsheet.GetCellContents(nameOfCell);
            // Checks if the contents of the cell are a formula object and then prepends a "="
            if (contents is Formula)
            {
                cellContents.Text = "=" + contents.ToString();
            }
            else
            {
                cellContents.Text = contents.ToString();
            }
            cellContents.Focus();
            cellContents.SelectAll();
        }

        /// <summary>
        /// Make changes to selected cell and updates everything dependent on the cell
        /// </summary>
        private void updateCellContent(string name, string val)
        {
            ISet<string> effectedCells = spreadsheet.SetContentsOfCell(name, val);
            foreach (string effectedCell in effectedCells)
            {
                Coord cellCoord = cellNameToCord(effectedCell);
                Object cellValue = spreadsheet.GetCellValue(effectedCell);
                string newValue;
                // Catches FormulaErrors to display more clear message
                if (cellValue is FormulaError)
                {
                    newValue = ((FormulaError)cellValue).Reason;
                    spreadsheetPanel.SetError(cellCoord.col, cellCoord.row);
                }
                else
                {
                    newValue = cellValue.ToString();
                    spreadsheetPanel.RemoveError(cellCoord.col, cellCoord.row);
                }
                spreadsheetPanel.SetValue(cellCoord.col, cellCoord.row, newValue);
            }
            displaySelection(spreadsheetPanel);
        }

        /// <summary>
        /// Use as a validator delegate for back spreadsheet object.
        /// </summary>
        /// <param name="cellName">Name of the cell to be check.</param>
        /// <returns>True if there is such a cell, false otherwise.</returns>
        private bool validCellName(string cellName)
        {
            Coord cellCoord = cellNameToCord(cellName);
            string temp;
            return spreadsheetPanel.GetValue(cellCoord.col, cellCoord.row, out temp);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        private string cellCordToName (Coord coord)
        {
            return Convert.ToChar(coord.col + 65).ToString() + (coord.row + 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Coord cellNameToCord(string name)
        {
            // ASCII values
            int col = Convert.ToInt32(name[0]) - 65;
            int row = Int32.Parse(name.Substring(1))-1;
            return new Coord(col, row);
        }

        /// <summary>
        /// Abstracts the col and row properties
        /// </summary>
        private struct Coord
        {
            public int col;
            public int row;
            public Coord(int c, int r)
            {
                col = c;
                row = r;
            }
        }



        /// <summary>
        /// Deals with the New menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Tell the application context to run the form on the same
            // thread as the other forms.
            SpreadsheetApplicationContext.getAppContext().RunForm(new SpreadsheetForm());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            Close();
        }

        private void butEnter_Click(object sender, EventArgs e)
        {
            updateCellContent(cellName.Text, cellContents.Text);
        }

        private void cellContents_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                butEnter_Click(sender, e);
                e.Handled = true;
            }
        }

        /// <summary>
        /// This event will control the Help Menu -> About dropdown.  It opens a new
        /// form that describes Benwei and my information, and what this project is for.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutMenuPopUp AboutPopup = new AboutMenuPopUp();
            DialogResult dialogResult = AboutPopup.ShowDialog();
            AboutPopup.Dispose();
        }

        /// <summary>
        /// Event that is triggered when clicking the Save button.  If the FileName property is empty,
        /// it will just call the Save As event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FileName == "")
            {
                saveAsToolStripMenuItem_Click(sender, e);
            }
            else
            {
                try
                {
                    spreadsheet.Save(FileName);
                }
                catch (SpreadsheetReadWriteException)
                {
                    string message = "There was an error saving your file " + FileName + "\n Please choose a different file";
                    string caption = "Spreadsheet Read/Write Error";
                    MessageBoxButtons button = MessageBoxButtons.OK;
                    // Displays the MessageBox
                    MessageBox.Show(message, caption, button);
                }
            }
        }

        /// <summary>
        /// Creates a new file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "sprd files (*.sprd)|*.sprd|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileName = saveFileDialog1.FileName;
                try
                {
                    spreadsheet.Save(FileName);
                }
                catch (SpreadsheetReadWriteException)
                {
                    string message = "There was an error saving your file " + FileName + "\n Please choose a different file";
                    string caption = "Spreadsheet Read/Write Error";
                    MessageBoxButtons button = MessageBoxButtons.OK;
                    // Displays the MessageBox
                    MessageBox.Show(message, caption, button);
                }
                UpdateFileName();
            }
        }

        /// <summary>
        /// Opens a previously made spreadsheet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "txt files (*.sprd)|*.sprd|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SpreadsheetApplicationContext.getAppContext().RunForm(new SpreadsheetForm(openFileDialog1.FileName));

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void SpreadsheetForm_Shown(object sender, EventArgs e)
        {
            // default select A1 sell.
            spreadsheetPanel.SetSelection(0, 0);
        }

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contents AboutPopup = new Contents();
            DialogResult dialogResult = AboutPopup.ShowDialog();
            AboutPopup.Dispose();
        }
    }
}

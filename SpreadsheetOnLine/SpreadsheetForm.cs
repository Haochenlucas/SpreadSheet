/****************************************************************************
* Author:   Benwei Shi, Haocheng Zhang, Jawtyng Wei, Yucheng Yang
* Date:     04/23/2017
* Purpose:  Project 2 of CS 3505
* Usage:    Client of the online spreadsheet application
****************************************************************************/
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
using System.Net.Sockets;
using static SS.Network;
using System.Text.RegularExpressions;
using System.Xml;

namespace SpreadsheetGUI
{
    public partial class SpreadsheetForm : Form
    {
        // setting file name.
        private string XMLSettingFileName = "settings.xml";
        // Represents the connection with the server
        private Socket theServer = null;
        // The default port that will be used to connect with the server
        private int port = 2112;
        private string ServerAddr = "lab2-13.eng.utah.edu";

        // ClientID from server
        private int clientID;
        private bool isTyping = false;

        private Spreadsheet spreadsheet;
        private string spreadsheetName;

        public SpreadsheetForm() : this("")
        {

        }

        public SpreadsheetForm(string filename)
        {
            // Initialize.
            InitializeComponent();
            // Read spreadsheet name argument
            if (filename != "") textBox_SpreadsheetName.Text = filename;
            spreadsheetName = textBox_SpreadsheetName.Text;
            spreadsheet = new Spreadsheet(validCellName, s => s.ToUpper(), "ps6");
            spreadsheetPanel.SelectionChanged += displaySelection;
            this.FormClosing += (CloseNetwork);

            // Read setting file
            if (File.Exists(XMLSettingFileName))
            {
                Console.WriteLine("Reading settings from {0}...", XMLSettingFileName);
                Dictionary<String, String> readSettings = new Dictionary<string, string>();
                using (XmlReader reader = XmlReader.Create(XMLSettingFileName))
                {
                    bool startRead = false;
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            if (startRead)
                            {
                                // Read settings
                                string name = reader.Name;
                                if (reader.Read() && reader.Value.Length > 0)
                                {
                                    if (name == "Server")
                                    {
                                        ServerAddr = reader.Value;
                                    }
                                    else if (name == "Port")
                                    {
                                        Int32.TryParse(reader.Value, out port);
                                    }
                                }
                            }
                            else if (reader.Name == "ClientSettings")
                                startRead = true;
                        }
                    }
                }
            }
            else
            {
                Console.Error.WriteLine("Cannot find {0}.", XMLSettingFileName);
                // Write a default settings.xml
                XmlWriterSettings XMLsettings = new XmlWriterSettings();
                XMLsettings.Indent = true;
                using (XmlWriter writer = XmlWriter.Create(XMLSettingFileName, XMLsettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("ClientSettings");
                    writer.WriteElementString("Server", ServerAddr);
                    writer.WriteElementString("Port", port.ToString());
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }

            Console.WriteLine("Server: {0}.", ServerAddr);
            Console.WriteLine("Port: {0}.", port);
            // Connect
            textBox_SpreadsheetName_KeyPress(this, new KeyPressEventArgs('\r'));
        }

        /// <summary>
        /// Send the player name, used as a parameter in ConnectToServer method 
        /// </summary>
        /// <param name="ss"></param>
        private void FirstConnect(SocketState state)
        {
            // Check if a connection was successful
            if (!state.SocketConnected)
            {
                Invoke((MethodInvoker)delegate {
                    string message = "Server: " + ServerAddr + 
                        ".\n\nMake sure the server is running. " + 
                        "\npress \"Retry\" to try again." +
                        "\npress \"Cancel\" to exit.";
                    string caption = "Connection failed";
                    DialogResult result = MessageBox.Show(message, caption, MessageBoxButtons.RetryCancel);
                    if (result == DialogResult.Cancel)
                    {
                        this.Close();
                    }
                    else 
                    {
                        // Connect
                        textBox_SpreadsheetName_KeyPress(this, new KeyPressEventArgs('\r'));
                    }
                });
            }
            else
            {
                // Prepare to receive startup spreadsheet content.
                state.EventProcessor = ReceiveStartup;
                state.DisconnectedProcessor = Disconnect;
                // Send the spreadsheet name to server to request the spreadsheet content.
                Network.Send(state.socket, "Connect\t" + spreadsheetName + "\t");
                Console.WriteLine("Send: " + "Connect\t " + spreadsheetName + "\t");/////////////////////
            }
        }

        private void Disconnect(SocketState state)
        {
            state.sb.Clear();
            Network.Disconnect(state.socket);
            // Turn off all the elements of spreadsheet.
            Invoke((MethodInvoker)delegate {
                spreadsheetPanel.Enabled = false;
                cellContents.Enabled = false;
            });
        }

        /// <summary>
        /// Method to begin the game after all data that is needed has been recieved
        /// </summary>
        /// <param name="ss"></param>
        /// <returns></returns>
        private void ReceiveStartup(SocketState state)
        {
            string startupStarting = "Startup\t";
            string startupEnding = "\t\n";
            // Get the data out of our growable buffer
            string receivedData;
            lock (state.sb)
            {
                receivedData = state.sb.ToString();
            }
            Console.WriteLine("Received: " + receivedData);/////////////////////

            // Find the first "Startup\t"
            int startupStart = receivedData.IndexOf(startupStarting);
            if (startupStart == -1) return; // Cannot find "Startup\t"

            // Find the first "\t\n"
            int startupEnd = receivedData.IndexOf(startupEnding);
            if (startupEnd == -1) return; // Cannot find "\t\n"

            // Extract the startup message.
            string startupMessage = receivedData.Substring(startupStart, startupEnd - startupStart + 1);
            lock (state.sb)
            {
                state.sb.Remove(0, startupEnd + startupEnding.Length);
            }

            // Split that at each "\n" and put the pieces in an array
            string[] parts = startupMessage.Split('\t');
            // Read clientID from 2nd element of parts
            if (!Int32.TryParse(parts[1], out clientID))
            {
                MessageBox.Show("Failed to read client ID from server, you can try to connect again.");
                Disconnect(state);
                return;
            }

            // Load data
            spreadsheet = new Spreadsheet(validCellName, s => s.ToUpper(), "ps6");
            // Loop until we have processed all cell contents.
            for (int i = 2; i < parts.Length - 1; i += 2)
            {
                Invoke((MethodInvoker)delegate
                {
                    updateCellContent(parts[i], parts[i + 1], state);
                });
            }
            
            // Turn on all the element for spreadsheet.
            Invoke((MethodInvoker)delegate {
                spreadsheetPanel.Enabled = true;
                cellContents.Enabled = true;
            });
                        
            // Set the SocketState's delegate to the ProcessData method below
            state.EventProcessor = ProcessData;
            GetData(state);
        }


        /// <summary>
        /// This splits the data into managable strings in the correct format
        /// and passes it to the World class to deserialize
        /// </summary>
        /// <param name="ss"></param>
        private void ProcessData(SocketState state)
        {
            string receivedData;
            // Get the growable buffer full of string stuffs
            lock (state.sb)
            {
                receivedData = state.sb.ToString();
            }
            Console.WriteLine("Received: " + receivedData);/////////////////////

            int lastNewLine = receivedData.LastIndexOf('\n');
            lock (state.sb)
            {
                // Get the growable buffer full of string stuffs
                state.sb.Remove(0, lastNewLine+1);
            }

            // Split that at each "\n" and put the pieces in an array
            string[] messages = receivedData.Substring(0, lastNewLine+1).Split('\n');

            foreach (string p in messages)
            {
                if (p.Length < 7) continue;
                if (p.Substring(0, 7) == "Change\t")
                {
                    string[] elements = p.Split('\t');
                    Invoke((MethodInvoker)delegate
                    {
                        updateCellContent(elements[1], elements[2], state);
                    });
                }
                else if (p.Substring(0, 9) == "IsTyping\t")
                {
                    string[] elements = p.Split('\t');
                    if (Int32.Parse(elements[1]) == clientID) continue;
                    if (validCellName(elements[2]))
                    {
                        Coord coord = cellNameToCord(elements[2]);
                        spreadsheetPanel.SetEditing(coord.col, coord.row);
                    }
                }
                else if (p.Substring(0, 11) == "DoneTyping\t")
                {
                    string[] elements = p.Split('\t');
                    if (Int32.Parse(elements[1]) == clientID) continue;
                    if (validCellName(elements[2]))
                    {
                        Coord coord = cellNameToCord(elements[2]);
                        spreadsheetPanel.RemoveEditing(coord.col, coord.row);
                    }
                }
            }
            
            // Get some more data from the server
            GetData(state);
        }

        /// <summary>
        /// Checks if user wants to save unsaved data before closing the spreadsheet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseNetwork(object sender, FormClosingEventArgs e)
        {
            Invoke((MethodInvoker)delegate {
                spreadsheetPanel.Enabled = false;
                cellContents.Enabled = false;
            });
            if (theServer == null || !theServer.Connected) return;
            if (!(theServer.Available == 0) && !theServer.Poll(1000, SelectMode.SelectRead))
            {
                theServer.Shutdown(SocketShutdown.Send);
                theServer.Close();
            }
        }

        /// <summary>
        /// Every time the selection changes, this method is called with the
        //  SpreadsheetPanel as its parameter.
        /// </summary>
        /// <param name="SSPanel"></param>
        private void displaySelection (SpreadsheetPanel SSPanel)
        {
            if (isTyping)
            {
                isTyping = false;
                Network.Send(theServer, "DoneTyping\t" + clientID + "\t" + cellName.Text + "\t");
                Console.WriteLine("Sending: " + "DoneTyping\t" + clientID + "\t" + cellName.Text + "\t");
            }
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
        private void updateCellContent(string cellName, string cellContents, SocketState state)
        {
            if (!validCellName(cellName))
            {
                MessageBox.Show("Received unvalid cell name: \"" + cellName
                    + "\" from server, disconnect from server. \nyou can try to connect again.");
                Disconnect(state);
                return;
            }

            ISet<string> effectedCells = spreadsheet.SetContentsOfCell(cellName, cellContents);
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
            Coord cellCoord;
            try
            {
                cellCoord = cellNameToCord(cellName);
            } catch (Exception)
            {
                return false;
            }
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
        /// Menu item exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            Close();
        }

        /// <summary>
        /// Editing cell content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cellContents_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (e.KeyChar == '\r')
                {
                    if (isTyping)
                    {
                        isTyping = false;
                        Network.Send(theServer, "DoneTyping\t" + clientID + "\t" + cellName.Text + "\t");
                        Console.WriteLine("Sending: " + "DoneTyping\t" + clientID + "\t" + cellName.Text + "\t");
                        Network.Send(theServer, "Edit\t" + cellName.Text + "\t" + cellContents.Text + "\t");
                        Console.WriteLine("Sending: " + "Edit\t" + cellName.Text + "\t" + cellContents.Text + "\t");
                    }
                    e.Handled = true;
                }
                else if (Control.ModifierKeys != Keys.Control)
                {
                    if (!isTyping)
                    {
                        isTyping = true;
                        Network.Send(theServer, "IsTyping\t" + clientID + "\t" + cellName.Text + "\t");
                        Console.WriteLine("Sending: " + "IsTyping\t" + clientID + "\t" + cellName.Text + "\t");
                    }
                }
            } catch (Exception)
            {
                MessageBox.Show("Connection has lost, press Enter in the spreadsheet name box to connect again.");
                CloseNetwork(this, new FormClosingEventArgs(CloseReason.None, false));
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


        private void SpreadsheetForm_Shown(object sender, EventArgs e)
        {
            // default select A1 sell.
            spreadsheetPanel.SetSelection(0, 0);
        }
        
        /// <summary>
        /// For undo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpreadsheetForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Z && (e.Control))
            {
                undoToolStripMenuItem_Click(sender, e);
            }
        }

        /// <summary>
        /// Undo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Network.Send(theServer, "Undo\t");
                Console.WriteLine("Sending: " + "Undo\t");
            } catch (Exception)
            {
                MessageBox.Show("Connection has lost, press Enter in the spreadsheet name box to connect again.");
                CloseNetwork(this, new FormClosingEventArgs(CloseReason.None, false));
            }
        }
        
        /// <summary>
        /// "Enter" key pressed to connect and request spreadsheet content with spreadsheet name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_SpreadsheetName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (theServer != null)
                {
                    Invoke((MethodInvoker)delegate {
                        spreadsheetPanel.Clear();
                        cellContents.Text = "";
                        cellName.Text = "";
                        cellValue.Text = "";
                    });
                    if (!theServer.Poll(1000, SelectMode.SelectRead))
                    {
                        theServer.Shutdown(SocketShutdown.Send);
                        theServer.Close();
                    }
                }
                spreadsheetName = textBox_SpreadsheetName.Text;
                // Attempt to create a socket with and connect to the server
                theServer = Network.ConnectToServer(ServerAddr, port, FirstConnect);
                e.Handled = true;
            }
        }

    }
}

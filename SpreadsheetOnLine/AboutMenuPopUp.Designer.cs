namespace SpreadsheetGUI
{
    partial class AboutMenuPopUp
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutMenuPopUp));
            this.ExitPopUpButton = new System.Windows.Forms.Button();
            this.AboutTheSpreadsheet = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ExitPopUpButton
            // 
            this.ExitPopUpButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ExitPopUpButton.Location = new System.Drawing.Point(192, 109);
            this.ExitPopUpButton.Name = "ExitPopUpButton";
            this.ExitPopUpButton.Size = new System.Drawing.Size(75, 23);
            this.ExitPopUpButton.TabIndex = 0;
            this.ExitPopUpButton.Text = "Exit";
            this.ExitPopUpButton.UseVisualStyleBackColor = true;
            this.ExitPopUpButton.Click += new System.EventHandler(this.ExitPopUpButton_Click);
            // 
            // AboutTheSpreadsheet
            // 
            this.AboutTheSpreadsheet.Enabled = false;
            this.AboutTheSpreadsheet.Location = new System.Drawing.Point(12, 12);
            this.AboutTheSpreadsheet.Multiline = true;
            this.AboutTheSpreadsheet.Name = "AboutTheSpreadsheet";
            this.AboutTheSpreadsheet.Size = new System.Drawing.Size(448, 77);
            this.AboutTheSpreadsheet.TabIndex = 1;
            this.AboutTheSpreadsheet.Text = resources.GetString("AboutTheSpreadsheet.Text");
            // 
            // AboutMenuPopUp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(473, 144);
            this.Controls.Add(this.AboutTheSpreadsheet);
            this.Controls.Add(this.ExitPopUpButton);
            this.Name = "AboutMenuPopUp";
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ExitPopUpButton;
        private System.Windows.Forms.TextBox AboutTheSpreadsheet;
    }
}
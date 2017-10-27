namespace SpreadsheetGUI
{
    partial class Contents
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Contents));
            this.ContentsExitButton = new System.Windows.Forms.Button();
            this.ContentsTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ContentsExitButton
            // 
            this.ContentsExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ContentsExitButton.Location = new System.Drawing.Point(251, 292);
            this.ContentsExitButton.Name = "ContentsExitButton";
            this.ContentsExitButton.Size = new System.Drawing.Size(87, 29);
            this.ContentsExitButton.TabIndex = 0;
            this.ContentsExitButton.Text = "Exit";
            this.ContentsExitButton.UseVisualStyleBackColor = true;
            // 
            // ContentsTextBox
            // 
            this.ContentsTextBox.Location = new System.Drawing.Point(12, 12);
            this.ContentsTextBox.Multiline = true;
            this.ContentsTextBox.Name = "ContentsTextBox";
            this.ContentsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ContentsTextBox.Size = new System.Drawing.Size(557, 249);
            this.ContentsTextBox.TabIndex = 1;
            this.ContentsTextBox.Text = resources.GetString("ContentsTextBox.Text");
            this.ContentsTextBox.TextChanged += new System.EventHandler(this.ContentsTextBox_TextChanged);
            // 
            // Contents
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(581, 324);
            this.Controls.Add(this.ContentsTextBox);
            this.Controls.Add(this.ContentsExitButton);
            this.Name = "Contents";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ContentsExitButton;
        private System.Windows.Forms.TextBox ContentsTextBox;
    }
}
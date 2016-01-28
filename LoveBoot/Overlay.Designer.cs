namespace LoveBoot
{
    partial class Overlay
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Overlay));
            this.tmrOverlay = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // tmrOverlay
            // 
            this.tmrOverlay.Interval = 25;
            this.tmrOverlay.Tick += new System.EventHandler(this.tmrOverlay_Tick);
            // 
            // Overlay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Fuchsia;
            this.ClientSize = new System.Drawing.Size(133, 129);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Overlay";
            this.Text = "Overlay";
            this.TransparencyKey = System.Drawing.Color.Fuchsia;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Overlay_FormClosing);
            this.Load += new System.EventHandler(this.Overlay_Load);
            this.Shown += new System.EventHandler(this.Overlay_Shown);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Overlay_Paint);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer tmrOverlay;
    }
}
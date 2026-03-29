namespace AuxiliaryPanel
{
    partial class AuxiliaryPanel
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
            this.SnapshotButton = new System.Windows.Forms.Button();
            this.ProfilerDetachButton = new System.Windows.Forms.Button();
            this.ProcessDisplayLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // SnapshotButton
            // 
            this.SnapshotButton.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SnapshotButton.Location = new System.Drawing.Point(11, 55);
            this.SnapshotButton.Name = "SnapshotButton";
            this.SnapshotButton.Size = new System.Drawing.Size(248, 38);
            this.SnapshotButton.TabIndex = 0;
            this.SnapshotButton.Text = "TAKE SNAPSHOT";
            this.SnapshotButton.UseVisualStyleBackColor = true;
            // 
            // ProfilerDetachButton
            // 
            this.ProfilerDetachButton.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProfilerDetachButton.Location = new System.Drawing.Point(11, 103);
            this.ProfilerDetachButton.Name = "ProfilerDetachButton";
            this.ProfilerDetachButton.Size = new System.Drawing.Size(248, 37);
            this.ProfilerDetachButton.TabIndex = 1;
            this.ProfilerDetachButton.Text = "DETACH PROFILER";
            this.ProfilerDetachButton.UseVisualStyleBackColor = true;
            // 
            // ProcessDisplayLabel
            // 
            this.ProcessDisplayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ProcessDisplayLabel.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProcessDisplayLabel.Location = new System.Drawing.Point(11, 9);
            this.ProcessDisplayLabel.Name = "ProcessDisplayLabel";
            this.ProcessDisplayLabel.Size = new System.Drawing.Size(248, 35);
            this.ProcessDisplayLabel.TabIndex = 2;
            this.ProcessDisplayLabel.Text = "PID:";
            this.ProcessDisplayLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // AuxiliaryPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(265, 157);
            this.Controls.Add(this.ProcessDisplayLabel);
            this.Controls.Add(this.ProfilerDetachButton);
            this.Controls.Add(this.SnapshotButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AuxiliaryPanel";
            this.Load += new System.EventHandler(this.AuxiliaryPanel_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button SnapshotButton;
        private System.Windows.Forms.Button ProfilerDetachButton;
        private System.Windows.Forms.Label ProcessDisplayLabel;
    }
}


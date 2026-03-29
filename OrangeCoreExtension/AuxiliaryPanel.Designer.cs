namespace OrangeClient
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
            this.ProcessDisplayLabel = new System.Windows.Forms.Label();
            this.ProfilerDetachButton = new System.Windows.Forms.Button();
            this.SnapshotButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ProcessDisplayLabel
            // 
            this.ProcessDisplayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ProcessDisplayLabel.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProcessDisplayLabel.Location = new System.Drawing.Point(9, 12);
            this.ProcessDisplayLabel.Name = "ProcessDisplayLabel";
            this.ProcessDisplayLabel.Size = new System.Drawing.Size(230, 35);
            this.ProcessDisplayLabel.TabIndex = 5;
            this.ProcessDisplayLabel.Text = "PID:";
            // 
            // ProfilerDetachButton
            // 
            this.ProfilerDetachButton.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProfilerDetachButton.Location = new System.Drawing.Point(9, 106);
            this.ProfilerDetachButton.Name = "ProfilerDetachButton";
            this.ProfilerDetachButton.Size = new System.Drawing.Size(248, 37);
            this.ProfilerDetachButton.TabIndex = 4;
            this.ProfilerDetachButton.Text = "DETACH PROFILER";
            this.ProfilerDetachButton.UseVisualStyleBackColor = true;
            this.ProfilerDetachButton.Click += new System.EventHandler(this.ProfilerDetachButton_Click);
            // 
            // SnapshotButton
            // 
            this.SnapshotButton.BackColor = System.Drawing.Color.Transparent;
            this.SnapshotButton.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SnapshotButton.Location = new System.Drawing.Point(9, 58);
            this.SnapshotButton.Name = "SnapshotButton";
            this.SnapshotButton.Size = new System.Drawing.Size(248, 38);
            this.SnapshotButton.TabIndex = 3;
            this.SnapshotButton.Text = "TAKE SNAPSHOT";
            this.SnapshotButton.UseVisualStyleBackColor = false;
            this.SnapshotButton.Click += new System.EventHandler(this.SnapshotButton_Click);
            // 
            // AuxiliaryPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Linen;
            this.ClientSize = new System.Drawing.Size(266, 151);
            this.Controls.Add(this.ProcessDisplayLabel);
            this.Controls.Add(this.ProfilerDetachButton);
            this.Controls.Add(this.SnapshotButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AuxiliaryPanel";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label ProcessDisplayLabel;
        private System.Windows.Forms.Button ProfilerDetachButton;
        private System.Windows.Forms.Button SnapshotButton;
    }
}
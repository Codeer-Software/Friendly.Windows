
namespace LogTest
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this._textBoxPID = new System.Windows.Forms.TextBox();
            this._buttonAttach = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _textBoxPID
            // 
            this._textBoxPID.Location = new System.Drawing.Point(42, 10);
            this._textBoxPID.Name = "_textBoxPID";
            this._textBoxPID.Size = new System.Drawing.Size(100, 19);
            this._textBoxPID.TabIndex = 0;
            // 
            // _buttonAttach
            // 
            this._buttonAttach.Location = new System.Drawing.Point(148, 8);
            this._buttonAttach.Name = "_buttonAttach";
            this._buttonAttach.Size = new System.Drawing.Size(75, 23);
            this._buttonAttach.TabIndex = 1;
            this._buttonAttach.Text = "Attach";
            this._buttonAttach.UseVisualStyleBackColor = true;
            this._buttonAttach.Click += new System.EventHandler(this._buttonAttach_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(23, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "PID";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(254, 47);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._buttonAttach);
            this.Controls.Add(this._textBoxPID);
            this.Name = "MainForm";
            this.Text = "LogTest";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxPID;
        private System.Windows.Forms.Button _buttonAttach;
        private System.Windows.Forms.Label label1;
    }
}


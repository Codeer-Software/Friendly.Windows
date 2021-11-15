using Codeer.Friendly.Windows;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LogTest
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void _buttonAttach_Click(object sender, EventArgs e)
        {
            try
            {
                using (var app = new WindowsAppFriend(Process.GetProcessById(int.Parse(_textBoxPID.Text)))) { }
                MessageBox.Show("成功");
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MultiDomain_2_32
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        public void StartMultiDomain()
        {
            AppDomain.CreateDomain("XXX").DoCallBack(() => new Form().Show());
            AppDomain.CreateDomain("YYY").DoCallBack(() => new Form().Show());
        }
    }
}

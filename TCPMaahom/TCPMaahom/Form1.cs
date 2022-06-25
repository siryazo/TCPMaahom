using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TCPMaahom
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartProcess();
        }

        void StartProcess()
        {
            try
            {
                StartTask();
            }
            catch
            {
                StartProcess();
            }
        }

        void StartTask()
        {
            Task.Run(() =>
            {
                AsynchronousSocketListener.StartListening();
            });
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = "";
                foreach(var a in AsynchronousSocketListener._log)
                {
                    textBox1.Text += a + Environment.NewLine;
                }
            }
            catch
            {

            }
        }
    }
}

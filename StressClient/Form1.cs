using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StressClient
{
    public partial class Form1 : Form
    {
        NetworkModule networkModule;
        public Form1()
        {
            InitializeComponent();
            networkModule = new NetworkModule();
            networkModule.Run();
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            foreach(var c in networkModule.clients)
            {
                e.Graphics.DrawRectangle(new Pen(Brushes.Black), c.Value.position.X, c.Value.position.Z, 1f, 1f);
            }
        }
        private void Form1_KeyEvent(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.Left:
                    break;
            }
        }
        private void Form1_Timer1(object sender, EventArgs e)
        {
            Invalidate();
        }
    }
}

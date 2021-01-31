using System;
using System.Drawing;
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
            lock (networkModule.clients)
            {
                foreach(var c in networkModule.clients)
                {
                    e.Graphics.DrawRectangle(new Pen(Brushes.Black), c.Value.position.X, c.Value.position.Z, 1f, 1f);
                }
            }
            e.Graphics.DrawString("Connect: " + networkModule.clients.Count, Font,
                Brushes.Black, 700, 550);
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

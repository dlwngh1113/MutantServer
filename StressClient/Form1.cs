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
        Point pt;
        const int size = 10;
        public Form1()
        {
            InitializeComponent();
            pt.X = 10;
            pt.Y = 10;
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawEllipse(new Pen(Brushes.Black, 1), pt.X, pt.Y, size, size);
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
            pt.X += 1;
            pt.Y += 1;
            Invalidate();
        }
    }
}

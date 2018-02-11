using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace customize_qrcode
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //var m = Generator.GenerateQRCode("你好世界！", 7, 0);
            //Generator.SaveQRCode2(m, "D:/output.png", 10, 9, 70);
            pictureBox2.Parent = pictureBox1;
            pictureBox2.MouseWheel += PictureBox2_MouseWheel;
            comboBox1.SelectedIndex = 2;
            comboBox2.SelectedIndex = 0;
        }

        private int scale_factor = 10;
        private void PictureBox2_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
                scale_factor++;
            else if (scale_factor > 1)
                scale_factor--;
            else
                return;
            pictureBox2.Width = 20 * scale_factor;
            pictureBox2.Height = 20 * scale_factor;

        }


        int xPos;
        int yPos;
        bool MoveFlag;
        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            MoveFlag = true;//已经按下.
            xPos = e.X;//当前x坐标.
            yPos = e.Y;//当前y坐标.
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            MoveFlag = false;
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (MoveFlag)
            {
                pictureBox2.Left += Convert.ToInt16(e.X - xPos);//设置x坐标.
                pictureBox2.Top += Convert.ToInt16(e.Y - yPos);//设置y坐标.
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox2.Visible = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read);
                var buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
                fs.Close();
                var ms = new MemoryStream(buf);
                var img = Image.FromStream(ms);

                img = ImageProcess.ToGray(img);
                int step = 1;
                if (comboBox1.SelectedIndex > 0)
                {
                    var img1 = ImageProcess.HorizontalGrayDeltafy(img, step);
                    var img2 = ImageProcess.VerticalGrayDeltafy(img, step);
                    img1 = ImageProcess.CropFromLeftTop(img1, img1.Width, img1.Height - step);
                    img2 = ImageProcess.CropFromLeftTop(img2, img2.Width - step, img2.Height);
                    img = ImageProcess.GrayAdd(img1, img2);
                    img = ImageProcess.ReverseGray(img);
                }
                if (comboBox1.SelectedIndex > 1)
                    img = ImageProcess.ToBinary(img, 230);


                var m = Generator.GenerateQRCode(textBox1.Text, (int)numericUpDown1.Value, comboBox2.SelectedIndex);
                var img_g = Generator.CustomizeQRCode(m, 10);

                pictureBox1.Image = img;
                pictureBox2.Image = img_g;
            }
        }
    }
}

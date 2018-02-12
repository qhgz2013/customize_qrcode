using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
            comboBox1.SelectedIndex = 1;
            comboBox2.SelectedIndex = 0;
            checkBox1.Checked = true;
            checkBox2.Checked = true;
            form_init = true;
        }

        private bool form_init = false;
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
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox2.Visible = true;
                load_image();
                generate_qrcode();
            }
        }

        private void load_image()
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
                img1 = ImageProcess.CropImage(img1, 0, 0, img1.Width, img1.Height - step);
                img2 = ImageProcess.CropImage(img2, 0, 0, img2.Width - step, img2.Height);
                img = ImageProcess.GrayAdd(img1, img2);
                img = ImageProcess.ReverseGray(img);
            }
            if (checkBox1.Checked)
                img = ImageProcess.ToBinary(img, (byte)((comboBox1.SelectedIndex > 0) ? 230 : 127));


            pictureBox1.Image = img;
        }
        private void generate_qrcode()
        {
            var m = Generator.GenerateQRCode(textBox1.Text, (int)numericUpDown1.Value, comboBox2.SelectedIndex);
            var img_g = Generator.CustomizeQRCode(m, padding: 0, fill_padding: false, foreground: panel1.BackColor);

            pictureBox2.Image = img_g;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox2.Visible)
            {
                int crop_left = pictureBox2.Left;
                int crop_top = pictureBox2.Top;
                int crop_width = pictureBox2.Width;
                int crop_height = pictureBox2.Height;

                Debug.Print("Crop Rect: {{{0},{1},{2},{3}}}", crop_left, crop_top, crop_width, crop_height);
                //计算原图片的坐标信息

                //放缩比
                double scale = Math.Min(pictureBox1.Width / (double)pictureBox1.Image.Width, pictureBox1.Height / (double)pictureBox1.Image.Height);

                Debug.Print("scale ratio: {0}", scale);
                //原图片左上角在屏幕的位置
                int origin_left = (int)((pictureBox1.Width - pictureBox1.Image.Width * scale) / 2);
                int origin_top = (int)((pictureBox1.Height - pictureBox1.Image.Height * scale) / 2);
                int origin_width = (int)((pictureBox1.Image.Width * scale));
                int origin_height = (int)((pictureBox1.Image.Height * scale));

                Debug.Print("the original (background) image left top point at screen:({0},{1}), size:({2},{3})", origin_left, origin_top, origin_width, origin_height);

                //二维码图片映射到原图的坐标
                int mapped_left = (int)((crop_left - origin_left) / scale);
                int mapped_top = (int)((crop_top - origin_top) / scale);
                int mapped_width = (int)(crop_width / scale);
                int mapped_height = (int)(crop_height / scale);
                Debug.Print("Crop Rect Mapped to the original image: {{{0},{1},{2},{3}}}", mapped_left, mapped_top, mapped_width, mapped_height);
                //裁剪原图
                var cropped_image = ImageProcess.CropImage(pictureBox1.Image, mapped_left, mapped_top, mapped_width, mapped_height);
                //缩放原图，匹配分辨率到二维码
                Image resized_image = new Bitmap(cropped_image, pictureBox2.Image.Width, pictureBox2.Image.Height);

                //平均灰度
                if (checkBox2.Checked)
                    resized_image = ImageProcess.AverageGray(resized_image, 3, 3);
                //二值化
                if (checkBox1.Checked)
                    resized_image = ImageProcess.ToBinary(resized_image);
                //绘图
                var output_image = new Bitmap(resized_image.Width + 20, resized_image.Height + 20);
                var gr = Graphics.FromImage(output_image);
                gr.FillRectangle(Brushes.White, 0, 0, output_image.Width, output_image.Height);
                gr.DrawImage(resized_image, 10, 10);
                gr.DrawImage(pictureBox2.Image, 10, 10);
                gr.Dispose();
                //保存
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    output_image.Save(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private void panel1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                var color = colorDialog1.Color;
                panel1.BackColor = color;
                if (pictureBox2.Visible)
                    generate_qrcode();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (form_init && !string.IsNullOrEmpty(openFileDialog1.FileName))
                load_image();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (form_init && !string.IsNullOrEmpty(openFileDialog1.FileName))
                load_image();
        }
    }
}

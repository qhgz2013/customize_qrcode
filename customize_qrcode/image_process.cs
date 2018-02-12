using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace customize_qrcode
{
    public class ImageProcess
    {
        public static Image ToGray(Image origin)
        {
            var bmp = new Bitmap(origin);
            var ret = new Bitmap(bmp.Width, bmp.Height);

            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, buf, 0, buf.Length);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = y * lck.Stride + x * 3;
                    int sum = buf[offset] + buf[offset + 1] + buf[offset + 2];
                    byte average = (byte)(sum / 3);
                    buf[offset] = average; buf[offset + 1] = average; buf[offset + 2] = average;
                }
            }
            bmp.UnlockBits(lck);

            lck = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, lck.Scan0, buf.Length);
            ret.UnlockBits(lck);
            return ret;
        }

        public static Image HorizontalGrayDeltafy(Image origin, int step = 1)
        {
            var bmp = new Bitmap(origin);
            var ret = new Bitmap(bmp.Width - step, bmp.Height);

            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, buf, 0, buf.Length);

            var lck2 = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf2 = new byte[lck2.Stride * ret.Height];

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width - step; x++)
                {
                    int src_offset = y * lck.Stride + 3 * x;
                    int dst_offset = y * lck2.Stride + 3 * x;
                    int delta_gray = Math.Abs(buf[src_offset + 3 * step] - buf[src_offset]);
                    //int delta_gray = 0;
                    //for (int i = 1; i <= step; i++)
                    //    delta_gray += Math.Abs(buf[src_offset + i * step] - buf[src_offset]);
                    byte gray = (byte)Math.Min(delta_gray, 255);
                    buf2[dst_offset] = gray; buf2[dst_offset + 1] = gray; buf2[dst_offset + 2] = gray;
                }
            }

            bmp.UnlockBits(lck);
            Marshal.Copy(buf2, 0, lck2.Scan0, buf2.Length);
            ret.UnlockBits(lck2);

            return ret;
        }

        public static Image VerticalGrayDeltafy(Image origin, int step = 1)
        {
            var bmp = new Bitmap(origin);
            var ret = new Bitmap(bmp.Width, bmp.Height - step);

            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, buf, 0, buf.Length);

            var lck2 = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf2 = new byte[lck2.Stride * ret.Height];

            for (int y = 0; y < bmp.Height - step; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int src_offset = y * lck.Stride + 3 * x;
                    int dst_offset = y * lck2.Stride + 3 * x;
                    int delta_gray = Math.Abs(buf[src_offset + 3 * step] - buf[src_offset]);
                    //int delta_gray = 0;
                    //for (int i = 1; i <= step; i++)
                    //    delta_gray += Math.Abs(buf[src_offset + i * step] - buf[src_offset]);
                    byte gray = (byte)Math.Min(delta_gray, 255);
                    buf2[dst_offset] = gray; buf2[dst_offset + 1] = gray; buf2[dst_offset + 2] = gray;
                }
            }

            bmp.UnlockBits(lck);
            Marshal.Copy(buf2, 0, lck2.Scan0, buf2.Length);
            ret.UnlockBits(lck2);

            return ret;
        }

        public static Image CropImage(Image origin, int x, int y, int width, int height, Color? background = null)
        {
            var bmp = new Bitmap(origin);
            var ret = new Bitmap(width, height);

            if (background == null)
                background = Color.White;

            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, buf, 0, buf.Length);
            var lck2 = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf2 = new byte[lck2.Stride * ret.Height];

            for (int ty = 0; ty < height; ty++)
            {
                for (int tx = 0; tx < width; tx++)
                {
                    int dst_offset = ty * lck2.Stride + tx * 3;
                    if (y + ty < 0 || x + tx < 0)
                    {
                        buf2[dst_offset] = background.Value.R;
                        buf2[dst_offset + 1] = background.Value.G;
                        buf2[dst_offset + 2] = background.Value.B;
                        continue;
                    }
                    int src_offset = (y + ty) * lck.Stride + (x + tx) * 3;

                    for (int i = 0; i < 3; i++)
                        buf2[dst_offset + i] = buf[src_offset + i];
                }
            }
            bmp.UnlockBits(lck);

            Marshal.Copy(buf2, 0, lck2.Scan0, buf2.Length);
            ret.UnlockBits(lck2);
            return ret;
        }
        public static Image GrayAdd(Image first, Image second)
        {
            var bmp1 = new Bitmap(first);
            var bmp2 = new Bitmap(second);
            var ret = new Bitmap(bmp1.Width, bmp1.Height);
            if (bmp1.Width != bmp2.Width) throw new ArgumentException("Two image have different width!");
            if (bmp1.Height != bmp2.Height) throw new ArgumentException("Two image have different height!");

            var lck1 = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var lck2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var buf1 = new byte[lck1.Stride * bmp1.Height];
            var buf2 = new byte[buf1.Length];

            Marshal.Copy(lck1.Scan0, buf1, 0, buf1.Length);
            Marshal.Copy(lck2.Scan0, buf2, 0, buf2.Length);

            for (int y = 0; y < ret.Height; y++)
            {
                for (int x = 0; x < ret.Width; x++)
                {
                    int offset = y * lck1.Stride + 3 * x;
                    buf1[offset] += buf2[offset];
                    buf1[offset + 1] += buf2[offset + 1];
                    buf1[offset + 2] += buf2[offset + 2];
                }
            }

            bmp1.UnlockBits(lck1);
            bmp2.UnlockBits(lck2);
            var lck = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Marshal.Copy(buf1, 0, lck.Scan0, buf1.Length);
            ret.UnlockBits(lck);

            return ret;
        }
        public static Image ReverseGray(Image origin)
        {
            var bmp = new Bitmap(origin);
            var ret = new Bitmap(bmp.Width, bmp.Height);

            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, buf, 0, buf.Length);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = y * lck.Stride + x * 3;
                    buf[offset] = (byte)(255 - buf[offset]);
                    buf[offset + 1] = (byte)(255 - buf[offset + 1]);
                    buf[offset + 2] = (byte)(255 - buf[offset + 2]);
                }
            }
            bmp.UnlockBits(lck);

            lck = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, lck.Scan0, buf.Length);
            ret.UnlockBits(lck);
            return ret;
        }

        public static Image ToBinary(Image origin, byte white_threshold = 127)
        {
            byte avg = white_threshold;
            var bmp = new Bitmap(origin);
            var ret = new Bitmap(bmp.Width, bmp.Height);
            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, buf, 0, buf.Length);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = y * lck.Stride + 3 * x;
                    buf[offset] = (byte)((buf[offset] > avg) ? 255 : 0);
                    buf[offset + 1] = (byte)((buf[offset + 1] > avg) ? 255 : 0);
                    buf[offset + 2] = (byte)((buf[offset + 2] > avg) ? 255 : 0);
                }
            }

            bmp.UnlockBits(lck);
            lck = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, lck.Scan0, buf.Length);
            ret.UnlockBits(lck);
            return ret;
        }

        //对每个(window_size x window_size)进行灰度平均
        public static Image AverageGray(Image origin, int window_size, int stride = 1)
        {
            var bmp = new Bitmap(origin);
            var ret = new Bitmap(bmp.Width, bmp.Height);
            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var buf = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, buf, 0, buf.Length);

            for (int y = 0; y < bmp.Height; y += stride)
            {
                for (int x = 0; x < bmp.Width; x += stride)
                {
                    ulong sum = 0;
                    for (int i = 0; i < window_size && y + i < bmp.Height; i++)
                        for (int j = 0; j < window_size && x + j < bmp.Width; j++)
                        {
                            int offset = (y + i) * lck.Stride + 3 * (x + j);
                            sum += buf[offset];
                        }
                    sum /= (ulong)(window_size * window_size);
                    for (int i = 0; i < window_size && y + i < bmp.Height; i++)
                        for (int j = 0; j < window_size && x + j < bmp.Width; j++)
                        {
                            int offset = (y + i) * lck.Stride + 3 * (x + j);
                            buf[offset] = (byte)sum;
                            buf[offset + 1] = (byte)sum;
                            buf[offset + 2] = (byte)sum;
                        }
                }
            }
            bmp.UnlockBits(lck);
            lck = ret.LockBits(new Rectangle(0, 0, ret.Width, ret.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, lck.Scan0, buf.Length);
            ret.UnlockBits(lck);
            return ret;
        }
    }
}

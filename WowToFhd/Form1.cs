using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Drawing.Drawing2D;

namespace WowToFhd
{
    public partial class Form1 : Form
    {
        private const int orgWidth = 854;
        private const int orgHeight = 480;
        private const int newWidth = 1920;
        private const int newHeight = 1080;

        public Form1()
        {
            InitializeComponent();
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void chooseInfile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "wow files (*.wow)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    textBox1.Text = openFileDialog.FileName;
                }
            }
        }

        private void chooseOutfile()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                // openFileDialog.InitialDirectory = "c:\\";
                saveFileDialog.Filter = "fhd files (*.fhd)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 2;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    textBox2.Text = saveFileDialog.FileName;
                }
            }
        }

        private void textBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            chooseInfile();
        }

        private void textBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            chooseOutfile();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0 || textBox2.Text.Length == 0)
            {
                MessageBox.Show("Select input and output files first!");
                return;
            }
            label3.Text = "Processing...";
            label3.Refresh();

            // Open the input and output streams
            using (FileStream infile = File.Open(textBox1.Text, FileMode.Open))
            using (FileStream outfile = File.Open(textBox2.Text, FileMode.Create))
            {
                // The original image data is raw binary bitmap data, 1 bit per pixel
                byte[] imgbuf = new byte[orgWidth * orgHeight / 8];

                String imageFilenameBase = Path.GetDirectoryName(textBox2.Text) + "\\" + Path.GetFileNameWithoutExtension(textBox2.Text);

                // The input file contains both text lines - G-code - and binary image data,
                // but since it's not so easy to do mixed text and binary reads here,
                // we simply read the text as binary too.
                byte[] strbuf = new byte[8192];
                int strind = 0;
                int b;

                // This is just to count the images, for console output and for when
                // we create image files
                int idx = 0;

                // This is needed for converting text to bytes
                ASCIIEncoding enc = new ASCIIEncoding();

                // Some variables that we reuse below just happened to end up here
                Size newsize = new Size(newWidth, newHeight);
                BitmapSizeOptions bmso = BitmapSizeOptions.FromEmptyOptions();

                // Read text, one byte at a time, and collect it in the string buffer
                while ((b = infile.ReadByte()) > 0)
                {
                    strbuf[strind++] = (byte)b;
                    strbuf[strind] = 0;

                    // When we encounter a linefeed, we handle the line
                    if (b == '\n')
                    {
                        // If the line starts with "{{" and image block will follow
                        if (strbuf[0] == '{' && strbuf[1] == '{')
                        {
                            // Read the binary image
                            infile.Read(imgbuf, 0, orgWidth * orgHeight / 8);

                            // Create a bitmap from the binary image. Apparently,
                            // we can't just do this in bulk. Instead we are plotting every
                            // single pixel from the source to the destination
                            using (Bitmap image = new Bitmap(orgWidth, orgHeight))
                            {
                                int x, y;
                                Console.WriteLine("Handling image " + idx.ToString());
                                for (x = 0; x < orgWidth; x++)
                                    for (y = 0; y < orgHeight; y++)
                                    {
                                        // Note that the Sparkmaker Original keeps the bitmap rotated 90
                                        // degrees compared to what you might expect
                                        int pos = (orgWidth - x - 1) * orgHeight + y;
                                        int offs = pos / 8;

                                        // Also, the Sparkmaker keeps the individual pixels in reverse order
                                        // in each byte.
                                        int bit = 1 << (pos % 8);

                                        // Determine the colour: if the bit is one, it's white, otherwise it's black
                                        Color c = ((imgbuf[offs] & bit) != 0) ? Color.White : Color.Black;
                                        image.SetPixel(x, y, c);
                                    }

                                // Now we create a new bitmap with the FHD size
                                // There is a way to do the resizing in a single statement, but
                                // that yields grey borders due to some aliasing artifact, so this is the
                                // way to do it.
                                using (Bitmap image2 = new Bitmap(newWidth, newHeight))
                                {
                                    using (Graphics graphics = Graphics.FromImage(image2))
                                    {
                                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                                        var attributes = new ImageAttributes();
                                        attributes.SetWrapMode(WrapMode.TileFlipXY);

                                        var destination = new Rectangle(0, 0, newWidth, newHeight);
                                        graphics.DrawImage(image, destination, 0, 0, orgWidth, orgHeight,
                                            GraphicsUnit.Pixel, attributes);
                                    }

                                    // Now we need to create an 8-bit grayscale PNG, and of course the 
                                    // Bitmap class can't be used for that. Instead, we have to convert the Bitmap
                                    // to a BitmapSource and then to a FormatConvertedBitmap, after which we
                                    // can use PngBitmapEncoder to create the correct type of PNG.
                                    // Note that the handle "hbm" has to be explicitly deleted afterwards,
                                    // otherwise it will cause a memory leak
                                    IntPtr hbm = image2.GetHbitmap();
                                    BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                        hbm,
                                        IntPtr.Zero,
                                        System.Windows.Int32Rect.Empty,
                                        bmso);
                                    DeleteObject(hbm);
                                    FormatConvertedBitmap fcb = new FormatConvertedBitmap(bs, System.Windows.Media.PixelFormats.Gray8, null, 0);
                                    PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
                                    pngBitmapEncoder.Interlace = PngInterlaceOption.Off;
                                    BitmapFrame bf = BitmapFrame.Create(fcb);
                                    pngBitmapEncoder.Frames.Add(bf);

                                    // Save it to a MemoryStream instead of directly to the file, so
                                    // we can count the bytes and emit a "dataSize" row.
                                    MemoryStream ms = new MemoryStream();
                                    pngBitmapEncoder.Save(ms);
                                    byte[] foo = enc.GetBytes(";dataSize:" + ms.Length.ToString() + "\n{{\n");
                                    outfile.Write(foo, 0, foo.Length);
                                    outfile.Write(ms.ToArray(), 0, (int)ms.Length);
                                    // Not gonna explain all the below since it's mostly the same as the
                                    // previous. Here we save all the original and converted images.
                                    if (checkBox1.Checked)
                                    {
                                        FileStream outfile2 = File.OpenWrite(imageFilenameBase + "_new" + idx.ToString() + ".png");
                                        outfile2.Write(ms.ToArray(), 0, (int)ms.Length);
                                        outfile2.Close();
                                        hbm = image.GetHbitmap();
                                        bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                            hbm,
                                            IntPtr.Zero,
                                            System.Windows.Int32Rect.Empty,
                                            bmso);
                                        DeleteObject(hbm);
                                        fcb = new FormatConvertedBitmap(bs, System.Windows.Media.PixelFormats.Gray8, null, 0);
                                        pngBitmapEncoder = new PngBitmapEncoder();
                                        pngBitmapEncoder.Interlace = PngInterlaceOption.Off;
                                        bf = BitmapFrame.Create(fcb);
                                        pngBitmapEncoder.Frames.Add(bf);
                                        
                                        outfile2 = File.OpenWrite(imageFilenameBase + "_org" + idx.ToString() + ".png");
                                        pngBitmapEncoder.Save(outfile2);
                                        outfile2.Close();
                                    }
                                    idx++;
                                }
                            }
                        }
                        // Otherwise check for some special cases that need to be treated separately:
                        // Width, Height, X and Y
                        else if (strbuf[0] == ';' && ("WHXY".Contains((char)strbuf[1])) && strbuf[2] == ':')
                        {
                            if (strbuf[1] == 'W')
                            {
                                byte[] outbuf = enc.GetBytes(";W:1920;\n");
                                outfile.Write(outbuf, 0, outbuf.Length);
                            }
                            else if (strbuf[1] == 'H')
                            {
                                byte[] outbuf = enc.GetBytes(";H:1080;\n");
                                outfile.Write(outbuf, 0, outbuf.Length);
                            }
                            else if (strbuf[1] == 'X')
                            {
                                byte[] outbuf = enc.GetBytes(";X:110.016\n");
                                outfile.Write(outbuf, 0, outbuf.Length);
                            }
                            else if (strbuf[1] == 'Y')
                            {
                                byte[] outbuf = enc.GetBytes(";Y:61.885\n");
                                outfile.Write(outbuf, 0, outbuf.Length);
                            }
                        }
                        // Otherwise just output the line we just read
                        else
                        {
                            outfile.Write(strbuf, 0, strind);
                        }

                        // Reset the string buffer
                        strind = 0;

                        // Force a GC, since lots of handles and other crap gets accumulated
                        // otherwise
                        System.GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
                if (strind > 0)
                {
                    outfile.Write(strbuf, 0, strind);
                }
            }
            label3.Text = "Done.";
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            chooseInfile();
        }

        private void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            chooseOutfile();
        }
    }
}

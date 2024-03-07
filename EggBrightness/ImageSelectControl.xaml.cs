using Emgu.CV;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EggBrightness
{
    /// <summary>
    /// ImageSelectControl.xaml 的互動邏輯
    /// </summary>
    public partial class ImageSelectControl : UserControl, INotifyPropertyChanged
    {
        public Mat Mat { get; set; }
        public BitmapImage BitmapImage { get; set; }

        public event EventHandler<BitmapImage> ReadImageCompleted;

        public event PropertyChangedEventHandler PropertyChanged;
        public ImageSelectControl()
        {
            InitializeComponent();
        }

        public void SelectImage_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var clickOK = dialog.ShowDialog();
            if ((bool)clickOK)
            {
                var imagePath = dialog.FileName;
                Mat = new Mat(imagePath);
                BitmapImage = ConvertToBitmapImage(Mat.Bitmap);
                ReadImageCompleted?.Invoke(this, BitmapImage);
            }
        }

        public BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            if (bitmap != null)
            {
                BitmapImage image = new BitmapImage();
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Bmp);
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    ms.Seek(0, SeekOrigin.Begin);
                    image.StreamSource = ms;
                    image.EndInit();
                };
                return image;
            }
            else
            {
                return null;
            }
        }
    }
}

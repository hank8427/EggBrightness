using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace EggBrightness
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        public Mat Mat { get; set; }
        public BitmapImage BitmapImage { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageViewModel(BitmapImage bitmapImage)
        {
            BitmapImage = bitmapImage;
        }
    }
}

using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing;
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
using Rectangle = System.Drawing.Rectangle;

namespace EggBrightness
{
    /// <summary>
    /// EggBrightnessSelectionControl.xaml 的互動邏輯
    /// </summary>
    public partial class EggBrightnessSelectionControl : UserControl, INotifyPropertyChanged
    {
        public BrighetnessSelectorSetting SelectorSetting { get; set; }
        public List<BrighetnessSelectorSetting> SelectorSettingList { get; set; }
        public int LeftIndex { get; set; }
        public int MiddleIndex { get; set; }
        public int RightIndex { get; set; }
        public BitmapImage CombinedImage { get; set; }
        public ImageViewModel FirstImageViewModel { get; set; }
        public ImageViewModel SecondImageViewModel { get; set; }
        public ImageViewModel ThirdImageViewModel { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public EggBrightnessSelectionControl()
        {
            InitializeComponent();

            FirstImageViewModel = new ImageViewModel(null);
            SecondImageViewModel = new ImageViewModel(null);
            ThirdImageViewModel = new ImageViewModel(null);

            var imageViewModels = new Dictionary<UserControl, ImageViewModel>
            {
                { First, FirstImageViewModel },
                { Second, SecondImageViewModel },
                { Third, ThirdImageViewModel }
            };

            First.ReadImageCompleted += (sender, e) => Image_ReadImageCompleted(sender, e, imageViewModels);
            Second.ReadImageCompleted += (sender, e) => Image_ReadImageCompleted(sender, e, imageViewModels);
            Third.ReadImageCompleted += (sender, e) => Image_ReadImageCompleted(sender, e, imageViewModels);

            PropertyChanged += MainWindow_PropertyChanged;
            SelectorSettingListInitialize();
        }

        private void SelectorSettingListInitialize()
        {
            SelectorSettingList = new List<BrighetnessSelectorSetting>
            {
                new BrighetnessSelectorSetting(){ Name="CameraLeft", LeftGrid = 816, RightGrid=1632, BrightTHR = new BrightTHR()},
                new BrighetnessSelectorSetting(){ Name="CameraUp", LeftGrid = 816, RightGrid=1632, BrightTHR = new BrightTHR()},
                new BrighetnessSelectorSetting(){ Name="CameraRight", LeftGrid = 816, RightGrid=1632, BrightTHR = new BrightTHR()},
            };

            //EggBrightnessSelector.SelectorSetting = SelectorSetting;
            SelectorSetting = SelectorSettingList[0];
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //EggBrightnessSelector.SelectorSetting = SelectorSetting;
            Console.WriteLine(SelectorSetting.LeftGrid.ToString());
        }

        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LeftGrid" || e.PropertyName == "RightGrid")
            {
                var width = FirstImageViewModel.Mat?.Width;
                if (width != 0 && SelectorSetting.LeftGrid > width || SelectorSetting.RightGrid > width)
                {
                    MessageBox.Show($"Grid Setting must less than image width {width}");
                    if (SelectorSetting.LeftGrid > width)
                    {
                        SelectorSetting.LeftGrid = (int)(width / 3);
                    }
                    else
                    {
                        SelectorSetting.RightGrid = (int)(width / 3 * 2);
                    }
                    return;
                }
                else if (SelectorSetting.LeftGrid >= SelectorSetting.RightGrid)
                {
                    MessageBox.Show("LeftGrid must less tha RightGrid");
                    SelectorSetting.LeftGrid = (int)(width / 3);
                    SelectorSetting.RightGrid = (int)(width / 3 * 2);
                    return;
                }
            }
        }

        private void Image_ReadImageCompleted(object sender, BitmapImage e, Dictionary<UserControl, ImageViewModel> imageViewModels)
        {
            UserControl control = (UserControl)sender;

            if (!imageViewModels.TryGetValue(control, out var imageViewModel) || imageViewModel == null)
            {
                imageViewModel = new ImageViewModel(e)
                {
                    Mat = BitmapImage2Bitmap(e)
                };
                imageViewModels[control] = imageViewModel;
            }
            else
            {
                imageViewModel.BitmapImage = e;
                imageViewModel.Mat = BitmapImage2Bitmap(e);
            }

            //SelectorSetting.LeftGrid = imageViewModel.Mat.Width / 3;
            //SelectorSetting.RightGrid = imageViewModel.Mat.Width / 3 * 2;
        }

        private void Combine_OnClick(object sender, RoutedEventArgs e)
        {
            var result = EggBrightnessSelector.Select(FirstImageViewModel.Mat, SecondImageViewModel.Mat, ThirdImageViewModel.Mat, SelectorSetting);
            CombinedImage = ConvertToBitmapImage(result?.Bitmap);

            LeftIndex = EggBrightnessSelector.GetTargetImageIndex("Left", SelectorSetting);
            MiddleIndex = EggBrightnessSelector.GetTargetImageIndex("Middle", SelectorSetting);
            RightIndex = EggBrightnessSelector.GetTargetImageIndex("Right", SelectorSetting);
        }

        private Mat BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                var bitmap = new Bitmap(outStream);

                var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                       System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                       System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                var mat = new Mat(bmpData.Height, bmpData.Width, Emgu.CV.CvEnum.DepthType.Cv8U, 3, bmpData.Scan0, bmpData.Stride);

                return mat;
            }
        }

        public static BitmapImage ConvertToBitmapImage(Bitmap bitmap)
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

        private void FindContour_OnClick(object sender, RoutedEventArgs e)
        {
            EggBrightnessSelector.FindContour();
        }
    }
}

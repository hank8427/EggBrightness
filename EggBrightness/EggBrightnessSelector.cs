using EggBrightness.Properties;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EggBrightness
{
    public static class EggBrightnessSelector
    {
        private static object myLockObject = new object();
        private static Dictionary<string, List<RoiInfo>> myRoiDictionary;
        public static List<BrighetnessSelectorSetting> SelectorSettingList { get; set; }
        //public static BrighetnessSelectorSetting SelectorSetting { get; set; } = new BrighetnessSelectorSetting();
        public static Mat Select(Mat image1, Mat image2, Mat image3, BrighetnessSelectorSetting setting)
        {
            lock (myLockObject)
            {

                var mats = new List<Mat>() { image1, image2, image3 };

                if (mats.Count(x => x != null) < 3)
                {
                    return null;
                }

                var brightnessList = new List<double>();

                var roiDictionary = GenerateRoiDictionary();

                int pos = 0;

                foreach (var pair in roiDictionary)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        var rect = new Rectangle();
                        if (pos == 0)
                            rect = new Rectangle(0, 0, setting.LeftGrid, mats[index].Height);
                        else if (pos == 1)
                            rect = new Rectangle(setting.LeftGrid, 0, setting.RightGrid - setting.LeftGrid, mats[index].Height);
                        else
                            rect = new Rectangle(setting.RightGrid, 0, mats[index].Width - setting.RightGrid, mats[index].Height);

                        if (mats[index].Width < rect.Right || mats[index].Height < rect.Bottom)
                        {
                            MessageBox.Show("Please check image size and Left/Right grid setting.");
                            return null;
                        }

                        var mat = new Mat(mats[index], rect);

                        Mat grayMat = new Mat();
                        CvInvoke.CvtColor(mat, grayMat, ColorConversion.Rgb2Gray);

                        pair.Value[index].Index = index;

                        pair.Value[index].Brightness = CvInvoke.Mean(grayMat).V0;
                        pair.Value[index].Mat = mat;
                        //CvInvoke.Imshow($"{index}-{pos}", grayMat);
                    }

                    pos++;
                }

                myRoiDictionary = roiDictionary;

                return CombineImage(roiDictionary, setting);
            }
        }

        private static Dictionary<string, List<RoiInfo>> GenerateRoiDictionary()
        {
            var roiDictionary = new Dictionary<string, List<RoiInfo>>()
            {
                { "Left", Enumerable.Range(0, 3).Select(_ => new RoiInfo()).ToList()},
                { "Middle", Enumerable.Range(0, 3).Select(_ => new RoiInfo()).ToList()},
                { "Right", Enumerable.Range(0, 3).Select(_ => new RoiInfo()).ToList()},
            };
            return roiDictionary;
        }

        private static Mat CombineImage(Dictionary<string, List<RoiInfo>> roiDict, BrighetnessSelectorSetting setting)
        {
            var combinedMat = new Mat();

            var mat1 = roiDict["Left"].OrderBy(x => Math.Abs(x.Brightness - setting.BrightTHR.LeftTHR)).First().Mat;
            var mat2 = roiDict["Middle"].OrderBy(x => Math.Abs(x.Brightness - setting.BrightTHR.MiddleTHR)).First().Mat;
            var mat3 = roiDict["Right"].OrderBy(x => Math.Abs(x.Brightness - setting.BrightTHR.RightTHR)).First().Mat;

            CvInvoke.HConcat(mat1, mat2, combinedMat);
            CvInvoke.HConcat(combinedMat, mat3, combinedMat);

            //CvInvoke.Line(combinedMat, new Point(mat1.Width, 0), new Point(mat1.Width, mat1.Height), new MCvScalar(0, 0, 255), 2, LineType.FourConnected);
            //CvInvoke.Line(combinedMat, new Point(mat1.Width + mat2.Width, 0), new Point(mat1.Width + mat2.Width, mat1.Height), new MCvScalar(0, 0, 255), 2, LineType.FourConnected);

            return combinedMat;
        }

        public static int GetTargetImageIndex(string position, BrighetnessSelectorSetting setting)
        {
            if (myRoiDictionary == null)
            {
                return 0;
            }

            double brightTHR;
            switch (position)
            {
                case "Left":
                    brightTHR = setting.BrightTHR.LeftTHR;
                    break;
                case "Middle":
                    brightTHR = setting.BrightTHR.MiddleTHR;
                    break;
                case "Right":
                    brightTHR = setting.BrightTHR.RightTHR;
                    break;
                default: return 0;
            }

            //var index = roiList.OrderBy(x => Math.Abs(x.Brightness - SelectorSetting.BrightTHR.LeftTHR)).First().Index;
            var index = myRoiDictionary[position].OrderBy(x => Math.Abs(x.Brightness - brightTHR)).First().Index;

            return index;
        }

        public static void FindContour()
        {
            if (myRoiDictionary == null)
            {
                return;
            }
            var orig = myRoiDictionary["Left"][0].Mat.Clone();

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

            Mat m = new Mat();
            Mat grayMat = new Mat();
            CvInvoke.CvtColor(orig, grayMat, ColorConversion.Rgb2Gray);
            CvInvoke.Threshold(grayMat, grayMat, 100, 200, ThresholdType.Binary);
            CvInvoke.FindContours(grayMat, contours, m, RetrType.External, ChainApproxMethod.ChainApproxNone);

            CvInvoke.Imshow("Orig", grayMat);

            for (int i = 0; i < contours.Size; i++)
            {
                double perimeter = CvInvoke.ArcLength(contours[i], true);
                VectorOfPoint approx = new VectorOfPoint();
                CvInvoke.ApproxPolyDP(contours[i], approx, 0.04 * perimeter, true);

                CvInvoke.DrawContours(orig, contours, i, new MCvScalar(0, 0, 255), 2);
            }
            CvInvoke.Imshow("Contour", orig);
        }
    }
}

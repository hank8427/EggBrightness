using EggBrightness.Properties;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
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

                var sortedMats = SortMats(mats);

                var grayMats = new List<Mat>();

                if (sortedMats.Count(x => x != null) < 3)
                {
                    return null;
                }

                var brightnessList = new List<double>();

                var roiDictionary = GenerateRoiDictionary();

                int pos = 0;

                var offset = new int[]{0, 60, 110};

                //Parallel.ForEach(roiDictionary, pair =>
                foreach (var pair in roiDictionary)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        var rect = new Rectangle();
                        if (pos == 0)
                            rect = new Rectangle(0, 0, setting.LeftGrid - offset[index], sortedMats[index].Height);
                        else if (pos == 1)
                            rect = new Rectangle(setting.LeftGrid - offset[index], 0, setting.RightGrid - setting.LeftGrid, sortedMats[index].Height);
                        else
                            rect = new Rectangle(setting.RightGrid- offset[index], 0, sortedMats[index].Width - setting.RightGrid, sortedMats[index].Height);

                        if (sortedMats[index].Width < rect.Right || sortedMats[index].Height < rect.Bottom)
                        {
                            MessageBox.Show("Please check image size and Left/Right grid setting.");
                            //return null;
                        }

                        pair.Value[index].Index = index;                        

                        var newMat = new Mat(sortedMats[index], rect);

                        if (pos == 0 && index > 0)
                        {
                            var padding = Mat.Zeros(rect.Height, offset[index], DepthType.Cv8U, 3);
                            CvInvoke.HConcat(padding, newMat, newMat);
                        }

                        var temp = CvInvoke.Mean(newMat);
                        var brightness = (temp.V0 + temp.V1 + temp.V2)/3;
                        pair.Value[index].Brightness = brightness;

                        pair.Value[index].Mat = newMat;
                    }
                    
                    pos++;


                };
                
                myRoiDictionary = roiDictionary;

                grayMats = null;

                var combinedImage = CombineImage(roiDictionary, setting);

                return combinedImage;
            }
        }

        private static List<Mat> SortMats(List<Mat> mats)
        {
            List<double> brightness = new List<double>();

            for(int i = 0; i < mats.Count; i++)
            {
                var averageValue = CvInvoke.Mean(mats[i]);
                brightness.Add((averageValue.V0 + averageValue.V1 + averageValue.V2)/3);
            }

            brightness.IndexOf(brightness.Min());
            
            brightness.IndexOf(brightness.Max());

            mats.Sort((mat1, mat2) =>
            {
                double bright1 = (CvInvoke.Mean(mat1).V0 + CvInvoke.Mean(mat1).V1 + CvInvoke.Mean(mat1).V2) / 3;
                double bright2 = (CvInvoke.Mean(mat2).V0 + CvInvoke.Mean(mat2).V1 + CvInvoke.Mean(mat2).V2) / 3;
                return bright1.CompareTo(bright2);
            });

            return mats;
        }

        private static Mat GetSingleChannelMat(Mat mat, int channel)
        {
            byte[] data = new byte[mat.Rows * mat.Cols];

            Marshal.Copy(mat.DataPointer + channel * mat.Rows * mat.Cols, data, 0, data.Length);

            Mat extractedMat = new Mat(mat.Rows, mat.Cols, DepthType.Cv8U, 1);

            Marshal.Copy(data, 0, extractedMat.DataPointer, data.Length);

            return extractedMat;
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

            return index+1;
        }

        //變更，此項可能沒有實際功能
        public static int GetTargetImageBright(string position, BrighetnessSelectorSetting setting)
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
            var index = myRoiDictionary[position].OrderBy(x => Math.Abs(x.Brightness - brightTHR)).First().Brightness;

            return (int)index;
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

        public static bool GetLockIsRelease()
        {
            return Monitor.IsEntered(myLockObject);
        }
    }
}

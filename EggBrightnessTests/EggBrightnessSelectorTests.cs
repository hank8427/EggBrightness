using Microsoft.VisualStudio.TestTools.UnitTesting;
using EggBrightness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;

namespace EggBrightness.Tests
{
    [TestClass()]
    public class EggBrightnessSelectorTests
    {
        private object myLockObject = new object();
        private Dictionary<string, List<RoiInfo>> myRoiDictionary;
        private List<Mat> Camera1 = new List<Mat>() { new Mat("TestImage\\Camera1_1.png", Emgu.CV.CvEnum.ImreadModes.Grayscale), new Mat("TestImage\\Camera1_2.png", Emgu.CV.CvEnum.ImreadModes.Grayscale), new Mat("TestImage\\Camera1_3.png", Emgu.CV.CvEnum.ImreadModes.Grayscale) };
        private List<Mat> Camera2 = new List<Mat>() { new Mat("TestImage\\Camera2_1.png"), new Mat("TestImage\\Camera2_2.png"), new Mat("TestImage\\Camera2_3.png") };
        private List<Mat> Camera3 = new List<Mat>() { new Mat("TestImage\\Camera3_1.png"), new Mat("TestImage\\Camera3_2.png"), new Mat("TestImage\\Camera3_3.png") };
        private BrighetnessSelectorSetting TestSetting = new BrighetnessSelectorSetting() { Name = "CameraTest", LeftGrid = 816, RightGrid = 1632, BrightTHR = new BrightTHR() };


        public EggBrightnessSelectorTests()
        {
            Camera1[0].Save("test123.png");
        }

        [TestMethod()]
        public void SelectTest()
        {
            var startTime = DateTime.Now;

            EggBrightnessSelector.Select(Camera1[0], Camera1[1], Camera1[2], TestSetting);

            var stopTime = DateTime.Now;

            var testTime = (stopTime - startTime).TotalMilliseconds;

            Console.WriteLine(testTime);

            var expectTime = 20;

            //Assert.IsTrue(testTime < expectTime);
        } 

        [TestMethod()]
        public void SplitTimeTest()
        {
            var mat = Camera2[0];

            var grayMat = new Mat();

            var startTime = DateTime.Now;

            var matSplit = mat.Split();

            grayMat = matSplit[0].Clone();

            var stopTime = DateTime.Now;

            var expectTime = 10;

            var testTime = (stopTime - startTime).TotalMilliseconds;

            Assert.IsTrue(testTime < expectTime);
        }

        [TestMethod()]
        public void CopyToTimeTest()
        {
            var mat = Camera2[0];

            var grayMat = new Mat();

            var startTime = DateTime.Now;

            //grayMat = mat.Clone();
            mat.CopyTo(grayMat);

            var stopTime = DateTime.Now;

            var expectTime = 10;

            var testTime = (stopTime - startTime).TotalMilliseconds;

            Assert.IsTrue(testTime < expectTime);
        }

        [TestMethod()]
        public void Select2()
        {
            lock (myLockObject)
            {
                var startTime = DateTime.Now;

                var mats = new List<Mat>() { Camera2[0], Camera2[1], Camera2[2] };

                var grayMats = new List<Mat>();

                foreach (var mat in mats)
                {
                    var grayMat = new Mat();

                    if (mat.Bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
                    {
                        var startTime2 = DateTime.Now;
                        
                        //var matSplit = mat.Split();
                        //grayMat = matSplit[0].Clone();

                        grayMat = GetSingleChannelMat(mat, 0);

                        var stopTime2 = DateTime.Now;

                        var testTime2 = (stopTime2 - startTime2).TotalMilliseconds;

                        Console.WriteLine($"Brightness Select2 Time2 =========>{testTime2}");
                    }
                    else
                    {
                        grayMat = mat.Clone();
                    }
                    grayMats.Add(grayMat);
                }

                var brightnessList = new List<double>();

                var roiDictionary = GenerateRoiDictionary();

                int pos = 0;

                //Parallel.ForEach(roiDictionary, pair =>
                foreach (var pair in roiDictionary)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        var rect = new Rectangle();
                        if (pos == 0)
                            rect = new Rectangle(0, 0, TestSetting.LeftGrid, mats[index].Height);
                        else if (pos == 1)
                            rect = new Rectangle(TestSetting.LeftGrid, 0, TestSetting.RightGrid - TestSetting.LeftGrid, mats[index].Height);
                        else
                            rect = new Rectangle(TestSetting.RightGrid, 0, mats[index].Width - TestSetting.RightGrid, mats[index].Height);

                        var newMat = new Mat(mats[index], rect);

                        var newGrayMat = new Mat(grayMats[index], rect);

                        pair.Value[index].Index = index;

                        pair.Value[index].Brightness = CvInvoke.Mean(newGrayMat).V0;
                        pair.Value[index].Mat = newMat;
                    }

                    pos++;
                };

                myRoiDictionary = roiDictionary;

                grayMats = null;

                var combinedImage = CombineImage(roiDictionary, TestSetting);

                var stopTime = DateTime.Now;

                var testTime = (stopTime - startTime).TotalMilliseconds;

                Console.WriteLine($"Brightness Select2 Time =========>{testTime}");

                var expectTime = 30;

                Assert.IsTrue(testTime < expectTime);
            }
        }

        private Dictionary<string, List<RoiInfo>> GenerateRoiDictionary()
        {
            var roiDictionary = new Dictionary<string, List<RoiInfo>>()
            {
                { "Left", Enumerable.Range(0, 3).Select(_ => new RoiInfo()).ToList()},
                { "Middle", Enumerable.Range(0, 3).Select(_ => new RoiInfo()).ToList()},
                { "Right", Enumerable.Range(0, 3).Select(_ => new RoiInfo()).ToList()},
            };
            return roiDictionary;
        }

        private Mat CombineImage(Dictionary<string, List<RoiInfo>> roiDict, BrighetnessSelectorSetting setting)
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

        private Mat GetSingleChannelMat(Mat mat, int channel)
        {
            byte[] data = new byte[mat.Rows * mat.Cols];

            Marshal.Copy(mat.DataPointer + channel * mat.Rows * mat.Cols, data, 0, data.Length);

            Mat extractedMat = new Mat(mat.Rows, mat.Cols, DepthType.Cv8U, 1);

            Marshal.Copy(data, 0, extractedMat.DataPointer, data.Length);

            return extractedMat;
        }
    }
}
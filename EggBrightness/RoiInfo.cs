using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggBrightness
{
    public class RoiInfo
    {
        public int Index { get; set; }
        public Mat Mat { get; set; }
        public double Brightness {  get; set; }

        public RoiInfo() 
        {
            Index = 0;
            Mat = new Mat();
            Brightness = 0.0;
        }
    }
}

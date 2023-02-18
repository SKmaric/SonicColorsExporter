using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicColorsExporter
{
    public class SettingsFlags
    {
        public bool scaleMode;
        public bool singleBindMode;
        public bool multimatCombine;
        public bool tagMat;
        public bool tagObj;
        public bool UVOrganize;
        public bool lightmapMatMerge;
        public bool opaAddGeo;
        public bool AnimsXML;
        public float cFactor = 1.0f / 255.0f;
        public float mFactor = 1.0f / 10.0f;
    }
}

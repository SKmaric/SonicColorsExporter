using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicColorsExporter
{
    public class SettingsFlags
    {
        public bool separateFolders;
        public bool scaleMode;
        public bool singleBindMode;
        public bool multimatCombine;
        public bool tagMat;
        public bool tagObj;
        public bool UVOrganize;
        public bool lightmapMatMerge;
        public bool opaAddGeo;
        public bool AnimsXML;
        public bool chr0DAE = false;
        public bool flipXUV;
        public float cFactor = 1.0f / 255.0f;
        public float mFactor = 1.0f / 10.0f;

        public bool processMDL = true;
        public bool processCHR = true;
        public bool processSCN = true;
        public bool processSRT = true;
        public bool processVIS = true;
        public bool processPAT = true;
        public bool processCLR = true;
        public bool processREFF = true;
        public bool processGISM = true;
        public bool splitCameraShots = false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrawlLib.Modeling.Collada;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.Wii.Animations;
using HedgeLib.Models;

namespace SonicColorsExporter
{
    internal class CHR0Handler
    {
        public static void processCHR0(CHR0Node chr0, string outpath, SettingsFlags flags, bool solo = true)
        {
            float mFactor = 1.0f;
            if (flags.scaleMode)
                mFactor = flags.mFactor;
                
            string outfile = outpath + "\\" + chr0.Name + (flags.chr0DAE?".dae":".anim");
            if (!solo)
            {
                foreach (BRESGroupNode group in chr0.Parent.Parent.Children)
                {
                    if (group.Type == BRESGroupNode.BRESGroupType.Models)
                    {
                        foreach (MDL0Node model in group.Children)
                        {
                            if (flags.chr0DAE)
                            {
                                if (model.Name == chr0.Name || model.Name == "chr_Sonic_SD")
                                    ColladaExportColors.Serialize(chr0, 60.0f, false, outfile, model);
                            }
                            else
                            {
                                if (model.Name == chr0.Name || model.Name == "chr_Sonic_SD")
                                    AnimFormat.Serialize(chr0, outfile, model, mFactor);
                            }
                        }
                    }
                }
            }
            else
            {
                if (flags.chr0DAE)
                {
                    ColladaExportColors.Serialize(chr0, 60.0f, false, outfile);
                }
                else
                    AnimFormat.Serialize(chr0, outfile, mFactor);
            }
                
        }
    }
}

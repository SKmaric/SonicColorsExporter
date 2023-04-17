using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.Wii.Animations;

namespace SonicColorsExporter
{
    internal class CHR0Handler
    {
        public static void processCHR0(CHR0Node chr0, string outpath, SettingsFlags flags, bool solo = true)
        {
            string outfile = outpath + "\\" + chr0.Name + ".anim";
            if (!solo)
            {
                foreach (BRESGroupNode group in chr0.Parent.Parent.Children)
                {
                    if (group.Type == BRESGroupNode.BRESGroupType.Models)
                    {
                        foreach (MDL0Node model in group.Children)
                        {
                            if (model.Name == chr0.Name)
                                AnimFormat.Serialize(chr0, outfile, model);
                        }
                    }
                }
            }
            else
                AnimFormat.Serialize(chr0, outfile);
        }
    }
}

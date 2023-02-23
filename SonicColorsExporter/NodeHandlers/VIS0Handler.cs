using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;
using BrawlLib.SSBB.ResourceNodes;
using HedgeLib;
using HedgeLib.IO;
using HedgeLib.Exceptions;
using HedgeLib.Animations;

namespace SonicColorsExporter
{
    internal class VIS0Handler : AnimNodeHandler
    {
        public static void processVIS0(VIS0Node vis0, string outpath, SettingsFlags flags)
        {
            string outfile;

            VisibilityAnimation animation = ConvertVISAnim(vis0, flags);

            animation.ModelName = vis0.Name;
            animation.MeshName = "default";

            if (flags.AnimsXML)
            {
                outfile = outpath + "\\" + vis0.Name + ".vis-anim.xml";
                animation.ExportXML(outfile);
            }
            else
            {
                outfile = outpath + "\\" + vis0.Name + ".vis-anim";
                animation.Save(outfile);
            }
        }

        private static VisibilityAnimation ConvertVISAnim(VIS0Node vis0, SettingsFlags flags)
        {
            VisibilityAnimation animation = new VisibilityAnimation();

            animation.Header.RootNodeType = 1;

            foreach (VIS0EntryNode node in vis0.Children)
            {
                var anim = ConvertAnim(node, flags);
                if (anim != null)
                    animation.Animations.Add(anim);
            }
            return animation;
        }

        private static GensAnimation.Animation ConvertAnim(VIS0EntryNode node, SettingsFlags flags)
        {
            // Skip meshes with no animation data
            if (node.EntryCount <= 0)
                return null;

            VIS0Handler vIS0Handler = new VIS0Handler();

            GensAnimation.Animation anim = new GensAnimation.Animation();
            var keyframes = new GensAnimation.KeyframeSet();

            anim.Name = node.Name;
            anim.FPS = 60f;
            anim.StartTime = 0;
            anim.EndTime = node.EntryCount - 1;


            keyframes.Flag1 = 0;
            keyframes.Flag2 = 0;
            keyframes.Flag3 = 0;
            keyframes.Flag4 = 0;

            for (uint i = 0; i < node.EntryCount; ++i)
            {
                var value = Convert.ToSingle(node.GetEntry((int)i));

                // Remove unnecessary keyframes
                if (i > 0 && i < node.EntryCount - 1)
                {
                    var prevValue = Convert.ToSingle(node.GetEntry((int)i - 1));
                    var nextValue = Convert.ToSingle(node.GetEntry((int)i + 1));
                    if (prevValue == value && value == nextValue)
                        continue;
                }

                keyframes.Add(vIS0Handler.ConvertKeyframe(i, value));
            }

            anim.KeyframeSets.Add(keyframes);

            return anim;
        }
    }
}
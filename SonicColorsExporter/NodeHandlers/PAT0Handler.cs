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
    internal class PAT0Handler : AnimNodeHandler
    {
        public static void processPAT0(PAT0Node pat0, string outpath, SettingsFlags flags)
        {
            foreach (PAT0EntryNode pat in pat0.Children)
            {
                foreach (PAT0TextureNode node in pat.Children)
                {
                    string outfile;

                    // Parse texture/map ID from name
                    int i = int.Parse(new String(node.Name.Where(Char.IsDigit).ToArray()));

                    PatternAnimation animation = ConvertPATAnim(node, flags);
                    animation.MaterialName = pat.Name;
                    animation.MapName = pat.Name + "-" + i.ToString("D4");

                    if (flags.AnimsXML)
                    {
                        outfile = outpath + "\\" + animation.MapName + ".pt-anim.xml";
                        animation.ExportXML(outfile);
                    }
                    else
                    {
                        outfile = outpath + "\\" + animation.MapName + ".pt-anim";
                        animation.Save(outfile);
                    }
                }
            }
        }

        private static PatternAnimation ConvertPATAnim(PAT0TextureNode node, SettingsFlags flags)
        {
            PatternAnimation animation = new PatternAnimation();

            animation.Animations.Add(ConvertAnim(node, flags));

            return animation;
        }

        private static GensAnimation.Animation ConvertAnim(PAT0TextureNode node, SettingsFlags flags)
        {
            PAT0Handler pAT0Handler = new PAT0Handler();
            GensAnimation.Animation anim = new GensAnimation.Animation();
            var keyframes = new GensAnimation.KeyframeSet();

            anim.Name = "default";
            anim.FPS = 60f;
            anim.StartTime = 0;
            anim.EndTime = node.NumFrames - 1;

            keyframes.Flag1 = 0;
            keyframes.Flag2 = 1;
            keyframes.Flag3 = 0;
            keyframes.Flag4 = 0;

            int index = -1;
            foreach (PAT0TextureEntryNode e in node.Children)
            {
                if (!string.IsNullOrEmpty(e._tex))
                {
                    if ((index = keyframes.textureNames.IndexOf(e._tex)) < 0)
                    {
                        keyframes.textureNames.Add(e._tex);
                    }
                }
                var value = Convert.ToSingle(keyframes.textureNames.IndexOf(e._tex));

                keyframes.Add(pAT0Handler.ConvertKeyframe((uint)e.FrameIndex, value));
            }

            anim.KeyframeSets.Add(keyframes);
            return anim;
        }
    }
}
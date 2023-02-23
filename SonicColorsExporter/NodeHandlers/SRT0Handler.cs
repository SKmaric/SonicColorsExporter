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
    internal class SRT0Handler : AnimNodeHandler
    {
        public static void processSRT0(SRT0Node srt0, string outpath, SettingsFlags flags)
        {
            foreach (SRT0EntryNode mat in srt0.Children)
            {
                foreach (SRT0TextureNode node in mat.Children)
                {
                    string outfile;

                    // Parse texture/map ID from name
                    int i = int.Parse(new String(node.Name.Where(Char.IsDigit).ToArray()));

                    UVAnimation animation = ConvertUVAnim(node, flags);
                    animation.MaterialName = mat.Name;
                    animation.MapName = mat.Name + "-" + i.ToString("D4");

                    if (flags.AnimsXML)
                    {
                        outfile = outpath + "\\" + animation.MapName + ".uv-anim.xml";
                        animation.ExportXML(outfile);
                    }
                    else
                    {
                        outfile = outpath + "\\" + animation.MapName + ".uv-anim";
                        animation.Save(outfile);
                    }
                }
            }
        }

        private static UVAnimation ConvertUVAnim(SRT0TextureNode node, SettingsFlags flags)
        {
            UVAnimation animation = new UVAnimation();

            animation.Header.RootNodeType = 2;

            animation.Animations.Add(ConvertAnim(node, flags));

            return animation;
        }

        private static GensAnimation.Animation ConvertAnim(SRT0TextureNode node, SettingsFlags flags)
        {
            SRT0Handler sRT0Handler = new SRT0Handler();
            GensAnimation.Animation anim = new GensAnimation.Animation();

            anim.Name = "default";
            anim.FPS = 60f;
            anim.StartTime = 0;
            anim.EndTime = node.FrameCount - 1;

            int id = 0;
            foreach (var set in node.KeyArrays)
            {
                var keyframes = sRT0Handler.ConvertKeyframeSet(set, id, flags);
                if (keyframes != null)
                    anim.KeyframeSets.Add(keyframes);
                id++;
            }

            return anim;
        }

        public override GensAnimation.KeyframeSet ConvertKeyframeSet(BrawlLib.Wii.Animations.KeyframeArray set, int id, SettingsFlags flags)
        {
            switch (id)
            {
                case 3:
                case 4:
                    id = id - 3;
                    break;
                default:
                    return null;
            }

            GensAnimation.KeyframeSet keyframes = new GensAnimation.KeyframeSet();

            keyframes.Flag1 = (byte)id;
            keyframes.Flag2 = 0;
            keyframes.Flag3 = 0;
            keyframes.Flag4 = 0;

            for (uint i = 0; i < set.FrameLimit; ++i)
            {
                var value = set.GetFrameValue(i);

                // Remove unnecessary keyframes
                if (i > 0 && i < set.FrameLimit - 1)
                {
                    var prevValue = set.GetFrameValue(i - 1);
                    var nextValue = set.GetFrameValue(i + 1);
                    if (prevValue == value && value == nextValue)
                        continue;
                }
                
                //Needed fix for Sonic eyes might not be good for others
                if (id == 0 && flags.flipXUV)
                    value = -value;

                keyframes.Add(ConvertKeyframe(i, value));
            }
            // Remove sets with no animation data
            if (keyframes.Count < 1)
                return null;
            if (keyframes.Count == 2)
                if (keyframes[0].Value == keyframes[1].Value)
                {
                    keyframes.RemoveAt(1);
                    if (keyframes[0].Value == 0)
                        return null;
                }

            return keyframes;
        }
    }
}
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
    internal class CLR0Handler : AnimNodeHandler
    {
        public static void processCLR0(CLR0Node clr0, string outpath, SettingsFlags flags)
        {
            foreach (CLR0MaterialNode mat in clr0.Children)
            {
                string outfile;

                MaterialAnimation animation = ConvertCLRAnim(mat, flags);
                animation.MaterialName = mat.Name;

                if (flags.AnimsXML)
                {
                    outfile = outpath + "\\" + animation.MaterialName + ".mat-anim.xml";
                    animation.ExportXML(outfile);
                }
                else
                {
                    outfile = outpath + "\\" + animation.MaterialName + ".mat-anim";
                    animation.Save(outfile);
                }
            }
        }

        private static MaterialAnimation ConvertCLRAnim(CLR0MaterialNode node, SettingsFlags flags)
        {
            MaterialAnimation animation = new MaterialAnimation();

            animation.Animations.Add(ConvertAnim(node, flags));

            return animation;
        }

        private static GensAnimation.Animation ConvertAnim(CLR0MaterialNode node, SettingsFlags flags)
        {
            CLR0Handler cLR0Handler = new CLR0Handler();
            GensAnimation.Animation anim = new GensAnimation.Animation();

            anim.Name = "default";
            anim.FPS = 60f;
            anim.StartTime = 0;
            anim.EndTime = ((CLR0Node)node.Parent).FrameCount - 1;

            foreach (CLR0MaterialEntryNode set in node.Children)
            {
                for (int i = 0; i < 4; i++)
                {
                    int targetid = i;
                    switch (set.Target)
                    {
                        case BrawlLib.SSBB.Types.Animations.EntryTarget.LightChannel0AmbientColor:
                        case BrawlLib.SSBB.Types.Animations.EntryTarget.LightChannel1AmbientColor:
                            targetid += 4;
                            break;
                        default: // need to figure out the rest of the flag1 values
                            break;
                    }
                        var keyframes = cLR0Handler.ConvertKeyframeSet(set, targetid, i, flags);
                        if (keyframes != null)
                            anim.KeyframeSets.Add(keyframes);
                }
            }

            return anim;
        }

        public GensAnimation.KeyframeSet ConvertKeyframeSet(CLR0MaterialEntryNode set, int id, int colorid, SettingsFlags flags)
        {
            GensAnimation.KeyframeSet keyframes = new GensAnimation.KeyframeSet();

            keyframes.Flag1 = (byte)id;
            keyframes.Flag2 = 0;
            keyframes.Flag3 = 0;
            keyframes.Flag4 = 0;

            // pls i need a better way to do this
            if (colorid == 0)
            {
                for (int i = 0; i < set.ColorCount(0); ++i)
                {
                    var value = set.GetColor(i, 0).R * flags.cFactor;

                    // Remove unnecessary keyframes
                    if (i > 0 && i < set.ColorCount(0) - 1)
                    {
                        var prevValue = set.GetColor(i - 1, 0).R * flags.cFactor;
                        var nextValue = set.GetColor(i + 1, 0).R * flags.cFactor;
                        if (prevValue == value && value == nextValue)
                            continue;
                    }

                    keyframes.Add(ConvertKeyframe((uint)i, value));
                }
            }
            else if (colorid == 1)
            {
                for (int i = 0; i < set.ColorCount(0); ++i)
                {
                    var value = set.GetColor(i, 0).R * flags.cFactor;

                    // Remove unnecessary keyframes
                    if (i > 0 && i < set.ColorCount(0) - 1)
                    {
                        var prevValue = set.GetColor(i - 1, 0).G * flags.cFactor;
                        var nextValue = set.GetColor(i + 1, 0).G * flags.cFactor;
                        if (prevValue == value && value == nextValue)
                            continue;
                    }

                    keyframes.Add(ConvertKeyframe((uint)i, value));
                }
            }
            else if (colorid == 2)
            {
                for (int i = 0; i < set.ColorCount(0); ++i)
                {
                    var value = set.GetColor(i, 0).B * flags.cFactor;

                    // Remove unnecessary keyframes
                    if (i > 0 && i < set.ColorCount(0) - 1)
                    {
                        var prevValue = set.GetColor(i - 1, 0).B * flags.cFactor;
                        var nextValue = set.GetColor(i + 1, 0).B * flags.cFactor;
                        if (prevValue == value && value == nextValue)
                            continue;
                    }

                    keyframes.Add(ConvertKeyframe((uint)i, value));
                }
            }
            else if (colorid == 3)
            {
                for (int i = 0; i < set.ColorCount(0); ++i)
                {
                    var value = set.GetColor(i, 0).A * flags.cFactor;

                    // Remove unnecessary keyframes
                    if (i > 0 && i < set.ColorCount(0) - 1)
                    {
                        var prevValue = set.GetColor(i - 1, 0).A * flags.cFactor;
                        var nextValue = set.GetColor(i + 1, 0).A * flags.cFactor;
                        if (prevValue == value && value == nextValue)
                            continue;
                    }

                    keyframes.Add(ConvertKeyframe((uint)i, value));
                }
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
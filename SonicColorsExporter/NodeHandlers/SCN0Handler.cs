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
    internal class SCN0Handler : AnimNodeHandler
    {
        public static void processSCN0(SCN0Node scn0, string outpath, SettingsFlags flags)
        {
            foreach (SCN0GroupNode group in scn0.Children)
            {
                if (group.Name.Contains("Camera"))
                {
                    foreach (SCN0CameraNode node in group.Children)
                    {
                        string outfile;

                        CameraAnimation animation = ConvertCamera(node, flags);

                        if (flags.AnimsXML)
                        {
                            outfile = outpath + "\\" + scn0.Name + "-" + node.Name + ".cam-anim.xml";
                            animation.ExportXML(outfile);
                        }
                        else
                        {
                            outfile = outpath + "\\" + scn0.Name + "-" + node.Name + ".cam-anim";
                            animation.Save(outfile);
                        }
                    }
                }
                if (group.Name.Contains("Lights"))
                {
                    /// WIP
                    //foreach (SCN0LightNode node in group.Children)
                    //{
                    //    string outfile;

                    //    LightAnimation animation = ConvertLight(node, flags);

                    //    if (flags.AnimsXML)
                    //    {
                    //        outfile = outpath + "\\" + scn0.Name + "-" + node.Name + ".lit-anim.xml";
                    //        animation.ExportXML(outfile);
                    //    }
                    //    else
                    //    {
                    //        outfile = outpath + "\\" + scn0.Name + "-" + node.Name + ".lit-anim";
                    //        animation.Save(outfile);
                    //    }
                    //}
                }
            }
        }

        public static CameraAnimation ConvertCamera(SCN0CameraNode node, SettingsFlags flags)
        {
            CameraAnimation camera = new CameraAnimation();

            camera.Animations.Add(ConvertAnim(node, flags));

            return camera;
        }

        public static GensAnimation.Animation ConvertAnim(SCN0CameraNode node, SettingsFlags flags)
        {
            SCN0Handler sCN0Handler = new SCN0Handler();
            GensAnimation.Animation anim = new GensAnimation.Animation();

            anim.Name = node.Name;
            anim.FPS = 60f;
            anim.StartTime = 0;
            anim.EndTime = node.FrameCount - 1;

            anim.Flag1 = 1;
            anim.Flag2 = 0;
            anim.Flag3 = 0;
            anim.Flag4 = 0;

            anim.Position = new Vector3(node.PosX.GetFrameValue(0) * flags.mFactor,
                node.PosY.GetFrameValue(0) * flags.mFactor,
                node.PosZ.GetFrameValue(0) * flags.mFactor);
            anim.Rotation = new Vector3(node.RotX.GetFrameValue(0) * flags.mFactor,
                node.RotY.GetFrameValue(0) * flags.mFactor,
                node.RotZ.GetFrameValue(0) * flags.mFactor);
            anim.Aim = new Vector3(node.AimX.GetFrameValue(0) * flags.mFactor,
                node.AimY.GetFrameValue(0) * flags.mFactor,
                node.AimZ.GetFrameValue(0) * flags.mFactor);
            anim.Twist = node.Twist.GetFrameValue(0);
            anim.NearZ = node.NearZ.GetFrameValue(0);
            anim.FarZ = node.FarZ.GetFrameValue(0);
            anim.FOV = GetFOV(node.FovY.GetFrameValue(0));
            anim.Aspect = node.Aspect.GetFrameValue(0);

            int id = 0;
            foreach (var set in node.KeyArrays)
            {
                var keyframes = sCN0Handler.ConvertKeyframeSet(set, id, flags);
                if (keyframes != null)
                    anim.KeyframeSets.Add(keyframes);
                id++;
            }

            return anim;
        }

        public override GensAnimation.KeyframeSet ConvertKeyframeSet(BrawlLib.Wii.Animations.KeyframeArray set, int id, SettingsFlags flags)
        {
            GensAnimation.KeyframeSet keyframes = new GensAnimation.KeyframeSet();

            int targetid;

            switch (id)
            {
                case 0:
                case 1:
                case 2:
                    targetid = id;
                    break;
                case 3:
                    targetid = 13;
                    break;
                case 4:
                    targetid = 10;
                    break;
                case 5:
                    targetid = 11;
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                    targetid = id - 3;
                    break;
                case 12:
                    targetid = 9;
                    break;
                case 13:
                    targetid = 12;
                    break;
                default:
                    return null;
            }

            keyframes.Flag1 = (byte)targetid;
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

                if (new[] { 0, 1, 2, 6, 7, 8 }.Contains(targetid))
                    value = value * flags.mFactor;
                else if (targetid == 12)
                    value = GetFOV(value);

                keyframes.Add(ConvertKeyframe(i, value));
            }

            // Remove sets with no animation data
            if (keyframes.Count < 2)
                return null;
            if (keyframes.Count == 2)
                if (keyframes[0].Value == keyframes[1].Value)
                    return null;

            return keyframes;
        }

        private static float GetFOV(float input, float aspect = 1.777778f)
        {
            float output;
            output = (float)((Math.PI / 180) * input) * aspect;

            return output;
        }
    }
}
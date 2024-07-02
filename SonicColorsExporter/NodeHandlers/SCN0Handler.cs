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
using System.Text.Json;

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

                        bool writeAcornScn0 = true;

                        if (writeAcornScn0)
                        {
                            outfile = outpath + "\\" + scn0.Name + ".acorn_scn0";
                            ExportAcornScn0(node, flags, outfile);
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
            CameraAnimation animation = new CameraAnimation();

            animation.Header.RootNodeType = 2;

            bool splitCameraShots = true;

            if (splitCameraShots)
            {
                List<BrawlLib.Wii.Animations.KeyframeEntry> KeyframeList = new List<BrawlLib.Wii.Animations.KeyframeEntry>();
                List<int> camStartTimes = new List<int>();

                // Detect camera shots based on FOV keyframes
                var FOVFrames = node.KeyArrays[13];

                //bool loopExit = false;
                var curKeyFrame = FOVFrames.GetKeyframe(0);
                while (curKeyFrame._index >= 0)
                {
                    KeyframeList.Add(curKeyFrame);

                    if (curKeyFrame._next._index < 0)
                        break;

                    curKeyFrame = curKeyFrame._next;
                }

                for (int i = 0; i < KeyframeList.Count; i++)
                {
                    // Add total start and end times
                    if (i == 0 || i == KeyframeList.Count - 1)
                    {
                        camStartTimes.Add(KeyframeList[i]._index);
                        continue;
                    }

                    if (KeyframeList[i]._index == (KeyframeList[i-1]._index + 2) && !(KeyframeList[i]._index == (KeyframeList[i - 2]._index + 4)))
                    {
                        camStartTimes.Add(KeyframeList[i]._index);
                        continue;
                    }
                }

                for (int i = 0; i < camStartTimes.Count - 1; i++)
                {
                    bool lastCamera = false;
                    if (i == camStartTimes.Count - 2)
                        lastCamera = true;

                    animation.Animations.Add(ConvertAnim(node, flags, i+1, lastCamera, camStartTimes[i], camStartTimes[i+1]));
                }
            }
            else
            {
                animation.Animations.Add(ConvertAnim(node, flags, 0, true, 0, node.FrameCount - 1));
            }

            return animation;
        }

        public static GensAnimation.Animation ConvertAnim(SCN0CameraNode node, SettingsFlags flags, int camID = 0, bool lastCamera = true, int startTime = 0, int endTime = 0)
        {
            SCN0Handler sCN0Handler = new SCN0Handler();
            GensAnimation.Animation anim = new GensAnimation.Animation();

            anim.Name = node.Name; // Default name if not splitting takes
            if (camID > 0)
            {
                anim.Name = "Camera_" + camID.ToString("00");
            }
            anim.FPS = 60f;
            anim.StartTime = startTime;
            anim.EndTime = endTime;

            if (node.Type == BrawlLib.SSBB.Types.SCN0CameraType.Aim)
                anim.Flag1 = 1;
            else
                anim.Flag1 = 0;
            anim.Flag2 = 0;
            anim.Flag3 = 0;
            anim.Flag4 = 0;

            anim.Position = new Vector3(node.PosX.GetFrameValue(startTime) * flags.mFactor,
                node.PosY.GetFrameValue(startTime) * flags.mFactor,
                node.PosZ.GetFrameValue(startTime) * flags.mFactor);
            anim.Rotation = new Vector3(node.RotX.GetFrameValue(startTime) * (float)(Math.PI / 180),
                node.RotY.GetFrameValue(startTime) * (float)(Math.PI / 180),
                node.RotZ.GetFrameValue(startTime) * (float)(Math.PI / 180));
            anim.Aim = new Vector3(node.AimX.GetFrameValue(startTime) * flags.mFactor,
                node.AimY.GetFrameValue(startTime) * flags.mFactor,
                node.AimZ.GetFrameValue(startTime) * flags.mFactor);
            anim.Twist = node.Twist.GetFrameValue(startTime) * (float)(Math.PI / 180);
            anim.NearZ = node.NearZ.GetFrameValue(startTime);
            anim.FarZ = node.FarZ.GetFrameValue(startTime);
            anim.FOV = GetFOV(node.FovY.GetFrameValue(startTime), node.Aspect.GetFrameValue(startTime));
            anim.Aspect = node.Aspect.GetFrameValue(startTime);

            int id = 0;
            foreach (var set in node.KeyArrays)
            {
                var keyframes = sCN0Handler.ConvertKeyframeSet(set, id, flags, startTime, endTime);
                if (keyframes != null)
                {
                    if (!lastCamera)
                    {
                        // fix interpolation bug
                        if (keyframes[keyframes.Count - 1].Index == endTime &&
                            keyframes[keyframes.Count - 2].Index == endTime-1 &&
                            keyframes[keyframes.Count - 3].Index == endTime-2)
                        {
                            keyframes.RemoveAt(keyframes.Count - 1);
                            keyframes.RemoveAt(keyframes.Count - 1);

                            keyframes.Add(new GensAnimation.Keyframe());
                            keyframes[keyframes.Count - 1].Index = endTime;
                            keyframes[keyframes.Count - 1].Value = keyframes[keyframes.Count - 2].Value;

                            // Remove unnecessary animation data
                            if (keyframes.Count == 3)
                            {
                                if (!(keyframes[0].Value == keyframes[1].Value && keyframes[1].Value == keyframes[2].Value))
                                {
                                    anim.KeyframeSets.Add(keyframes);
                                }
                            }
                            else
                                anim.KeyframeSets.Add(keyframes);
                        }
                    }
                    else
                        anim.KeyframeSets.Add(keyframes);
                }
                id++;
            }

            return anim;
        }

        public override GensAnimation.KeyframeSet ConvertKeyframeSet(BrawlLib.Wii.Animations.KeyframeArray set, int id, SettingsFlags flags, int startTime = 0, int endTime = 0)
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

            for (uint i = (uint)startTime; i <= endTime; ++i)
            {
                var value = set.GetFrameValue(i);

                // Remove unnecessary keyframes
                if (i > startTime && i < endTime)
                {
                    var prevValue = set.GetFrameValue(i - 1);
                    var nextValue = set.GetFrameValue(i + 1);
                    if (prevValue == value && value == nextValue)
                        continue;
                }

                if (new[] { 0, 1, 2, 6, 7, 8 }.Contains(targetid))
                    value = value * flags.mFactor;
                else if (new[] { 3, 4, 5, 9 }.Contains(targetid))
                    value = value * (float)(Math.PI / 180);
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

        private static void ExportAcornScn0(SCN0CameraNode node, SettingsFlags flags, string outpath)
        {
            if (node == null) return;

            bool isCompress = true;

            var options = new JsonWriterOptions
            {
                Indented = true
            };

            var fileStream = File.Create(outpath);

            var writer = new Utf8JsonWriter(fileStream, options);
            
            writer.WriteStartObject();
            writer.WriteNumber("FrameCount", node.FrameCount - 1);
            writer.WriteBoolean("Loop", true);

            writer.WriteStartArray("CameraFrames");

            writer.WriteStartObject();
            writer.WriteString("Name", node.Name);
            writer.WriteString("Type", node.Type.ToString());
            writer.WriteString("ProjectionType", node.ProjectionType.ToString());

            writer.WriteStartArray("Frames");
            for (uint i = 0; i <= node.FrameCount - 1; ++i)
            {
                if (isCompress)
                {
                    if (i > 0 && i < node.FrameCount - 1)
                    {
                        bool isSkipThisFrame = true;
                        foreach (var set in node.KeyArrays)
                        {
                            if (set.GetFrameValue(i) != set.GetFrameValue(i-1) || set.GetFrameValue(i) != set.GetFrameValue(i + 1))
                            {
                                isSkipThisFrame = false;
                                break;
                            }
                        }

                        if (isSkipThisFrame)
                            continue;
                    }
                }

                writer.WriteStartObject();
                writer.WriteNumber("Aspect", node.Aspect.GetFrameValue(i));
                writer.WriteNumber("NearZ", node.NearZ.GetFrameValue(i));
                writer.WriteNumber("FarZ", node.FarZ.GetFrameValue(i));
                writer.WriteNumber("Twist", node.Twist.GetFrameValue(i));
                writer.WriteNumber("FovY", node.FovY.GetFrameValue(i));
                writer.WriteNumber("Height", node.Height.GetFrameValue(i));
                //writer.WriteNumber("PosXSlope", 0);
                //writer.WriteNumber("PosYSlope", 0);
                //writer.WriteNumber("PosZSlope", 0);
                //writer.WriteNumber("AspectSlope", 0);
                //writer.WriteNumber("NearZSlope", 0);
                //writer.WriteNumber("FarZSlope", 0);
                //writer.WriteNumber("RotXSlope", 0);
                //writer.WriteNumber("RotYSlope", 0);
                //writer.WriteNumber("RotZSlope", 0);
                //writer.WriteNumber("AimXSlope", 0);
                //writer.WriteNumber("AimYSlope", 0);
                //writer.WriteNumber("AimZSlope", 0);
                //writer.WriteNumber("TwistSlope", 0);
                //writer.WriteNumber("FovYSlope", 0);
                //writer.WriteNumber("HeightSlope", 0);
                writer.WriteNumber("Flags", 0);
                writer.WriteNumber("FrameNum", i);

                writer.WriteStartObject("Position");
                writer.WriteNumber("X", node.PosX.GetFrameValue(i));
                writer.WriteNumber("Y", node.PosY.GetFrameValue(i));
                writer.WriteNumber("Z", node.PosZ.GetFrameValue(i));
                writer.WriteEndObject();

                writer.WriteStartObject("Rotation");
                writer.WriteNumber("X", node.RotX.GetFrameValue(i));
                writer.WriteNumber("Y", node.RotY.GetFrameValue(i));
                writer.WriteNumber("Z", node.RotZ.GetFrameValue(i));
                writer.WriteEndObject();

                writer.WriteStartObject("Aim");
                writer.WriteNumber("X", node.AimX.GetFrameValue(i));
                writer.WriteNumber("Y", node.AimY.GetFrameValue(i));
                writer.WriteNumber("Z", node.AimZ.GetFrameValue(i));
                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();

            writer.WriteEndArray();

            writer.WriteEndObject();
            writer.Flush();
            
            fileStream.Close();
        }
    }
}
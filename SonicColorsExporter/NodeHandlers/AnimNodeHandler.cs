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
    internal class AnimNodeHandler
    {
        public virtual GensAnimation.KeyframeSet ConvertKeyframeSet(BrawlLib.Wii.Animations.KeyframeArray set, int id, SettingsFlags flags)
        {
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

        public virtual GensAnimation.Keyframe ConvertKeyframe(uint index, float value)
        {
            GensAnimation.Keyframe keyframe = new GensAnimation.Keyframe();

            keyframe.Index = (float)index;
            keyframe.Value = value;

            return keyframe;
        }
    }
}

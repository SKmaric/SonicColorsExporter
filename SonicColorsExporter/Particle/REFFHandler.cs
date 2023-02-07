using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;
using HedgeLib.Headers;
using HedgeLib.Materials;
using HedgeLib.Textures;
using HedgeLib.IO;
using HedgeLib.Exceptions;

namespace SonicColorsExporter.Particle
{
    internal class REFFHandler
    {
        #region Particle Effects
        float cFactor;
        float mFactor;

        public static List<String> ReadEffectNames(ExtendedBinaryReader reader)
        {
            const string Signature = "\0EFF";

            var effNames = new List<String>();

            // SOBJ Header
            var sig = reader.ReadChars(4);
            if (!reader.IsBigEndian)
                Array.Reverse(sig);

            string sigString = new string(sig);
            if (sigString != Signature)
                throw new InvalidSignatureException(Signature, sigString);

            uint unknown1 = reader.ReadUInt32();
            uint effTypeCount = reader.ReadUInt32();
            uint unknown2 = reader.ReadUInt32();

            for (uint i = 0; i < effTypeCount; ++i)
            {
                reader.JumpAhead(4);
                effNames.Add(reader.GetString());
            }

            return effNames;
        }

        public void processREFF(REFFNode reff, string outpath, List<string> particleList, SettingsFlags flags)
        {
            cFactor = flags.cFactor;
            mFactor = flags.mFactor;
            foreach (REFFEntryNode node in reff.Children)
            {
                if (particleList.Contains(node.Name))
                {
                    string outfile = outpath + "\\" + node.Name + ".gte.xml";

                    writeGTEXML(node, outfile);
                }
            }
        }

        private void writeGTEXML(REFFEntryNode node, string outfile)
        {
            REFFEmitterNode9 emitter = node.Children[0] as REFFEmitterNode9;
            REFFParticleNode particle = node.Children[1] as REFFParticleNode;
            REFFAnimationListNode animationlist = node.Children[2] as REFFAnimationListNode;

            string[] childrenNames = ((node.Children[2] as REFFAnimationListNode).FindChildrenByName("Child")[0] as REFFAnimationNode).Names;
            int childrenCount = childrenNames.Length;

            REFFEntryNode[] effectChildren = new REFFEntryNode[childrenCount];

            for (int i = 0; i < childrenCount; i++)
            {
                effectChildren[i] = node.Parent.FindChildrenByName(childrenNames[i])[0] as REFFEntryNode;
            }


            XmlWriterSettings _writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (FileStream stream = new FileStream(outfile, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                0x1000, FileOptions.SequentialScan))
            {
                using (XmlWriter writer = XmlWriter.Create(stream, _writerSettings))
                {
                    writer.Flush();
                    stream.Position = 0;

                    writer.WriteStartDocument();
                    writer.WriteStartElement("Effect");
                    writer.WriteAttributeString("Name", node.Name);

                    writer.WriteStartElement("StartTime");
                    writer.WriteAttributeString("Value", "0");
                    writer.WriteEndElement();

                    writer.WriteStartElement("LifeTime");
                    writer.WriteAttributeString("Value", "600");
                    writer.WriteEndElement();

                    writer.WriteStartElement("Color");
                    writer.WriteAttributeString("R", "1");
                    writer.WriteAttributeString("G", "1");
                    writer.WriteAttributeString("B", "1");
                    writer.WriteAttributeString("A", "1");
                    writer.WriteEndElement();

                    writer.WriteStartElement("Translation");
                    writer.WriteAttributeString("X", "0");
                    writer.WriteAttributeString("Y", "0");
                    writer.WriteAttributeString("Z", "0");
                    writer.WriteEndElement();

                    writer.WriteStartElement("Rotation");
                    writer.WriteAttributeString("X", "0");
                    writer.WriteAttributeString("Y", "0");
                    writer.WriteAttributeString("Z", "0");
                    writer.WriteEndElement();

                    writer.WriteStartElement("Flags");
                    writer.WriteAttributeString("Value", "1"); //Loop
                    writer.WriteEndElement();

                    writeEmitter(emitter, particle, writer, 0);
                    int childNumber = 1;
                    foreach (REFFEntryNode child in effectChildren)
                    {
                        writeEmitter(child.Children[0] as REFFEmitterNode9, child.Children[1] as REFFParticleNode, writer, childNumber);
                        childNumber++;
                    }

                    writeParticle(particle, emitter, writer, 0);
                    childNumber = 1;
                    foreach (REFFEntryNode child in effectChildren)
                    {
                        writeParticle(child.Children[1] as REFFParticleNode, child.Children[0] as REFFEmitterNode9, writer, childNumber);
                        childNumber++;
                    }

                    writer.WriteEndElement(); //Effect
                    writer.WriteEndDocument();
                }
            }
        }

        private void writeEmitter(REFFEmitterNode9 emitter, REFFParticleNode particle, XmlWriter writer, int id)
        {
            writer.WriteStartElement("Emitter");
            writer.WriteAttributeString("Id", id.ToString());
            writer.WriteAttributeString("Name", emitter.Parent.Name);
            writer.WriteAttributeString("Type", "Box");

            {
                writer.WriteStartElement("StartTime");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("LifeTime");
                writer.WriteAttributeString("Value", "180");
                writer.WriteEndElement();

                writer.WriteStartElement("LoopStartTime");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("LoopEndTime");
                writer.WriteAttributeString("Value", "-1");
                writer.WriteEndElement();

                writer.WriteStartElement("Translation");
                writer.WriteAttributeString("X", (emitter.Translate._x * mFactor).ToString());
                writer.WriteAttributeString("Y", (emitter.Translate._y * mFactor).ToString());
                writer.WriteAttributeString("Z", (emitter.Translate._z * mFactor).ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("Rotation");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("RotationAdd");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("RotationAddRandom");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("Scaling");
                writer.WriteAttributeString("X", "1");
                writer.WriteAttributeString("Y", "1");
                writer.WriteAttributeString("Z", "1");
                writer.WriteEndElement();

                writer.WriteStartElement("EmitCondition");
                writer.WriteAttributeString("Value", "Time");
                writer.WriteEndElement();

                writer.WriteStartElement("DirectionType");
                writer.WriteAttributeString("Value", "Billboard");
                writer.WriteEndElement();

                writer.WriteStartElement("EmissionInterval");
                writer.WriteAttributeString("Value", "5");
                writer.WriteEndElement();

                writer.WriteStartElement("ParticlePerEmission");
                writer.WriteAttributeString("Value", "1");
                writer.WriteEndElement();

                writer.WriteStartElement("EmissionDirectionType");
                writer.WriteAttributeString("Value", "Outward");
                writer.WriteEndElement();

                writer.WriteStartElement("Size");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("Flags");
                writer.WriteAttributeString("Value", "1"); //Loop
                writer.WriteEndElement();

                writer.WriteStartElement("Particle");
                writer.WriteAttributeString("Id", id.ToString());
                writer.WriteAttributeString("Name", emitter.Parent.Name);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private void writeParticle(REFFParticleNode particle, REFFEmitterNode9 emitter, XmlWriter writer, int id)
        {
            writer.WriteStartElement("Particle");
            writer.WriteAttributeString("Id", id.ToString());
            writer.WriteAttributeString("Name", particle.Parent.Name);
            writer.WriteAttributeString("Type", "Quad");

            {
                writer.WriteStartElement("LifeTime");
                writer.WriteAttributeString("Value", emitter.PtclLife.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("PivotPosition");
                writer.WriteAttributeString("Value", "MiddleCenter");
                writer.WriteEndElement();

                writer.WriteStartElement("DirectionType");
                writer.WriteAttributeString("Value", "Billboard");
                writer.WriteEndElement();

                writer.WriteStartElement("ZOffset");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("Size");
                writer.WriteAttributeString("X", (particle.Size.X * mFactor).ToString());
                writer.WriteAttributeString("Y", (particle.Size.Y * mFactor).ToString());
                writer.WriteAttributeString("Z", "1");
                writer.WriteEndElement();

                writer.WriteStartElement("SizeRandom");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("Rotation");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("RotationRandom");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("RotationAdd");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("RotationAddRandom");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("Direction");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("DirectionRandom");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("Speed");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("SpeedRandom");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("GravitationalAccel");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ExternalAccel");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ExternalAccelRandom");
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("Deceleration");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("DecelerationRandom");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ReflectionCoeff");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ReflectionCoeffRandom");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ReboundPlaneY");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("MaxCount");
                writer.WriteAttributeString("Value", "7");
                writer.WriteEndElement();

                writer.WriteStartElement("Color");
                writer.WriteAttributeString("R", (particle.Color1Primary.R * cFactor).ToString());
                writer.WriteAttributeString("G", (particle.Color1Primary.G * cFactor).ToString());
                writer.WriteAttributeString("B", (particle.Color1Primary.B * cFactor).ToString());
                writer.WriteAttributeString("A", (particle.Color1Primary.A * cFactor).ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("TextureIndex");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("UvIndexType");
                writer.WriteAttributeString("Value", "Fixed");
                writer.WriteEndElement();

                writer.WriteStartElement("UvIndex");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("UvChangeInterval");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ColorScroll");
                writer.WriteAttributeString("U", "0");
                writer.WriteAttributeString("V", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ColorScrollRandom");
                writer.WriteAttributeString("U", "0");
                writer.WriteAttributeString("V", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("ColorScrollSpeed");
                writer.WriteAttributeString("Value", "1");
                writer.WriteEndElement();

                writer.WriteStartElement("Material");
                writer.WriteAttributeString("Value", particle.Texture1Name);
                writer.WriteEndElement();

                writer.WriteStartElement("Flags");
                writer.WriteAttributeString("Value", "84");
                writer.WriteEndElement();

                foreach (REFFAnimationNode anim in particle.Parent.Children[2].Children)
                {
                    writeAnimation(anim, writer, anim.KindType.ToString());
                }
            }

            writer.WriteEndElement();
        }

        private void writeAnimation(REFFAnimationNode animation, XmlWriter writer, string type)
        {
            writer.WriteStartElement("Animation");
            writer.WriteAttributeString("Type", type);//To change to correct names

            writer.WriteStartElement("StartTime");
            writer.WriteAttributeString("Value", "0");
            writer.WriteEndElement();

            writer.WriteStartElement("EndTime");
            writer.WriteAttributeString("Value", animation.FrameCount.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("RepeatType");
            writer.WriteAttributeString("Value", "Constant");
            writer.WriteEndElement();

            writer.WriteStartElement("RandomFlags");
            writer.WriteAttributeString("Value", "0");
            writer.WriteEndElement();

            //Placeholder blank key tag
            writer.WriteStartElement("Key");
            writer.WriteAttributeString("Time", "0");
            writer.WriteAttributeString("Value", "0");
            {
                writer.WriteStartElement("InterpolationType");
                writer.WriteAttributeString("Value", "Linear");
                writer.WriteEndElement();

                writer.WriteStartElement("InParam");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();

                writer.WriteStartElement("OutParam");
                writer.WriteAttributeString("Value", "0");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
        #endregion
    }
}

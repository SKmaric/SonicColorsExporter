using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BrawlLib.SSBB.ResourceNodes;
using HedgeLib.Headers;
using HedgeLib.IO;
using HedgeLib.Exceptions;

namespace SonicColorsExporter.Gismo
{
    internal class GISMHandler
    {
        #region Gismo
        public static List<CLRGism> ReadGismoList(ExtendedBinaryReader reader)
        {
            const string Signature = "GISM";

            var gismoList = new List<CLRGism>();

            // SOBJ Header
            var sig = reader.ReadChars(4);
            if (!reader.IsBigEndian)
                Array.Reverse(sig);

            string sigString = new string(sig);
            if (sigString != Signature)
                throw new InvalidSignatureException(Signature, sigString);

            uint unknown1 = reader.ReadUInt32();
            uint gismoCount = reader.ReadUInt32();
            uint unknown2 = reader.ReadUInt32();

            for (int i = 0; i < gismoCount; ++i)
            {
                var currGism = new CLRGism();

                currGism.unknown1 = reader.ReadUInt32();
                currGism.unknown2 = reader.ReadUInt32();
                currGism.unknownByte1 = reader.ReadByte();
                reader.JumpAhead(2);
                currGism.unknownBool1 = reader.ReadBoolean();
                reader.JumpAhead(1);
                currGism.brkCount = reader.ReadUInt16();
                currGism.unknownBool2 = reader.ReadBoolean();
                currGism.unknown5 = reader.ReadUInt32();
                currGism.unknown6 = reader.ReadUInt32();
                currGism.unknown7 = reader.ReadUInt32();
                currGism.unknown8 = reader.ReadSingle();
                currGism.unknown9 = reader.ReadSingle();
                currGism.unknown10 = reader.ReadSingle();
                currGism.unknown11 = reader.ReadSingle();
                currGism.colSizeX = reader.ReadSingle();
                currGism.colSizeY = reader.ReadSingle();
                currGism.colSizeZ = reader.ReadSingle();
                currGism.unknown15 = reader.ReadUInt32();

                currGism.name = reader.GetString();
                currGism.effName = reader.GetString();
                currGism.soundCue = reader.GetString();

                gismoList.Add(currGism);
            }

            return gismoList;
        }

        private void writeGismObjectProd(List<CLRGism> gismoList, string outfile)
        {
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
                    writer.WriteStartElement("ObjectPhysics");

                    foreach (CLRGism gismo in gismoList)
                    {
                        writer.WriteStartElement(gismo.name);
                        {
                            writer.WriteElementString("DataPath", "object\\physics\\" + gismo.name + "_brk");
                            writer.WriteElementString("Model", gismo.name);
                            writer.WriteElementString("RigidBodyContainer", gismo.name);
                            writer.WriteElementString("RigidBody", gismo.name);
                            writer.WriteElementString("Type", "Fixed");
                            writer.WriteElementString("DamageType", "Contact");
                            writer.WriteElementString("IsVisible", "true");
                            writer.WriteElementString("HP", "1");
                            writer.WriteElementString("SurvivalTime", "4");
                            writer.WriteElementString("SurvivalRange", "1");
                            writer.WriteElementString("NextObjName", gismo.name + "_brk");
                            writer.WriteElementString("IsHoming", "false");
                            writer.WriteElementString("IsRepeat", "false");
                            writer.WriteElementString("Motion", null);
                            writer.WriteElementString("MotionSkeleton", null);
                            writer.WriteElementString("AttackPoint", "0");
                            writer.WriteElementString("Category", "Common");
                            writer.WriteElementString("DamageCollision", null);
                            writer.WriteElementString("WeightType", "Middle");
                            writer.WriteElementString("LightFieldType", "OffsetDisable");
                            writer.WriteElementString("SoundID", gismo.soundCue);
                            writer.WriteElementString("Effect", gismo.effName);
                            writer.WriteElementString("EffectHit", "false");
                            writer.WriteElementString("IsPlayerBound", "false");
                            writer.WriteElementString("FixedNode", null);
                            writer.WriteElementString("CollisionFlag", "true");
                        }
                        writer.WriteEndElement();//gismo name

                        writer.WriteStartElement(gismo.name + "_brk");
                        {
                            writer.WriteElementString("DataPath", "object\\physics\\" + gismo.name + "_brk");
                            for (int i = 1; i <= gismo.brkCount; i++)
                            {
                                writer.WriteElementString("Model", gismo.name + "_bp" + i.ToString());
                            }
                            writer.WriteElementString("RigidBodyContainer", gismo.name + "_brk");
                            for (int i = 1; i <= gismo.brkCount; i++)
                            {
                                writer.WriteElementString("RigidBody", gismo.name + "_bp" + i.ToString());
                            }
                            writer.WriteElementString("Type", "Debris");
                            writer.WriteElementString("DamageType", "Normal");
                            writer.WriteElementString("IsVisible", "false");
                            writer.WriteElementString("HP", "0");
                            writer.WriteElementString("SurvivalTime", "0");
                            writer.WriteElementString("SurvivalRange", "1");
                            writer.WriteElementString("NextObjName", null);
                            writer.WriteElementString("IsHoming", "false");
                            writer.WriteElementString("IsRepeat", "false");
                            writer.WriteElementString("Motion", null);
                            writer.WriteElementString("MotionSkeleton", null);
                            writer.WriteElementString("AttackPoint", "0");
                            writer.WriteElementString("Category", "Common");
                            writer.WriteElementString("DamageCollision", null);
                            writer.WriteElementString("WeightType", "Middle");
                            writer.WriteElementString("LightFieldType", "OffsetDisable");
                            writer.WriteElementString("SoundID", null);
                            writer.WriteElementString("Effect", null);
                            writer.WriteElementString("EffectHit", "false");
                            writer.WriteElementString("IsPlayerBound", "false");
                            writer.WriteElementString("FixedNode", null);
                            writer.WriteElementString("CollisionFlag", "true");
                            writer.WriteElementString("UnitNameBase", gismo.name + "_bp");
                            writer.WriteElementString("UnitNameCount", gismo.brkCount.ToString());
                            writer.WriteElementString("UnitNameCountPlace", "1");
                        }
                        writer.WriteEndElement();//gismo name
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }

        public class CLRGism
        {
            public string name, effName, soundCue;
            public uint brkCount;

            public uint unknown1, unknown2, unknown5, unknown6, unknown7, unknown15;
            public float unknown8, unknown9, unknown10, unknown11;
            public bool unknownBool1, unknownBool2;
            public byte unknownByte1;

            public float colSizeX, colSizeY, colSizeZ;
        }
        #endregion
    }
}

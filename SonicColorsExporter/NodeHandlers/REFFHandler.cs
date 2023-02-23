using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.SSBB.Types;
using BrawlLib.Internal;
using HedgeLib.Headers;
using HedgeLib.Materials;
using HedgeLib.Textures;
using HedgeLib.IO;
using HedgeLib.Exceptions;

namespace SonicColorsExporter
{
    internal class REFFHandler
    {
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

        public static void processREFF(REFFNode reff, string outpath, List<string> particleList, SettingsFlags flags)
        {
            // DEBUG - work without particlelist
            if (particleList.Count < 1)
            {
                particleList = new List<string>();
                var childrenList = new List<string>();

                foreach (REFFEntryNode node in reff.Children)
                {
                    particleList.Add(node.Name);
                    var childNodes = (node.Children[2] as REFFAnimationListNode).FindChildrenByName("Child");

                    if (childNodes.Length > 0)
                    {
                        var childNode = childNodes[0] as REFFAnimationNode;
                        string[] childrenNames = childNode.Names;
                        for (int i = 0; i < childrenNames.Length; ++i)
                            childrenList.Add(childrenNames[i]);
                    }
                }
                particleList = particleList.Except(childrenList).ToList();
            }

            foreach (REFFEntryNode node in reff.Children)
            {
                if (particleList.Contains(node.Name))
                {
                    string outfile = outpath + "\\" + node.Name + ".gte.xml";

                    ExportXML(node, outfile, flags);
                }
            }

            // DEBUG - output AnimCurve data to xml
            bool debugAnim = true;

            if (debugAnim)
            {
                using (var fileStream = File.Create(outpath + "\\" + reff.Name + "_effanims.xml"))
                {
                    var animroot = new XElement("Animations");

                    WriteDebugAnimXML(reff, animroot, flags);

                    var animdoc = new XDocument(animroot);
                    animdoc.Save(fileStream);
                }
            }
        }

        private static void ExportXML(REFFEntryNode node, string outfile, SettingsFlags flags)
        {
            using (var fileStream = File.Create(outfile))
            {
                var root = new XElement("Effect", new XAttribute(
                "Name", node.Name));

                WriteXML(node, root, flags);

                var doc = new XDocument(root);
                doc.Save(fileStream);
            }
        }

        private static void WriteXML(REFFEntryNode node, XElement root, SettingsFlags flags)
        {
            REFFEmitterNode9 parentEmitter = node.Children[0] as REFFEmitterNode9;

            REFFEntryNode[] effectChildren = new REFFEntryNode[0];

            string[] childrenNames;
            int childrenCount = 0;
            var childNodes = (node.Children[2] as REFFAnimationListNode).FindChildrenByName("Child");
            if (childNodes.Length > 0)
            {
                var childNode = childNodes[0] as REFFAnimationNode;
                childrenNames = childNode.Names;
                childrenCount = childrenNames.Length;

                effectChildren = new REFFEntryNode[childrenCount];

                for (int i = 0; i < childrenCount; i++)
                {
                    effectChildren[i] = node.Parent.FindChildrenByName(childrenNames[i])[0] as REFFEntryNode;
                }
            }

            root.Add(new XElement("StartTime", new XAttribute("Value", 0)),
                    new XElement("LifeTime", new XAttribute("Value", (parentEmitter.EmitLife == 0xFFFF) ? 60 : parentEmitter.EmitLife)),
                    new XElement("Color",
                        new XAttribute("R", 1),
                        new XAttribute("G", 1),
                        new XAttribute("B", 1),
                        new XAttribute("A", 1)),
                    new XElement("Translation",
                        new XAttribute("X", 0),
                        new XAttribute("Y", 0),
                        new XAttribute("Z", 0)),
                    new XElement("Rotation",
                        new XAttribute("X", 0),
                        new XAttribute("Y", 0),
                        new XAttribute("Z", 0)),
                    new XElement("Flags", new XAttribute("Value", (parentEmitter.EmitLife == 0xFFFF) ? 1 : 0))
                    );

            root.Add(writeEmitter(node, 0, flags));

            int childNumber = 1;
            foreach (REFFEntryNode child in effectChildren)
            {
                root.Add(writeEmitter(child, childNumber, flags));
                childNumber++;
            }

            root.Add(writeParticle(node, 0, flags));

            childNumber = 1;
            foreach (REFFEntryNode child in effectChildren)
            {
                root.Add(writeParticle(child, childNumber, flags));
                childNumber++;
            }
        }

        [Flags]
        public enum EmitOption
        {
            LOD = 1,
            BillboardY = 128,
            Billboard = 256,
            FixedInterval = 512,
            FixedPos = 1024,
            Instance = 2048
        }

        [Flags]
        public enum EmitTypeOption
        {
            FixedDensity = 1,
            LinkedSize = 2
        }

        public enum EmitFormType
        {
            Disc = 0,
            Line = 1,
            Cube = 5,
            Cylinder = 7,
            Sphere = 8,
            Point = 9,
            Torus = 10
        }

        private static XElement writeEmitter(REFFEntryNode node, int id, SettingsFlags flags)
        {
            REFFEmitterNode9 emitter = node.Children[0] as REFFEmitterNode9;
            REFFParticleNode particle = node.Children[1] as REFFParticleNode;

            List<EffectAnim> animations = new List<EffectAnim>();
            foreach (REFFAnimationNode anim in node.Children[2].Children)
            {
                animations.Add(new EffectAnim(anim));
            }

            // Get Emitter flags
            EmitOption emitOption = (EmitOption)((emitter.EmitFlag >> (8 * 1)) & 0xFFFF);
            EmitFormType emitForm = (EmitFormType)(emitter.EmitFlag & 0xFF);
            EmitTypeOption emitTypeOption;
            if (emitForm != EmitFormType.Point)
                emitTypeOption = (EmitTypeOption)((emitter.EmitFlag >> (8 * 0)) & 0xFF);

            //emitOption.HasFlag(EmitTypeOption.LOD);

            var elem = new XElement("Emitter",
                new XAttribute("Id", id),
                new XAttribute("Name", node.Name),
                new XAttribute("Type", GetEmitFormGens(emitForm)));

            elem.Add(new XElement("StartTime", new XAttribute("Value", 0)),
                new XElement("LifeTime", new XAttribute("Value", (emitter.EmitLife == 0xFFFF) ? 60 : emitter.EmitLife)),
                new XElement("LoopStartTime", new XAttribute("Value", 0)),
                new XElement("LoopEndTime", new XAttribute("Value", -1)),
                new XElement("Translation",
                    new XAttribute("X", emitter.Translate._x * flags.mFactor),
                    new XAttribute("Y", emitter.Translate._y * flags.mFactor),
                    new XAttribute("Z", emitter.Translate._z * flags.mFactor)),
                new XElement("Rotation",
                    new XAttribute("X", (180 / Math.PI) * emitter.Rotate._x),
                    new XAttribute("Y", (180 / Math.PI) * emitter.Rotate._y),
                    new XAttribute("Z", (180 / Math.PI) * emitter.Rotate._z)),
                new XElement("RotationAdd",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("RotationAddRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("Scaling", // todo: other radius axis sizes
                    new XAttribute("X", emitter.Scale._x),
                    new XAttribute("Y", emitter.Scale._y),
                    new XAttribute("Z", emitter.Scale._z)),
                new XElement("EmitCondition", new XAttribute("Value", "Time")),
                new XElement("DirectionType", new XAttribute("Value", GetDirectionTypeGens(emitOption))),
                new XElement("EmissionInterval", new XAttribute("Value", emitter.EmitInterval)),
                new XElement("ParticlePerEmission", new XAttribute("Value", emitter.Emit))
            );

            // Shape specific properties
            switch (emitForm)
            {
                case EmitFormType.Disc:
                    elem.Add(new XElement("Radius", new XAttribute("Value", emitter.CommonParam1 * flags.mFactor)),
                        new XElement("Height", new XAttribute("Value", 0)),
                        new XElement("StartAngle", new XAttribute("Value", (180 / Math.PI) * emitter.CommonParam3)),
                        new XElement("EndAngle", new XAttribute("Value", (180 / Math.PI) * emitter.CommonParam4)),
                        new XElement("EmissionDirectionType", new XAttribute("Value", "Outward"))
                    );
                    break;
                case EmitFormType.Cylinder:
                case EmitFormType.Torus: // idk
                    elem.Add(new XElement("Radius", new XAttribute("Value", emitter.CommonParam1 * flags.mFactor)),
                        new XElement("Height", new XAttribute("Value", emitter.CommonParam5)),
                        new XElement("StartAngle", new XAttribute("Value", (180 / Math.PI) * emitter.CommonParam3)),
                        new XElement("EndAngle", new XAttribute("Value", (180 / Math.PI) * emitter.CommonParam4)),
                        new XElement("EmissionDirectionType", new XAttribute("Value", "Outward"))
                    );
                    break;
                case EmitFormType.Sphere:
                    elem.Add(new XElement("Radius", new XAttribute("Value", emitter.CommonParam1 * flags.mFactor)),
                        new XElement("Latitude", new XAttribute("Value", (180 / Math.PI) * emitter.CommonParam3)),
                        new XElement("Longitude", new XAttribute("Value", (180 / Math.PI) * emitter.CommonParam4)),
                        new XElement("EmissionDirectionType", new XAttribute("Value", "Outward"))
                    );
                    break;
                case EmitFormType.Cube:
                    elem.Add(new XElement("Size",
                        new XAttribute("X", emitter.CommonParam1 * flags.mFactor),
                        new XAttribute("Y", emitter.CommonParam2 * flags.mFactor),
                        new XAttribute("Z", emitter.CommonParam3 * flags.mFactor))
                    );
                    break;
                case EmitFormType.Line: // idk
                    elem.Add(new XElement("Size",
                        new XAttribute("X", emitter.CommonParam1 * flags.mFactor),
                        new XAttribute("Y", 0),
                        new XAttribute("Z", 0))
                    );
                    break;
                default: // Point
                    elem.Add(new XElement("Size",
                        new XAttribute("X", 0),
                        new XAttribute("Y", 0),
                        new XAttribute("Z", 0))
                    );
                    break;
            }

            elem.Add(new XElement("Flags", new XAttribute("Value", (emitter.EmitLife == 0xFFFF)?1:0)) // Loop
                );

            elem.Add(new XElement("Particle",
                new XAttribute("Id", id.ToString()),
                new XAttribute("Name", node.Name))
                );

            return elem;
        }

        private static string GetEmitFormGens(EmitFormType flags)
        {
            switch (flags)
            {
                case EmitFormType.Disc:
                case EmitFormType.Cylinder:
                case EmitFormType.Torus:
                    return "Cylinder";
                case EmitFormType.Sphere:
                    return "Sphere";
                default:
                    return "Box";
            }
        }

        private static string GetDirectionTypeGens(EmitOption flags)
        {
            switch (flags)
            {
                case EmitOption.Billboard:
                    return "Billboard";
                case EmitOption.BillboardY:
                    return "YRotationOnly";
                default:
                    return "ParentAxis";
            }
        }

        private static XElement writeParticle(REFFEntryNode node, int id, SettingsFlags flags)
        {
            REFFEmitterNode9 emitter = node.Children[0] as REFFEmitterNode9;
            REFFParticleNode particle = node.Children[1] as REFFParticleNode;
            REFFAnimationListNode animationlist = node.Children[2] as REFFAnimationListNode;

            var elem = new XElement("Particle",
                new XAttribute("Id", id),
                new XAttribute("Name", node.Name),
                new XAttribute("Type", "Quad"));

            elem.Add(new XElement("LifeTime", new XAttribute("Value", emitter.PtclLife)),
                new XElement("PivotPosition", new XAttribute("Value", "MiddleCenter")),
                new XElement("DirectionType", new XAttribute("Value", "Billboard")),
                new XElement("ZOffset", new XAttribute("Value", 0)),
                new XElement("Size",
                    new XAttribute("X", (particle.Size.X * flags.mFactor)),
                    new XAttribute("Y", (particle.Size.Y * flags.mFactor)),
                    new XAttribute("Z", 1)),
                new XElement("SizeRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("Rotation",
                    new XAttribute("X", (180 / Math.PI) * particle.Rotate._x),
                    new XAttribute("Y", (180 / Math.PI) * particle.Rotate._y),
                    new XAttribute("Z", (180 / Math.PI) * particle.Rotate._z)),
                new XElement("RotationRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("RotationAdd",
                    new XAttribute("X", (180 / Math.PI) * particle.RotateOffset._x),
                    new XAttribute("Y", (180 / Math.PI) * particle.RotateOffset._y),
                    new XAttribute("Z", (180 / Math.PI) * particle.RotateOffset._z)),
                new XElement("RotationAddRandom",
                    new XAttribute("X", (((180 / Math.PI) * particle.RotateOffset._x) * 0.01) * particle.RotateOffsetRandom1),
                    new XAttribute("Y", (((180 / Math.PI) * particle.RotateOffset._y) * 0.01) * particle.RotateOffsetRandom2),
                    new XAttribute("Z", (((180 / Math.PI) * particle.RotateOffset._z) * 0.01) * particle.RotateOffsetRandom3)),
                new XElement("Direction",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("DirectionRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("Speed", new XAttribute("Value", 0)),
                new XElement("SpeedRandom", new XAttribute("Value", 0)),
                new XElement("GravitationalAccel",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("ExternalAccel",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("ExternalAccelRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("Deceleration", new XAttribute("Value", 0)),
                new XElement("DecelerationRandom", new XAttribute("Value", 0)),
                new XElement("ReflectionCoeff", new XAttribute("Value", 0)),
                new XElement("ReflectionCoeffRandom", new XAttribute("Value", 0)),
                new XElement("ReboundPlaneY", new XAttribute("Value", 0)),
                new XElement("MaxCount", new XAttribute("Value", 7)),
                new XElement("Color",
                    new XAttribute("R", (particle.Color1Primary.R * flags.cFactor)),
                    new XAttribute("G", (particle.Color1Primary.G * flags.cFactor)),
                    new XAttribute("B", (particle.Color1Primary.B * flags.cFactor)),
                    new XAttribute("A", (particle.Color1Primary.A * flags.cFactor))),
                new XElement("TextureIndex", new XAttribute("Value", 0)),
                new XElement("UvIndexType", new XAttribute("Value", "Fixed")),
                new XElement("UvIndex", new XAttribute("Value", 0)),
                new XElement("UvChangeInterval", new XAttribute("Value", 0)),
                new XElement("ColorScroll", new XAttribute("U", 0), new XAttribute("V", 0)),
                new XElement("ColorScrollRandom", new XAttribute("U", 0), new XAttribute("V", 0)),
                new XElement("ColorScrollSpeed", new XAttribute("Value", 1)),
                new XElement("Material", new XAttribute("Value", (particle.Texture1Name != null)?particle.Texture1Name:"")),
                new XElement("Flags", new XAttribute("Value", 84))
            );

            //foreach (REFFAnimationNode anim in node.Children[2].Children)
            //{
            //    elem.Add(writeAnimation(anim));
            //}

            return elem;
        }

        private static XElement writeAnimation(REFFAnimationNode anim)
        {
            var elem = new XElement("Animation", new XAttribute("Type", anim.KindType));

            elem.Add(new XElement("StartTime", new XAttribute("Value", 0)),
                new XElement("EndTime", new XAttribute("Value", anim.FrameCount)),
                new XElement("RepeatType", new XAttribute("Value", "Constant")),
                new XElement("RandomFlags", new XAttribute("Value", 0))
                );

            ////Placeholder blank key tag
            ///
            var key = new XElement("Key",
                new XAttribute("Time", 0),
                new XAttribute("Value", 0));

            key.Add(new XElement("InterpolationType", new XAttribute("Value", "Linear")),
                new XElement("InParam", new XAttribute("Value", 0)),
                new XElement("OutParam", new XAttribute("Value", 0))
                );

            elem.Add(key);

            return elem;
        }

        public class EffectAnim
        {
            public List<byte[]> KeyTable = new List<byte[]>();
            public List<byte[]> RangeTable = new List<byte[]>();
            public List<byte[]> RandomTable = new List<byte[]>();
            public byte[] InfoTable = new byte[0];

            // Field params
            public v9AnimCurveTargetField fieldType;

            // Field settings
            public float X, Y, Z, Strength, Scope, CenterRotSpd, ExRotSpd, OutsidePosSpec;


            // Get raw animation data
            public EffectAnim(REFFAnimationNode node)
            {
                VoidPtr offset = node.OriginalSource.Address + 0x20;
                if (node.KeyTableSize > 4)
                {
                    ushort count = offset.UShort;
                    uint keyLength = (node.KeyTableSize - 4) / count;
                    for (int i = 0; i < count; i++)
                    {
                        byte[] key = new byte[keyLength];

                        for (int j = 0; j < keyLength; j++)
                            key[j] = (offset+4+(j+(i*keyLength))).Byte;

                        KeyTable.Add(key);
                    }
                }

                offset += node.KeyTableSize;
                if (node.RangeTableSize > 4)
                {
                    ushort count = offset.UShort;
                    uint keyLength = (node.RangeTableSize - 4) / count;
                    for (int i = 0; i < count; i++)
                    {
                        byte[] key = new byte[keyLength];

                        for (int j = 0; j < keyLength; j++)
                            key[j] = (offset + 4 + (j + (i * keyLength))).Byte;

                        RangeTable.Add(key);
                    }
                }

                offset += node.RangeTableSize;
                if (node.RandomTableSize > 4)
                {
                    ushort count = offset.UShort;
                    uint keyLength = (node.RandomTableSize - 4) / count;
                    for (int i = 0; i < count; i++)
                    {
                        byte[] key = new byte[keyLength];

                        for (int j = 0; j < keyLength; j++)
                            key[j] = (offset + 4 + (j + (i * keyLength))).Byte;

                        RandomTable.Add(key);
                    }
                }

                offset += node.RandomTableSize;
                offset += node.NameTableSize;
                if (node.InfoTableSize > 4)
                {
                    byte[] key = new byte[node.InfoTableSize - 4];

                    for (int j = 0; j < (node.InfoTableSize - 4); j++)
                        key[j] = (offset + 4 + j).Byte;

                    InfoTable = key;
                }

                if (node.CurveFlag == AnimCurveType.Field)
                {
                    Enum.TryParse(node.KindType, out fieldType);

                    ExtendedBinaryReader stream = new ExtendedBinaryReader(new MemoryStream(InfoTable), true);

                    switch (fieldType)
                    {
                        case v9AnimCurveTargetField.Gravity:
                        case v9AnimCurveTargetField.Magnet:
                        case v9AnimCurveTargetField.Spin:
                            Strength = stream.ReadSingle(); // Spin: Angular Speed
                            X = stream.ReadSingle();
                            Y = stream.ReadSingle();
                            Z = stream.ReadSingle();
                            break;
                        case v9AnimCurveTargetField.Speed:
                        case v9AnimCurveTargetField.Tail:
                            Strength = stream.ReadSingle();
                            break;
                        case v9AnimCurveTargetField.Newton:
                            Strength = stream.ReadSingle();
                            Scope = stream.ReadSingle();
                            X = stream.ReadSingle();
                            Y = stream.ReadSingle();
                            Z = stream.ReadSingle();
                            break;
                        case v9AnimCurveTargetField.Vortex:
                            CenterRotSpd = stream.ReadSingle();
                            ExRotSpd = stream.ReadSingle();
                            OutsidePosSpec = stream.ReadSingle();
                            X = stream.ReadSingle();
                            Y = stream.ReadSingle();
                            Z = stream.ReadSingle();
                            break;
                        case v9AnimCurveTargetField.Random:
                            Strength = stream.ReadSingle();
                            Scope = stream.ReadSingle(); // Diffusion Angle
                            break;
                    }
                }
            }
        }

        private static void WriteDebugAnimXML(REFFNode reff, XElement root, SettingsFlags flags)
        {
            foreach (REFFEntryNode node in reff.Children)
            {
                var effElem = new XElement("Effect",
                new XAttribute("Name", node.Name));

                foreach (REFFAnimationNode anim in node.Children[2].Children)
                {
                    var animations = new EffectAnim(anim);

                    var animElem = new XElement("Animation",
                    new XAttribute("Name", anim.Name));

                    var keyTableElem = new XElement("KeyTable");
                    var rangeTableElem = new XElement("RangeTable");
                    var randomTableElem = new XElement("RandomTable");
                    var nameTableElem = new XElement("NameTable");

                    int i = 0;
                    foreach (byte[] key in animations.KeyTable)
                    {
                        keyTableElem.Add(new XElement("Key" + i, ByteArrayToString(key)));
                        i++;
                    }

                    i = 0;
                    foreach (byte[] key in animations.RangeTable)
                    {
                        rangeTableElem.Add(new XElement("Key" + i, ByteArrayToString(key)));
                        i++;
                    }

                    i = 0;
                    foreach (byte[] key in animations.RandomTable)
                    {
                        randomTableElem.Add(new XElement("Key" + i, ByteArrayToString(key)));
                        i++;
                    }

                    i = 0;
                    foreach (string name in anim.Names)
                    {
                        nameTableElem.Add(new XElement("Name" + i, name));
                        i++;
                    }

                    var infoTableElem = new XElement("Info", new XElement("Raw", ByteArrayToString(animations.InfoTable)));

                    if (anim.CurveFlag == AnimCurveType.Field)
                    {
                        switch (animations.fieldType)
                        {
                            case v9AnimCurveTargetField.Gravity:
                            case v9AnimCurveTargetField.Magnet:
                                infoTableElem.Add(new XElement("Strength", new XAttribute("Value", animations.Strength)));
                                infoTableElem.Add(new XElement("Vector",
                                    new XAttribute("X", animations.X),
                                    new XAttribute("Y", animations.Y),
                                    new XAttribute("Z", animations.Z)));
                                break;
                            case v9AnimCurveTargetField.Spin:
                                infoTableElem.Add(new XElement("AngularSpeed", new XAttribute("Value", animations.Strength)));
                                infoTableElem.Add(new XElement("Vector",
                                    new XAttribute("X", animations.X),
                                    new XAttribute("Y", animations.Y),
                                    new XAttribute("Z", animations.Z)));
                                break;
                            case v9AnimCurveTargetField.Speed:
                            case v9AnimCurveTargetField.Tail:
                                infoTableElem.Add(new XElement("Strength", new XAttribute("Value", animations.Strength)));
                                break;
                            case v9AnimCurveTargetField.Newton:
                                infoTableElem.Add(new XElement("Strength", new XAttribute("Value", animations.Strength)));
                                infoTableElem.Add(new XElement("Scope", new XAttribute("Value", animations.Scope)));
                                infoTableElem.Add(new XElement("Vector",
                                    new XAttribute("X", animations.X),
                                    new XAttribute("Y", animations.Y),
                                    new XAttribute("Z", animations.Z)));
                                break;
                            case v9AnimCurveTargetField.Vortex:
                                infoTableElem.Add(new XElement("CenterRotationSpeed", new XAttribute("Value", animations.CenterRotSpd)));
                                infoTableElem.Add(new XElement("ExternalRotationSpeed", new XAttribute("Value", animations.ExRotSpd)));
                                infoTableElem.Add(new XElement("OutsidePositionSpecification", new XAttribute("Value", animations.OutsidePosSpec)));
                                infoTableElem.Add(new XElement("Vector",
                                    new XAttribute("X", animations.X),
                                    new XAttribute("Y", animations.Y),
                                    new XAttribute("Z", animations.Z)));
                                break;
                            case v9AnimCurveTargetField.Random:
                                infoTableElem.Add(new XElement("Strength", new XAttribute("Value", animations.Strength)));
                                infoTableElem.Add(new XElement("DiffusionAngle", new XAttribute("Value", animations.Scope)));
                                break;
                        }
                    }
                    
                    animElem.Add(keyTableElem);
                    animElem.Add(rangeTableElem);
                    animElem.Add(randomTableElem);
                    animElem.Add(nameTableElem);
                    animElem.Add(infoTableElem);

                    effElem.Add(animElem);
                }
                root.Add(effElem);
            }
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2} ", b);
            return hex.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.SSBB.Types;
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
            particleList = new List<string>();
            var childrenList = new List<string>();

            foreach (REFFEntryNode node in reff.Children)
            {
                particleList.Add(node.Name);

                // export material
                var particle = node.Children[1] as REFFParticleNode;
                string MaterialName = ((particle.Texture1Name != null) ? particle.Texture1Name : "");

                if (particle.Texture1Name == null)
                {
                    foreach (REFFAnimationNode animNode in node.Children[2].Children)
                    {
                        if (animNode.Name == "Tex1")
                        {
                            MaterialName = animNode.Names[0];
                            break;
                        }
                    }
                } 

                if (MaterialName == "#1_2")
                    MaterialName = "DEFAULT";
                else if (MaterialName == "#3")
                    MaterialName = "INDIRECT_DEFAULT";

                string outfile = outpath + "\\" + MaterialName + ".gtm.xml";

                using (var fileStream = File.Create(outfile))
                {
                    var root = new XElement("Material", new XAttribute(
                    "Name", MaterialName));

                    WriteParticleMaterial(node, root, MaterialName, flags);

                    var doc = new XDocument(root);
                    doc.Save(fileStream);
                }


                // find Child nodes
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

            // DEBUG - attempt parse .remt from .breff

            bool debugREMT = false;
            if (debugREMT)
            {
                foreach (REFFEntryNode node in reff.Children)
                {
                    using (var fileStream = File.Create(outpath + "\\" + node.Name + ".remt"))
                    {
                        var ns_xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
                        var ns_xsd = XNamespace.Get("http://www.w3.org/2001/XMLSchema");

                        var root = new XElement("CEmitterClass",
                            new XAttribute(XNamespace.Xmlns + "xsi", ns_xsi),
                            new XAttribute(XNamespace.Xmlns + "xsd", ns_xsd));

                        WriteREMT(node, root);

                        var doc = new XDocument(root);
                        doc.Save(fileStream);
                    }
                }
            }
        }

        private static void WriteParticleMaterial(REFFEntryNode node, XElement root, string MaterialName, SettingsFlags flags)
        {
            REFFEmitterNode9 emitter = node.Children[0] as REFFEmitterNode9;
            REFFParticleNode particle = node.Children[1] as REFFParticleNode;

            // Get Blend type
            string BlendMode = "Typical";
            switch (emitter.BlendType)
            {
                case BrawlLib.Wii.Graphics.GXBlendMode.None:
                    BlendMode = "Zero";
                    break;
                case BrawlLib.Wii.Graphics.GXBlendMode.Blend:
                    if (emitter.SrcFactor == BrawlLib.Wii.Graphics.BlendFactor.SourceAlpha &&
                        emitter.DstFactor == BrawlLib.Wii.Graphics.BlendFactor.One)
                        BlendMode = "Add";
                    else if (emitter.SrcFactor == BrawlLib.Wii.Graphics.BlendFactor.Zero &&
                        emitter.DstFactor == BrawlLib.Wii.Graphics.BlendFactor.SourceColor)
                        BlendMode = "Multiply";
                    break;
                case BrawlLib.Wii.Graphics.GXBlendMode.Subtract:
                    BlendMode = "Subtract";
                    break;
                default:
                    break;
            }

            int wrapFlags = particle.TextureWrap & 0x0000000F;
            string AddressMode = "Clamp";
            if (wrapFlags != 0)
                AddressMode = "Wrap";


            root.Add(new XElement("Texture",
                    new XAttribute("Value", MaterialName)),
                new XElement("BlendMode",
                    new XAttribute("Value", BlendMode)),
                new XElement("AddressMode",
                    new XAttribute("Value", AddressMode)),
                new XElement("Split",
                    new XAttribute("U", 1 / particle.TextureScale1.X),
                    new XAttribute("V", 1 / particle.TextureScale1.Y)),
                new XElement("Shader",
                    new XAttribute("Name", "BillboardParticle_d[v]"),
                new XElement("Parameter",
                    new XAttribute("Id", 0),
                    new XAttribute("Value", 0)),
                new XElement("Parameter",
                    new XAttribute("Id", 1),
                    new XAttribute("Value", 0)),
                new XElement("Parameter",
                    new XAttribute("Id", 2),
                    new XAttribute("Value", 0)),
                new XElement("Parameter",
                    new XAttribute("Id", 3),
                    new XAttribute("Value", 0))));
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
                    new XAttribute("X", emitter.Translate._x),
                    new XAttribute("Y", emitter.Translate._y),
                    new XAttribute("Z", emitter.Translate._z)),
                new XElement("Rotation",
                    new XAttribute("X", ((180 / Math.PI) * emitter.Rotate._x).ToString("0.####")),
                    new XAttribute("Y", ((180 / Math.PI) * emitter.Rotate._y).ToString("0.####")),
                    new XAttribute("Z", ((180 / Math.PI) * emitter.Rotate._z).ToString("0.####"))),
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
                new XElement("EmissionInterval", new XAttribute("Value", (emitter.EmitInterval > 0)? emitter.EmitInterval : 1)),
                new XElement("ParticlePerEmission", new XAttribute("Value", emitter.Emit))
            );

            // Shape specific properties
            switch (emitForm)
            {
                case EmitFormType.Disc:
                    elem.Add(new XElement("Radius", new XAttribute("Value", emitter.CommonParam1)),
                        new XElement("Height", new XAttribute("Value", 0)),
                        new XElement("StartAngle", new XAttribute("Value", ((180 / Math.PI) * emitter.CommonParam3).ToString("0.####"))),
                        new XElement("EndAngle", new XAttribute("Value", ((180 / Math.PI) * emitter.CommonParam4).ToString("0.####"))),
                        new XElement("EmissionDirectionType", new XAttribute("Value", "ParticleVelocity"))
                    );
                    break;
                case EmitFormType.Cylinder:
                case EmitFormType.Torus: // idk
                    elem.Add(new XElement("Radius", new XAttribute("Value", emitter.CommonParam1)),
                        new XElement("Height", new XAttribute("Value", emitter.CommonParam5)),
                        new XElement("StartAngle", new XAttribute("Value", ((180 / Math.PI) * emitter.CommonParam3).ToString("0.####"))),
                        new XElement("EndAngle", new XAttribute("Value", ((180 / Math.PI) * emitter.CommonParam4).ToString("0.####"))),
                        new XElement("EmissionDirectionType", new XAttribute("Value", "ParticleVelocity"))
                    );
                    break;
                case EmitFormType.Sphere:
                    elem.Add(new XElement("Radius", new XAttribute("Value", emitter.CommonParam1)),
                        new XElement("Latitude", new XAttribute("Value", ((180 / Math.PI) * emitter.CommonParam3).ToString("0.####"))),
                        new XElement("Longitude", new XAttribute("Value", ((180 / Math.PI) * emitter.CommonParam4).ToString("0.####"))),
                        new XElement("EmissionDirectionType", new XAttribute("Value", "ParticleVelocity"))
                    );
                    break;
                case EmitFormType.Cube:
                    elem.Add(new XElement("Size",
                        new XAttribute("X", emitter.CommonParam1),
                        new XAttribute("Y", emitter.CommonParam2),
                        new XAttribute("Z", emitter.CommonParam3))
                    );
                    break;
                case EmitFormType.Line: // idk
                    elem.Add(new XElement("Size",
                        new XAttribute("X", emitter.CommonParam1),
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
            //EFFAnimationListNode animationlist = node.Children[2] as REFFAnimationListNode;

            // Get Field parameters from animation nodes
            List<EffectAnim> animations = new List<EffectAnim>();
            foreach (REFFAnimationNode anim in node.Children[2].Children)
            {
                animations.Add(new EffectAnim(anim));
            }


            string MaterialName = ((particle.Texture1Name != null) ? particle.Texture1Name : "");
            if (particle.Texture1Name == null)
            {
                foreach (REFFAnimationNode animNode in node.Children[2].Children)
                {
                    if (animNode.Name == "Tex1")
                    {
                        MaterialName = animNode.Names[0];
                        break;
                    }
                }
            }
            if (MaterialName == "#1_2")
                MaterialName = "DEFAULT";
            else if (MaterialName == "#3")
                MaterialName = "INDIRECT_DEFAULT";


            Vector3 gravity = new Vector3();
            float acceleration = 0;

            bool isColorRAnim = false;
            bool isColorGAnim = false;
            bool isColorBAnim = false;
            bool isColorAAnim = false;

            foreach (EffectAnim anim in animations)
            {
                if (anim.curveType == AnimCurveType.Field)
                {
                    switch (anim.fieldType)
                    {
                        case v9AnimCurveTargetField.Gravity:
                            gravity = EulerToDirection(anim.X, anim.Y, anim.Z, true) * anim.Strength;
                            break;
                        case v9AnimCurveTargetField.Magnet:
                            break;
                        case v9AnimCurveTargetField.Spin:
                            break;
                        case v9AnimCurveTargetField.Speed:
                            acceleration = anim.Strength;
                            break;
                        case v9AnimCurveTargetField.Tail:
                            break;
                        case v9AnimCurveTargetField.Newton:
                            break;
                        case v9AnimCurveTargetField.Vortex:
                            break;
                        case v9AnimCurveTargetField.Random:
                            break;
                    }
                }
                else if (anim.curveType == AnimCurveType.ParticleByte)
                    switch (anim.kindType)
                    {
                        case v9AnimCurveTargetByteFloat.Color0Primary:
                            if (IsBitSet(anim.animNode.KindEnable, 0))
                                isColorRAnim = true;
                            if (IsBitSet(anim.animNode.KindEnable, 1))
                                isColorGAnim = true;
                            if (IsBitSet(anim.animNode.KindEnable, 2))
                                isColorBAnim = true;
                            break;
                        case v9AnimCurveTargetByteFloat.Alpha0Primary:
                            isColorAAnim = true;
                            break;
                    }
                        
            }

            Vector3 direction = EulerToDirection(emitter.VelSpecDir._x, emitter.VelSpecDir._y, emitter.VelSpecDir._z, true);


            var elem = new XElement("Particle",
                new XAttribute("Id", id),
                new XAttribute("Name", node.Name),
                new XAttribute("Type", "Quad"));

            elem.Add(new XElement("LifeTime", new XAttribute("Value", emitter.PtclLife)),
                new XElement("PivotPosition", new XAttribute("Value", "MiddleCenter")),
                new XElement("DirectionType", new XAttribute("Value", "Billboard")),
                new XElement("ZOffset", new XAttribute("Value", 0)),
                new XElement("Size",
                    new XAttribute("X", particle.Size.X),
                    new XAttribute("Y", particle.Size.Y),
                    new XAttribute("Z", 1)),
                new XElement("SizeRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("Rotation",
                    new XAttribute("X", ((180 / Math.PI) * particle.Rotate._x).ToString("0.####")),
                    new XAttribute("Y", ((180 / Math.PI) * particle.Rotate._y).ToString("0.####")),
                    new XAttribute("Z", ((180 / Math.PI) * particle.Rotate._z).ToString("0.####"))),
                new XElement("RotationRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("RotationAdd",
                    new XAttribute("X", ((180 / Math.PI) * particle.RotateOffset._x).ToString("0.####")),
                    new XAttribute("Y", ((180 / Math.PI) * particle.RotateOffset._y).ToString("0.####")),
                    new XAttribute("Z", ((180 / Math.PI) * particle.RotateOffset._z).ToString("0.####"))),
                new XElement("RotationAddRandom",
                    new XAttribute("X", ((((180 / Math.PI) * particle.RotateOffset._x) * 0.01) * particle.RotateOffsetRandom1).ToString("0.####")),
                    new XAttribute("Y", ((((180 / Math.PI) * particle.RotateOffset._y) * 0.01) * particle.RotateOffsetRandom2).ToString("0.####")),
                    new XAttribute("Z", ((((180 / Math.PI) * particle.RotateOffset._z) * 0.01) * particle.RotateOffsetRandom3).ToString("0.####"))),
                new XElement("Direction",
                    new XAttribute("X", direction.X),
                    new XAttribute("Y", direction.Y),
                    new XAttribute("Z", direction.Z)),
                new XElement("DirectionRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("Speed", new XAttribute("Value", emitter.VelPowerSpecDir)),
                new XElement("SpeedRandom", new XAttribute("Value", 0)),
                new XElement("GravitationalAccel",
                    new XAttribute("X", gravity.X.ToString("0.####")),
                    new XAttribute("Y", gravity.Y.ToString("0.####")),
                    new XAttribute("Z", gravity.Z.ToString("0.####"))),
                new XElement("ExternalAccel",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("ExternalAccelRandom",
                    new XAttribute("X", 0),
                    new XAttribute("Y", 0),
                    new XAttribute("Z", 0)),
                new XElement("Deceleration", new XAttribute("Value", acceleration.ToString("0.####"))),
                new XElement("DecelerationRandom", new XAttribute("Value", 0)),
                new XElement("ReflectionCoeff", new XAttribute("Value", 0)),
                new XElement("ReflectionCoeffRandom", new XAttribute("Value", 0)),
                new XElement("ReboundPlaneY", new XAttribute("Value", 0)),
                new XElement("MaxCount", new XAttribute("Value", 30)),
                new XElement("Color",
                    new XAttribute("R", isColorRAnim ? 1 : (particle.Color1Primary.R * flags.cFactor)),
                    new XAttribute("G", isColorGAnim ? 1 : (particle.Color1Primary.G * flags.cFactor)),
                    new XAttribute("B", isColorBAnim ? 1 : (particle.Color1Primary.B * flags.cFactor)),
                    new XAttribute("A", isColorAAnim ? 1 : (particle.Color1Primary.A * flags.cFactor))), // todo set to 1 if color/alpha anim exists
                new XElement("TextureIndex", new XAttribute("Value", 0)),
                new XElement("UvIndexType", new XAttribute("Value", "Fixed")),
                new XElement("UvIndex", new XAttribute("Value", 0)),
                new XElement("UvChangeInterval", new XAttribute("Value", 0)),
                new XElement("ColorScroll", new XAttribute("U", 0), new XAttribute("V", 0)),
                new XElement("ColorScrollRandom", new XAttribute("U", 0), new XAttribute("V", 0)),
                new XElement("ColorScrollSpeed", new XAttribute("Value", 1)),
                new XElement("Material", new XAttribute("Value", MaterialName)),
                new XElement("Flags", new XAttribute("Value", 68))
            );

            foreach (EffectAnim anim in animations)
            {
                elem = writeAnimation(elem, anim);
            }

            return elem;
        }

        [Flags]
        public enum AnimTypeSize
        {
            Color0Primary = 3,
            Alpha0Primary = 1,
            Color0Secondary = 3,
            Alpha0Secondary = 1,
            Color1Primary = 3,
            Alpha1Primary = 1,
            Color1Secondary = 3,
            Alpha1Secondary = 1,
            Size = 2,
            Scale = 2,
            Tex1Scale = 2,
            Tex1Rot = 1,
            Tex1Trans = 2,
            Tex2Scale = 2,
            Tex2Rot = 1,
            Tex2Trans = 2,
            TexIndScale = 2,
            TexIndRot = 1,
            TexIndTrans = 2,
            AlphaCompareRef0 = 1,
            AlphaCompareRef1 = 1
        }
        private static XElement writeAnimation(XElement rootElem, EffectAnim anim)
        {
            string GensType = "default";
            int keySize;
            int currOffset = 0;

            switch (anim.curveType)
            {
                case AnimCurveType.ParticleByte:
                    switch (anim.kindType)
                    {
                        case v9AnimCurveTargetByteFloat.Color0Primary:
                            keySize = 3;
                            if (IsBitSet(anim.animNode.KindEnable, 0))
                            {
                                GensType = "ColorR";
                                rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                                currOffset++;
                            }
                            if (IsBitSet(anim.animNode.KindEnable, 1))
                            {
                                GensType = "ColorG";
                                rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                                currOffset++;
                            }
                            if (IsBitSet(anim.animNode.KindEnable, 2))
                            {
                                GensType = "ColorB";
                                rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                                currOffset++;
                            }
                            break;
                        case v9AnimCurveTargetByteFloat.Alpha0Primary:
                            keySize = 1;
                            GensType = "ColorA";
                            rootElem.Add(writeAnimationElement(anim, GensType, keySize));
                            break;
                        
                        default:
                            return rootElem;
                    }
                    break;
                case AnimCurveType.ParticleFloat:
                    switch (anim.kindType)
                    {
                        case v9AnimCurveTargetByteFloat.Size:
                            keySize = 2;
                            if (IsBitSet(anim.animNode.KindEnable, 0))
                            {
                                GensType = "Sx";
                                rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                                currOffset++;
                            }
                            if (IsBitSet(anim.animNode.KindEnable, 1))
                            {
                                GensType = "Sy";
                                rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                                currOffset++;
                            }
                            break;
                        default:
                            return rootElem;
                    }
                    break;
                case AnimCurveType.ParticleRotate:
                    keySize = 3;
                    if (IsBitSet(anim.animNode.KindEnable, 0))
                    {
                        GensType = "Rx";
                        rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                        currOffset++;
                    }
                    if (IsBitSet(anim.animNode.KindEnable, 1))
                    {
                        GensType = "Ry";
                        rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                        currOffset++;
                    }
                    if (IsBitSet(anim.animNode.KindEnable, 2))
                    {
                        GensType = "Rz";
                        rootElem.Add(writeAnimationElement(anim, GensType, keySize, currOffset));
                        currOffset++;
                    }
                    break;
                default:
                    return rootElem;
            }
            return rootElem;
        }

        static bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
        private static XElement writeAnimationElement(EffectAnim anim, string GensType, int keySize = 1, int offset = 0)
        {
            var elem = new XElement("Animation", new XAttribute("Type", GensType));

            elem.Add(new XElement("StartTime", new XAttribute("Value", 0)),
                new XElement("EndTime", new XAttribute("Value", anim.animNode.FrameCount)),
                new XElement("RepeatType", new XAttribute("Value", "Constant")),
                new XElement("RandomFlags", new XAttribute("Value", 0))
                );

            foreach (Key key in anim.keys)
            {
                bool isRadian = (anim.curveType == AnimCurveType.ParticleRotate);
                elem.Add(writeKeyElement(key, keySize, offset, isRadian));
            }

            return elem;
        }

        public static float GetRangePercentage(uint input)
        {
            Dictionary<uint, float> ranges = new Dictionary<uint, float>();
            ranges[0x75229000] = 0.01f;
            ranges[0x75A14852] = 0.02f;
            ranges[0x75F2EC7A] = 0.03f;
            ranges[0x761EB9F6] = 0.04f;
            ranges[0x76466734] = 0.05f;
            ranges[0x766E1571] = 0.06f;
            ranges[0x768767F4] = 0.07f;
            ranges[0x76999A3D] = 0.08f;
            ranges[0x76ADCD84] = 0.09f;
            ranges[0x76C001CD] = 0.1f;
            ranges[0x76D33414] = 0.11f;
            ranges[0x76E6675C] = 0.12f;
            ranges[0x77F57B65] = 0.13f;
            ranges[0x77FE7032] = 0.14f;
            ranges[0x77066700] = 0.15f;
            ranges[0x770F5DCD] = 0.16f;
            ranges[0x77195399] = 0.17f;
            ranges[0x77214867] = 0.18f;
            ranges[0x772A3F33] = 0.19f;
            ranges[0x77333400] = 0.2f;
            ranges[0x773D2ACB] = 0.21f;
            ranges[0x77452099] = 0.22f;
            ranges[0x774E1666] = 0.23f;
            ranges[0x77570B33] = 0.24f;
            ranges[0x77600080] = 0.25f;
            ranges[0x7764D88E] = 0.26f;
            ranges[0x7768AE9E] = 0.27f;
            ranges[0x776C85AD] = 0.28f;
            ranges[0x776F5DBE] = 0.29f;
            ranges[0x777334CD] = 0.3f;
            ranges[0x77770ADD] = 0.31f;
            ranges[0x777BE2EA] = 0.32f;
            ranges[0x777EB9FB] = 0.33f;
            ranges[0x77839009] = 0.34f;
            ranges[0x77876718] = 0.35f;
            ranges[0x778A3E29] = 0.36f;
            ranges[0x778E1538] = 0.37f;
            ranges[0x7792ED47] = 0.38f;
            ranges[0x7796C356] = 0.39f;
            ranges[0x77999A67] = 0.4f;
            ranges[0x779D7276] = 0.41f;
            ranges[0x77A24984] = 0.42f;
            ranges[0x77A51F95] = 0.43f;
            ranges[0x77A9F7A2] = 0.44f;
            ranges[0x77ADCEB2] = 0.45f;
            ranges[0x77B1A5C1] = 0.46f;
            ranges[0x77B47CD2] = 0.47f;
            ranges[0x77B853E1] = 0.48f;
            ranges[0x77BC2AF1] = 0.49f;
            ranges[0x78C0FF7F] = 0.5f;
            ranges[0x78C14731] = 0.51f;
            ranges[0x78C38FE0] = 0.52f;
            ranges[0x78C4D691] = 0.53f;
            ranges[0x78C51E43] = 0.54f;
            ranges[0x78C666F4] = 0.55f;
            ranges[0x78C8ADA3] = 0.56f;
            ranges[0x78C9F653] = 0.57f;
            ranges[0x78CA3D05] = 0.58f;
            ranges[0x78CC84B4] = 0.59f;
            ranges[0x78CDCD65] = 0.6f;
            ranges[0x78CE1417] = 0.61f;
            ranges[0x78CF5BC8] = 0.62f;
            ranges[0x78D1A477] = 0.63f;
            ranges[0x78D2EB28] = 0.64f;
            ranges[0x78D333DA] = 0.65f;
            ranges[0x78D47B8B] = 0.66f;
            ranges[0x78D6C23A] = 0.67f;
            ranges[0x78D70AEC] = 0.68f;
            ranges[0x78D8529C] = 0.69f;
            ranges[0x78DA994B] = 0.7f;
            ranges[0x78DBE1FC] = 0.71f;
            ranges[0x78DC29AE] = 0.72f;
            ranges[0x78DD715F] = 0.73f;
            ranges[0x78DFB80E] = 0.74f;
            ranges[0x78E0FFBF] = 0.75f;
            ranges[0x78E14871] = 0.76f;
            ranges[0x78E38F20] = 0.77f;
            ranges[0x78E4D6D1] = 0.78f;
            ranges[0x78E51F83] = 0.79f;
            ranges[0x78E66634] = 0.8f;
            ranges[0x78E8ADE3] = 0.81f;
            ranges[0x78E9F693] = 0.82f;
            ranges[0x78EA3D45] = 0.83f;
            ranges[0x78EC85F4] = 0.84f;
            ranges[0x78EDCDA5] = 0.85f;
            ranges[0x78EE1457] = 0.86f;
            ranges[0x78EF5C08] = 0.87f;
            ranges[0x78F1A4B7] = 0.88f;
            ranges[0x78F2EB68] = 0.89f;
            ranges[0x78F3331A] = 0.9f;
            ranges[0x78F47BCB] = 0.91f;
            ranges[0x78F6C37A] = 0.92f;
            ranges[0x78F70A2C] = 0.93f;
            ranges[0x78F852DC] = 0.94f;
            ranges[0x78FA9A8B] = 0.95f;
            ranges[0x78FBE13C] = 0.96f;
            ranges[0x78FC29EE] = 0.97f;
            ranges[0x78FD719F] = 0.98f;
            ranges[0x78FFB84E] = 0.99f;
            ranges[0x78000080] = 1f;
            //idk?
            ranges[0x76A14752] = 0.02f;
            ranges[0x76F2EB7A] = 0.03f;
            ranges[0x778766F4] = 0.07f;
            ranges[0x7799993D] = 0.08f;
            ranges[0x77ADCC84] = 0.09f;
            ranges[0x77C000CD] = 0.1f;
            ranges[0x77D33314] = 0.11f;
            ranges[0x77E6665C] = 0.12f;
            ranges[0x7860FF7F] = 0.25f;
            ranges[0x7864D78E] = 0.26f;
            ranges[0x7868AD9E] = 0.27f;
            ranges[0x786C84AD] = 0.28f;
            ranges[0x786F5CBE] = 0.29f;
            ranges[0x787333CD] = 0.3f;
            ranges[0x787709DD] = 0.31f;
            ranges[0x787BE1EA] = 0.32f;
            ranges[0x787EB8FB] = 0.33f;
            ranges[0x78838F09] = 0.34f;
            ranges[0x78876618] = 0.35f;
            ranges[0x788A3D29] = 0.36f;
            ranges[0x788E1438] = 0.37f;
            ranges[0x7892EC47] = 0.38f;
            ranges[0x7896C256] = 0.39f;
            ranges[0x78999967] = 0.4f;
            ranges[0x789D7176] = 0.41f;
            ranges[0x78A24884] = 0.42f;
            ranges[0x78A51E95] = 0.43f;
            ranges[0x78A9F6A2] = 0.44f;
            ranges[0x78ADCDB2] = 0.45f;
            ranges[0x78B1A4C1] = 0.46f;
            ranges[0x78B47BD2] = 0.47f;
            ranges[0x78B852E1] = 0.48f;
            ranges[0x78BC29F1] = 0.49f;

            if (ranges.ContainsKey(input))
                return ranges[input];
            else
                return -1f;
        }

        private static XElement writeKeyElement(Key key, int keysize = 1, int offset = 0, bool isRadian = false)
        {
            var elem = new XElement("Key",
                new XAttribute("Time", key.frame));

            float value = 0f;
            float randomRange = 0f;

            if (key.dataf32 != null)
            {
                if (key.flag == 1)
                {
                    value = key.dataf32[offset * 2];

                    uint baseVal = BitConverter.ToUInt32(BitConverter.GetBytes(value).Take(4).Reverse().ToArray(), 0);
                    uint rangeVal = BitConverter.ToUInt32(BitConverter.GetBytes(key.dataf32[(offset * 2)+1]).Take(4).Reverse().ToArray(), 0);

                    //byte[] baseVal = BitConverter.GetBytes(value);
                    //byte[] rangeVal = BitConverter.GetBytes(key.dataf32[(offset * 2) + 1]);

                    uint range = rangeVal - baseVal;

                    range = BitConverter.ToUInt32(BitConverter.GetBytes(range).Take(4).Reverse().ToArray(), 0);

                    //Console.WriteLine("{0:X8}", range);


                    float rangePercent = GetRangePercentage(range);

                    if (rangePercent > 0f)
                    {
                        float actualBase = value / (rangePercent + 1);

                        float actualRange = (actualBase * rangePercent) * 2;

                        value = actualBase;
                        randomRange = actualRange;
                    }
                    else
                    {
                        randomRange = 0f;
                    }
                }
                else if (key.flag == 1)
                {
                    value = key.dataf32[offset * 2];
                    randomRange = key.dataf32[(offset * 2) + 1];
                }
                else
                    value = key.dataf32[offset];

                if (isRadian)
                    value = (float)((180 / Math.PI) * value);
            }
            else if (key.datau8 != null)
            {
                if (key.flag == 1 || key.flag == 2)
                {
                    value = key.datau8[offset * 2];
                    randomRange = key.datau8[(offset * 2) + 1];
                }
                else
                    value = key.datau8[offset];
            }

            elem.Add(new XAttribute(new XAttribute("Value", value.ToString("0.####"))));

            string InterpType;
            switch (key.curveInterpType[offset])
            {
                case Key.CurveInterpolationType.Linear:
                    InterpType = "Linear";
                    break;
                case Key.CurveInterpolationType.Constant:
                    InterpType = "Constant";
                    break;
                default: // Hermite
                    InterpType = "Hermite";
                    break;
            }

            elem.Add(new XElement("InterpolationType", new XAttribute("Value", InterpType)),
                new XElement("InParam", new XAttribute("Value", 0)), // todo: calculate for Fast In/Out hermite
                new XElement("OutParam", new XAttribute("Value", 0))
                );

            if (randomRange != 0f)
            {
                elem.Add(new XElement("RandomRange", new XAttribute("Value", randomRange.ToString("0.####"))));
            }

            return elem;
        }

        public static Vector3 EulerToDirection(float pitch, float yaw, float roll, bool inRadians = false)
        {
            if (!inRadians)
            {
                // Convert degrees to radians
                yaw = (float)(yaw * Math.PI / 180.0);
                pitch = (float)(pitch * Math.PI / 180.0);
                roll = (float)(roll * Math.PI / 180.0);
            }

            // Calculate the direction vector
            float x = (float)(-Math.Cos(yaw) * Math.Sin(pitch));
            float y = (float)(Math.Cos(pitch));
            float z = (float)(Math.Sin(yaw) * Math.Sin(pitch));

            // Apply the roll
            Vector3 direction = new Vector3(x, y, z);
            Matrix4x4 rollRotation = Matrix4x4.CreateFromAxisAngle(direction, roll);
            direction = Vector3.Transform(direction, rollRotation);

            return direction;
        }

        public class Key
        {
            public uint frame;
            public uint flag; // 0: normal, 1: Range, 2: Random
            public CurveInterpolationType[] curveInterpType = new CurveInterpolationType[8];

            public uint randomIndex;

            // Texture
            public byte wrap;
            public byte reverse;

            //Texture + child
            public uint nameIndex;

            public byte[] datau8;
            public float[] dataf32;

            public EmitterInheritSetting inheritSetting;

            [Flags]
            public enum CurveInterpolationType
            {
                Linear = 0x00,
                Constant = 0x02,
                HermiteSlowInSlowOut = 0x01,
                HermiteFastInSlowOut = 0x05,
                HermiteFastInFastOut = 0x09,
                HermiteSlowInFastOut = 0x0D,
            }

            public struct EmitterInheritSetting
            {
                public int speed;
                public byte scale;
                public byte alpha;
                public byte color;
                public byte weight;
                public byte type;
                public byte flag;
                public byte alphaFuncPri;
                public byte alphaFuncSec;
            };


            //public struct XByteData // Alpha, AlphaComp
            //{
            //    public byte X;
            //    public byte Y;
            //    public byte Z;
            //};
            //public struct XYByteData
            //{
            //    public byte X;
            //    public byte Y;
            //};
            //public struct XYZByteData // Color
            //{
            //    public byte X;
            //    public byte Y;
            //    public byte Z;
            //};


            //public struct XFloatData // TexRot
            //{
            //    public float X;
            //};

            //public struct XYFloatData // Size, Scale, TexTrans, TexScale
            //{
            //    public float X;
            //    public float Y;
            //};

            //public struct XYZFloatData // Rotate, EmitterScale, 
            //{
            //    public float X;
            //    public float Y;
            //    public float Z;
            //};

        }

        public class EffectAnim
        {
            public class RndClrTbl
            {
                public byte R;
                public byte RRange;
                public byte G;
                public byte GRange;
                public byte B;
                public byte BRange;
                public byte A;
                public byte ARange;
            };

            public REFFAnimationNode animNode;

            public List<byte[]> KeyTable = new List<byte[]>();
            public List<byte[]> RangeTable = new List<byte[]>();
            public List<byte[]> RandomTable = new List<byte[]>();
            public byte[] InfoTable = new byte[0];

            public AnimCurveType curveType;

            public v9AnimCurveTargetByteFloat kindType;

            // Field params
            public v9AnimCurveTargetField fieldType;

            // Field settings
            public float X, Y, Z, Strength, Scope, CenterRotSpd, ExRotSpd, OutsidePosSpec;

            // Keys
            public List<Key> keys = new List<Key>();
            public List<RndClrTbl[]> RandomColorTable = new List<RndClrTbl[]>();


            // Get raw animation data
            public EffectAnim(REFFAnimationNode node)
            {
                animNode = node;

                curveType = node.CurveFlag;

                BrawlLib.Internal.VoidPtr offset = node.OriginalSource.Address + 0x20;
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

                switch (curveType)
                {

                    case AnimCurveType.ParticleByte:
                    case AnimCurveType.ParticleFloat:
                    case AnimCurveType.EmitterFloat:
                        Enum.TryParse(node.KindType, out kindType);
                        break;
                    case AnimCurveType.Field:
                        Enum.TryParse(node.KindType, out fieldType);
                        break;
                    default:
                        break;
                }

                // Read random color table
                if (RandomTable.Count > 0 && curveType == AnimCurveType.ParticleByte)
                {
                    switch (kindType)
                    {
                        case v9AnimCurveTargetByteFloat.Color0Primary:
                        case v9AnimCurveTargetByteFloat.Color0Secondary:
                        case v9AnimCurveTargetByteFloat.Color1Primary:
                        case v9AnimCurveTargetByteFloat.Color1Secondary:
                            for (int i = 0; i < RandomTable.Count; i++)
                            {
                                var rndClrStream = new ExtendedBinaryReader(new MemoryStream(RandomTable[i]), true);
                                RndClrTbl rndClrTblEntry = new RndClrTbl();

                                rndClrTblEntry.R = rndClrStream.ReadByte();
                                rndClrTblEntry.RRange = rndClrStream.ReadByte();
                                rndClrTblEntry.G = rndClrStream.ReadByte();
                                rndClrTblEntry.GRange = rndClrStream.ReadByte();
                                rndClrTblEntry.B = rndClrStream.ReadByte();
                                rndClrTblEntry.BRange = rndClrStream.ReadByte();
                            }
                            break;
                        case v9AnimCurveTargetByteFloat.Alpha0Primary:
                        case v9AnimCurveTargetByteFloat.Alpha0Secondary:
                        case v9AnimCurveTargetByteFloat.Alpha1Primary:
                        case v9AnimCurveTargetByteFloat.Alpha1Secondary:
                            for (int i = 0; i < RandomTable.Count; i++)
                            {
                                var rndClrStream = new ExtendedBinaryReader(new MemoryStream(RandomTable[i]), true);
                                RndClrTbl rndClrTblEntry = new RndClrTbl();

                                rndClrTblEntry.A = rndClrStream.ReadByte();
                                rndClrTblEntry.ARange = rndClrStream.ReadByte();
                            }
                            break;
                        default:
                            break;
                    }
                }

                // Field type basic parameters
                if (curveType == AnimCurveType.Field)
                {
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

                // Read keys
                foreach (byte[] rawKey in KeyTable)
                {
                    ExtendedBinaryReader stream = new ExtendedBinaryReader(new MemoryStream(rawKey), true);

                    Key key = new Key();

                    key.frame = stream.ReadUInt16();
                    key.flag = stream.ReadByte();
                    stream.JumpAhead(1);

                    for (int i = 0; i < 8; i++)
                        key.curveInterpType[i] = (Key.CurveInterpolationType)stream.ReadByte();

                    // Random key
                    if (key.flag == 1)
                    {
                        key.randomIndex = stream.ReadUInt16();

                        ExtendedBinaryReader randStream;

                        randStream = new ExtendedBinaryReader(new MemoryStream(RangeTable[(int)key.randomIndex]), true);

                        switch (curveType)
                        {
                            
                            case AnimCurveType.ParticleByte:
                                key.datau8 = randStream.ReadBytes((int)(randStream.BaseStream.Length - randStream.BaseStream.Position));
                                break;
                            case AnimCurveType.Field:
                            case AnimCurveType.PostField:
                            case AnimCurveType.ParticleRotate:
                            case AnimCurveType.ParticleFloat:
                            case AnimCurveType.EmitterFloat:
                                uint floatCount = (uint)((randStream.BaseStream.Length - randStream.BaseStream.Position) / 4);
                                float[] data = new float[floatCount];
                                for (int i = 0; i < floatCount; i++)
                                {
                                    data[i] = randStream.ReadSingle();
                                }
                                key.dataf32 = data;
                                break;
                            case AnimCurveType.ParticleTexture:
                            case AnimCurveType.Child:
                            default:
                                break;
                        }
                    }
                    else if (key.flag == 2) // Random color table
                    {
                        key.randomIndex = stream.ReadUInt16();
                        // I think this is just part of the random seed calculations?

                        // placeholder thing so prevent crash uhhh
                        ExtendedBinaryReader randStream;

                        randStream = new ExtendedBinaryReader(new MemoryStream(RandomTable[0]), true);

                        switch (curveType)
                        {

                            case AnimCurveType.ParticleByte:
                                key.datau8 = randStream.ReadBytes((int)(randStream.BaseStream.Length - randStream.BaseStream.Position));
                                break;
                            case AnimCurveType.Field:
                            case AnimCurveType.PostField:
                            case AnimCurveType.ParticleRotate:
                            case AnimCurveType.ParticleFloat:
                            case AnimCurveType.EmitterFloat:
                                uint floatCount = (uint)((randStream.BaseStream.Length - randStream.BaseStream.Position) / 4);
                                float[] data = new float[floatCount];
                                for (int i = 0; i < floatCount; i++)
                                {
                                    data[i] = randStream.ReadSingle();
                                }
                                key.dataf32 = data;
                                break;
                            case AnimCurveType.ParticleTexture:
                            case AnimCurveType.Child:
                            default:
                                break;
                        }
                    }
                    else // Normal non random key
                    {
                        switch (curveType)
                        {
                            case AnimCurveType.ParticleTexture:
                                key.wrap = stream.ReadByte();
                                key.reverse = stream.ReadByte();
                                key.nameIndex = stream.ReadUInt16();
                                break;
                            case AnimCurveType.Child:
                                key.inheritSetting.speed = stream.ReadInt16();
                                key.inheritSetting.scale = stream.ReadByte();
                                key.inheritSetting.alpha = stream.ReadByte();
                                key.inheritSetting.color = stream.ReadByte();
                                key.inheritSetting.weight = stream.ReadByte();
                                key.inheritSetting.type = stream.ReadByte();
                                key.inheritSetting.flag = stream.ReadByte();
                                key.inheritSetting.alphaFuncPri = stream.ReadByte();
                                key.inheritSetting.alphaFuncSec = stream.ReadByte();
                                key.nameIndex = stream.ReadUInt16();
                                break;
                            case AnimCurveType.ParticleByte:
                                key.datau8 = stream.ReadBytes((int)(stream.BaseStream.Length - stream.BaseStream.Position));
                                break;
                            case AnimCurveType.Field:
                            case AnimCurveType.PostField:
                            case AnimCurveType.ParticleRotate:
                            case AnimCurveType.ParticleFloat:
                            case AnimCurveType.EmitterFloat:
                                uint floatCount = (uint)((stream.BaseStream.Length - stream.BaseStream.Position) / 4);
                                float[] data = new float[floatCount];
                                for (int i = 0; i < floatCount; i++)
                                {
                                    data[i] = stream.ReadSingle();
                                }
                                key.dataf32 = data;
                                break;
                            default:
                                break;
                        }
                    }
                    
                    keys.Add(key);
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

                    animElem.Add(new XElement("KindEnable", Convert.ToString(anim.KindEnable, 2).PadLeft(8, '0')));

                    var keyTableElem = new XElement("KeyTable");
                    var rangeTableElem = new XElement("RangeTable");
                    var randomTableElem = new XElement("RandomTable");
                    var nameTableElem = new XElement("NameTable");

                    int i = 0;
                    foreach (byte[] key in animations.KeyTable)
                    {
                        var keyElem = new XElement("Key" + i);

                        keyElem.Add(new XElement("frame", animations.keys[i].frame));
                        keyElem.Add(new XElement("flag", animations.keys[i].flag));
                        keyElem.Add(new XElement("curveType", animations.keys[i].curveInterpType));

                        if (animations.keys[i].datau8 != null)
                            keyElem.Add(new XElement("data", ByteArrayToString(animations.keys[i].datau8)));
                        else if (animations.keys[i].dataf32 != null)
                            keyElem.Add(new XElement("data", FloatArrayToString(animations.keys[i].dataf32)));

                        byte[] restArray = new byte[key.Length - 12];
                        Array.Copy(key, 12, restArray, 0, key.Length - 12);

                        keyElem.Add(new XElement("raw", ByteArrayToString(restArray)));

                        keyTableElem.Add(keyElem);
                        i++;
                    }

                    i = 0;
                    foreach (byte[] key in animations.RangeTable)
                    {
                        rangeTableElem.Add(new XElement("Key" + i, ByteArrayToString(key)));
                        //if (animations.keys[i].dataf32 != null)
                        //{
                        //    int floatNum = key.Length / 8;

                        //    uint[] ranges = new uint[floatNum];

                        //    for (int j = 0; j < floatNum; j++)
                        //    {
                        //        uint baseVal = BitConverter.ToUInt32(key.Skip(j * 8).Take(4).Reverse().ToArray(), 0);
                        //        uint rangeVal = BitConverter.ToUInt32(key.Skip(j * 8 + 4).Take(4).Reverse().ToArray(), 0);

                        //        ranges[j] = rangeVal - baseVal;
                        //    }

                        //    rangeTableElem.Add(new XElement("Key" + i + "Range", UintArrayToString(ranges)));
                        //}
                        i++;
                    }

                    i = 0;
                    foreach (byte[] entry in animations.RandomTable)
                    {
                        var rndClrElem = new XElement("Entry" + i);

                        if (animations.curveType == AnimCurveType.ParticleByte)
                        {
                            switch (animations.kindType)
                            {
                                case v9AnimCurveTargetByteFloat.Color0Primary:
                                case v9AnimCurveTargetByteFloat.Color0Secondary:
                                case v9AnimCurveTargetByteFloat.Color1Primary:
                                case v9AnimCurveTargetByteFloat.Color1Secondary:
                                    rndClrElem.Add(new XElement("X",
                                        new XAttribute("Value", animations.RandomTable[i][0]),
                                        new XAttribute("Range", animations.RandomTable[i][1])));
                                    rndClrElem.Add(new XElement("Y",
                                        new XAttribute("Value", animations.RandomTable[i][2]),
                                        new XAttribute("Range", animations.RandomTable[i][3])));
                                    rndClrElem.Add(new XElement("Z",
                                        new XAttribute("Value", animations.RandomTable[i][4]),
                                        new XAttribute("Range", animations.RandomTable[i][5])));
                                    break;
                                case v9AnimCurveTargetByteFloat.Alpha0Primary:
                                case v9AnimCurveTargetByteFloat.Alpha0Secondary:
                                case v9AnimCurveTargetByteFloat.Alpha1Primary:
                                case v9AnimCurveTargetByteFloat.Alpha1Secondary:
                                    rndClrElem.Add(new XElement("A",
                                        new XAttribute("Value", animations.RandomTable[i][0]),
                                        new XAttribute("Range", animations.RandomTable[i][1])));
                                    break;
                                default:
                                    break;
                            }
                        }

                        rndClrElem.Add(new XElement("Raw" + i, ByteArrayToString(entry)));

                        randomTableElem.Add(rndClrElem);
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

        public static string UintArrayToString(uint[] ua)
        {
            StringBuilder hex = new StringBuilder(ua.Length * 2);
            foreach (uint u in ua)
                hex.AppendFormat("{0:X8} ", u);
            return hex.ToString();
        }

        public static string FloatArrayToString(float[] fa)
        {
            StringBuilder floats = new StringBuilder(fa.Length * 2);
            foreach (float f in fa)
                floats.AppendFormat("{0} ", f);
            return floats.ToString();
        }

        private static void WriteREMT(REFFEntryNode reff, XElement root)
        {
            REFFEmitterNode9 emitter = reff.Children[0] as REFFEmitterNode9;
            REFFParticleNode particle = reff.Children[1] as REFFParticleNode;
            List<EffectAnim> animations = new List<EffectAnim>();
            foreach (REFFAnimationNode anim in reff.Children[2].Children)
            {
                animations.Add(new EffectAnim(anim));
            }

            root.Add(new XElement("mVersion", "1.6.4.15"));
            root.Add(new XElement("mEmitterName", reff.Name));
            root.Add(new XElement("mCreateOperator", "master"));
            root.Add(new XElement("mCreateDate", DateTime.Now));
            root.Add(new XElement("mEditOperator", "master"));
            root.Add(new XElement("mEditDate", DateTime.Now));
            root.Add(new XElement("mTemplateComment"));


            var mInit_lighting = new XElement("mInit_lighting");

            mInit_lighting.Add(new XElement("lightType", emitter.Mode));
            mInit_lighting.Add(new XElement("suspectType", emitter.LightType));
            mInit_lighting.Add(new XElement("radius",
                new XElement("val", emitter.Radius),
                new XElement("valMax", 100),
                new XElement("valMin", 0)));
            mInit_lighting.Add(new XElement("diffuseColor",
                new XElement("r", emitter.Diffuse.R),
                new XElement("g", emitter.Diffuse.G),
                new XElement("b", emitter.Diffuse.B),
                new XElement("a", emitter.Diffuse.A)));
            mInit_lighting.Add(new XElement("ambientColor",
                new XElement("r", emitter.Ambient.R),
                new XElement("g", emitter.Ambient.G),
                new XElement("b", emitter.Ambient.B),
                new XElement("a", emitter.Ambient.A)));
            mInit_lighting.Add(new XElement("translate",
                new XElement("x",
                    new XElement("val", emitter.Position._x),
                    new XElement("valMax", 200),
                    new XElement("valMin", -200)),
                new XElement("y",
                    new XElement("val", emitter.Position._y),
                    new XElement("valMax", 200),
                    new XElement("valMin", -200)),
                new XElement("z",
                    new XElement("val", emitter.Position._z),
                    new XElement("valMax", 200),
                    new XElement("valMin", -200))));



            var mInit_fog = new XElement("mInit_fog");

            mInit_fog.Add(new XElement("useFog", emitter.mFlags.HasFlag(DrawFlag.FogEnable)));


            var mInit_blendMode = new XElement("mInit_blendMode");
            mInit_blendMode.Add(new XElement("settingType", "MANUAL"));
            mInit_blendMode.Add(new XElement("blendTypeP", "BLEND"));
            mInit_blendMode.Add(new XElement("blendTypeM", emitter.BlendType));
            mInit_blendMode.Add(new XElement("inCoefficient", emitter.SrcFactor));
            mInit_blendMode.Add(new XElement("outCoefficient", emitter.DstFactor));
            mInit_blendMode.Add(new XElement("operationExpression", emitter.Operation));


            var mInit_TEV = new XElement("mInit_TEV");

            var mInit_ZCompare = new XElement("mInit_ZCompare");

            var mInit_clipping = new XElement("mInit_clipping");

            var mInit_drawOrder = new XElement("mInit_drawOrder");

            var mInit_drawSwitch = new XElement("mInit_drawSwitch");

            var mInit_repeatability = new XElement("mInit_repeatability");

            var mEmitter_emitter = new XElement("mEmitter_emitter");

            var mEmitter_transform = new XElement("mEmitter_transform");

            var mEmitter_chase = new XElement("mEmitter_chase");

            var mEmitter_userData = new XElement("mEmitter_userData");

            var mEmitter_speed = new XElement("mEmitter_speed");

            var mEmitter_volume = new XElement("mEmitter_volume");

            var mEmitter_time = new XElement("mEmitter_time");

            var mParticle_life = new XElement("mParticle_life");

            var mParticle_srt = new XElement("mParticle_srt");

            var mParticle_shape = new XElement("mParticle_shape");

            var mParticle_child = new XElement("mParticle_child");

            var mColor_layer1 = new XElement("mColor_layer1");

            var mColor_layer2 = new XElement("mColor_layer2");

            var mColor_swing = new XElement("mColor_swing");

            var mColor_compare = new XElement("mColor_compare");

            var mTexture_layer1 = new XElement("mTexture_layer1");

            var mTexture_layer2 = new XElement("mTexture_layer2");

            var mTexture_indirect = new XElement("mTexture_indirect");

            var mScale_scale = new XElement("mScale_scale");

            var mRotation_rotation = new XElement("mRotation_rotation");

            var mField = new XElement("mField");

            var mPostField = new XElement("mPostField");



            root.Add(mInit_lighting);

            root.Add(mInit_fog);

            root.Add(mInit_blendMode);

            root.Add(mInit_TEV);

            root.Add(mInit_ZCompare);

            root.Add(mInit_clipping);

            root.Add(mInit_drawOrder);

            root.Add(mInit_drawSwitch);

            root.Add(mInit_repeatability);

            root.Add(mEmitter_emitter);

            root.Add(mEmitter_transform);

            root.Add(mEmitter_chase);

            root.Add(mEmitter_userData);

            root.Add(mEmitter_speed);

            root.Add(mEmitter_volume);

            root.Add(mEmitter_time);

            root.Add(mParticle_life);

            root.Add(mParticle_srt);

            root.Add(mParticle_shape);

            root.Add(mParticle_child);

            root.Add(mColor_layer1);

            root.Add(mColor_layer2);

            root.Add(mColor_swing);

            root.Add(mColor_compare);

            root.Add(mTexture_layer1);

            root.Add(mTexture_layer2);

            root.Add(mTexture_indirect);

            root.Add(mScale_scale);

            root.Add(mRotation_rotation);

            root.Add(mField);

            root.Add(mPostField);
        }
    }
}

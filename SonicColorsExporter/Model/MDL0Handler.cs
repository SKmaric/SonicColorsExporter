using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.Modeling.Collada;
using HedgeLib.Headers;
using HedgeLib.Materials;
using HedgeLib.Textures;
using HedgeLib.IO;
using HedgeLib.Exceptions;

namespace SonicColorsExporter.Model
{
    internal class MDL0Handler
    {
        public static void convertMDL0toDAE(MDL0Node model, string outPath, SettingsFlags flags)
        {
            if (outPath.ToUpper().EndsWith(".DAE"))
            {
                ColladaExportColors.Serialize(model, outPath, flags.scaleMode, flags.singleBindMode, flags.multimatCombine, flags.tagMat, flags.tagObj, flags.UVOrganize, flags.lightmapMatMerge, flags.opaAddGeo);
            }
            else
            {
                var raw = (BRESEntryNode)model;
                raw.Export(outPath);
            }
        }

        #region Materials
        public void convertMDL0toMaterials(MDL0Node model, string outpath)
        {

            //Shader flags
            string[] blendShaders = { "OpaOpa", "OpaOpaMul" };
            string[] lumiShaders = { "LuminouseO", "LuminouseP", "LuminouseT", "LuminouseAdd", "MatLuminouseO", "MatLuminouseAdd", "WetFloorLumi" };
            string[] lightDifShaders = { "LightOpacity", "LightPunch", "LightSpcO", "LightProOMask", "LightProMaskP", "LightRefOMask" };
            string[] difEnv0Shaders = { "Ref", "RefT", "GlassOpa", "GlassOpa2" };

            string[] spc0Shaders = { "Glass1Pro" };
            string[] spc1Shaders = { "SpcO", "SpcMul", "SpcO2", "SpcO2Mul", "SpcP", "SpcPMul", "SpcT", "LightSpcO", "RefOMask", "RefOMaskMul", "RefMaskP", "LightRefOMask", "ProOMask", "ProOMaskMul", "ProMaskP", "ProMaskPMul", "LightProOMask", "LightProMaskP" };

            string[] mask0Shaders = { "Punch", "PunchMul", "Trans", "SpcP", "SpcT", "Add", "LuminouseP", "LuminouseT", "LuminouseAdd", "MatLuminouseAdd", "RefMaskP", "ProMaskP", "ProMaskPMul", "LightPunch", "LightProMaskP", "WetFloorP", "WetGlow" };
            string[] mask1Shaders = { "PunchMask", "TransMask", "AddMask2" };
            string[] mask2Shaders = { "WaterFall", "OilFall" };

            string[] ref0Shaders = { "Glass2", "Glass2Pro" };
            string[] ref1Shaders = { "RefO" };
            string[] ref2Shaders = { "RefOMask", "RefOMaskMul", "RefMaskP", "LightRefOMask", "GlassOpa" };

            string[] pro1Shaders = { "ProO", "Glass1Pro", "Glass2Dif", "Choco" };
            string[] pro2Shaders = { "ProOMask", "ProOMaskMul", "ProMaskP", "ProMaskPMul", "LightProOMask", "LightProMaskP", "Poison" };

            string[] bump1Shaders = { "GlassOpa", "GlassOpa2", "Glass2", "Glass2Pro", "Poison" };
            string[] bump2shaders = { "Glass2Dif", "Choco", "Oil" };

            string[] disp1Shaders = { "Water", "WetFloor", "WetFloorLumi", "WetFloorP", "WetGlow", "Poison", "Oil", "OilFall" };

            string[] emis1Shaders = { "OpaAdd", "OpaAdd", "OpaAddMul" };

            //for (int i = 0; i < model.MaterialList.Count; i++)
            foreach (MDL0MaterialNode srcMat in model._matGroup.Children)
            {
                if (srcMat.Name.Contains("@LYR(trans)") || srcMat.Name.Contains("@LYR(punch)"))
                    srcMat.Name = srcMat.Name.Remove(srcMat.Name.Length - 11, 11);

                string outfile = outpath + "\\" + srcMat.Name + ".material";
                GensMaterial Material = new GensMaterial();

                //Spc shader
                //if (srcMat.Name.Contains("_SpcP_") || srcMat.Name.Contains("_SpcO_") || srcMat.Name.Contains("_SpcMul_") || srcMat.Name.Contains("_SpcO2Mul_") || srcMat.Name.Contains("_SpcO2_"))
                //{
                //    Material.ShaderName = "Common_dp";
                //    Material.SubShaderName = "Common_dp";

                //    //diffuse
                //    GensTexture tex = new GensTexture();
                //    tex.Name = srcMat.Name + "-0000";
                //    tex.TextureName = srcMat.Children[0].Name;
                //    tex.Type = "diffuse";

                //    Material.Texset.Textures.Add(tex);

                //    //diffuse
                //    GensTexture tex2 = new GensTexture();
                //    tex2.Name = srcMat.Name + "-0001";
                //    tex2.TextureName = srcMat.Children[1].Name;
                //    tex2.Type = "gloss";

                //    Material.Texset.Textures.Add(tex2);
                //}
                //else
                //{
                if (srcMat.Children.Count > 0)
                {
                    //diffuse
                    GensTexture tex = new GensTexture();
                    tex.Name = srcMat.Name + "-0000";
                    tex.TextureName = srcMat.Children[0].Name;
                    tex.Type = "diffuse";

                    Material.Texset.Textures.Add(tex);
                }
                //}

                GensMaterial.Parameter param = new GensMaterial.Parameter();
                param.Name = "diffuse";
                param.ParamFlag1 = 512;
                param.ParamFlag2 = 256;
                param.Value = new HedgeLib.Vector4(1, 1, 1, 0);
                Material.Parameters.Add(param);

                GensMaterial.Parameter param2 = new GensMaterial.Parameter();
                param2.Name = "ambient";
                param2.ParamFlag1 = 512;
                param2.ParamFlag2 = 256;
                param2.Value = new HedgeLib.Vector4(1, 1, 1, 0);
                Material.Parameters.Add(param2);

                GensMaterial.Parameter param3 = new GensMaterial.Parameter();
                param3.Name = "specular";
                param3.ParamFlag1 = 512;
                param3.ParamFlag2 = 256;
                param3.Value = new HedgeLib.Vector4(1, 1, 1, 0);
                Material.Parameters.Add(param3);

                GensMaterial.Parameter param4 = new GensMaterial.Parameter();
                param4.Name = "emissive";
                param4.ParamFlag1 = 512;
                param4.ParamFlag2 = 256;
                param4.Value = new HedgeLib.Vector4(0, 0, 0, 0);
                Material.Parameters.Add(param4);

                GensMaterial.Parameter param5 = new GensMaterial.Parameter();
                param5.Name = "power_gloss_level";
                param5.ParamFlag1 = 512;
                param5.ParamFlag2 = 256;
                param5.Value = new HedgeLib.Vector4(50, 0.01f, 0, 0);
                Material.Parameters.Add(param5);

                GensMaterial.Parameter param6 = new GensMaterial.Parameter();
                param6.Name = "opacity_reflection_refraction_spectype";
                param6.ParamFlag1 = 512;
                param6.ParamFlag2 = 256;
                param6.Value = new HedgeLib.Vector4(1, 0, 1, 0);
                Material.Parameters.Add(param6);


                //apply matched properties
                //Additive Blending
                if (srcMat.DstFactor == BrawlLib.Wii.Graphics.BlendFactor.One)
                    Material.AdditiveBlending = true;
                //Backface Culling
                if (srcMat.CullMode == BrawlLib.SSBB.Types.CullMode.Cull_None)
                    Material.NoBackFaceCulling = true;
                //Fog ignore
                if (srcMat._fogIndex == -1)
                {
                    GensMaterial.Parameter param7 = new GensMaterial.Parameter();
                    param7.Name = "g_LightScattering_Ray_Mie_Ray2_Mie2";
                    Material.Parameters.Add(param7);
                }
                Material.Header = new GensHeader { RootNodeType = 3 };
                //UpdateMaterial(Material);
                Material.Save(outfile, true);

            }
        }

        [Serializable]
        public class SerializableNode
        {
            // Variables/Constants
            public string Name
            {
                get => name;
                set
                {
                    if (value.Length > MirageHeader.Node.NameLength)
                    {
                        //GUI.ShowErrorBox(
                        //    "ERROR: MirageNode names cannot contain more than 8 characters!");
                    }
                    else
                    {
                        name = value;
                    }
                }
            }

            public List<SerializableNode> Children { get => children; set => value = children; }
            public uint DataSize { get => node.DataSize; set => value = node.DataSize; }
            public uint Value { get => node.Value; set => value = node.DataSize; }

            protected List<SerializableNode> children = new List<SerializableNode>();
            protected MirageHeader.Node node;
            protected string name = string.Empty;

            // Constructors
            public SerializableNode()
            {
                node = new MirageHeader.Node();
            }

            public SerializableNode(MirageHeader.Node node)
            {
                this.node = node;
                name = node.Name;

                foreach (var child in node.Nodes)
                {
                    children.Add(new SerializableNode(child));
                }
            }

            // Methods
            public MirageHeader.Node GetNode()
            {
                node.Name = name;
                node.Nodes.Clear();

                foreach (var child in children)
                {
                    node.Nodes.Add(child.GetNode());
                }

                return node;
            }

            public override string ToString()
            {
                return name;
            }
        }
        #endregion
    }
}

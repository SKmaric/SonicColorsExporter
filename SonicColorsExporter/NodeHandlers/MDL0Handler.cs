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

namespace SonicColorsExporter
{
    internal class MDL0Handler
    {
        public static void convertMDL0toDAE(MDL0Node model, string outPath, SettingsFlags flags)
        {
            if (outPath.ToUpper().EndsWith(".DAE"))
            {
                ColladaExportColors.Serialize(model, outPath, flags.scaleMode, flags.singleBindMode, flags.multimatCombine, flags.tagMat, flags.tagObj, flags.UVOrganize, flags.lightmapMatMerge, flags.opaAddGeo);
                //Collada.Serialize(model, outPath);
            }
            else
            {
                var raw = (BRESEntryNode)model;
                raw.Export(outPath);
            }
        }

        #region Materials
        public static void debugListMaterialNames(MDL0Node model, string outpath)
        {
            string file = outpath + "\\matnames.txt";

            var matGroup = model.FindChildrenByName("Materials");

            if (matGroup.Length > 0)
            {
                using (StreamWriter sw = File.AppendText(file))
                {
                    sw.WriteLine(model.Parent.Parent.Name + "\\" + model.Name + ".mdl0");
                    sw.WriteLine("");
                    foreach (MDL0MaterialNode srcMat in matGroup[0].Children)
                    {
                        sw.WriteLine(srcMat.Name);
                    }
                    sw.WriteLine("");
                }
            }
        }
        public static void convertMDL0toMaterials(MDL0Node model, string outpath)
        {
            // root shader types
            string[] rootShaders = { 
                // Standard
                "Opacity", "Luminouse", "Pro", "Ref", "Spc", "Add", 
                // Special
                "Illumination", "Sky", "Glass", "MultiWater", "Water", "WetFloor", "WetGlow", "Oil", "Poison", "Monolith", "ClodColor", "Ring", "Plastic",
                // chr / en / obj
                "Eye", "SuperFur", "Fur", "Homing", "Metal", "Mouth", "Phantom", "PixyEye", "PixyWait", "Pixy", "Rbox", "Base"
                };
            string[] shaderSubTypes = { "Mask", "Light", "Decal", "Fall", "NoTex", "Mul" };

            //Shader flags
            string[] blendShaders = { "OpaOpa", "OpaOpaMul" };
            string[] lumiShaders = { "LuminouseO", "LuminouseP", "LuminouseT", "LuminouseAdd", "MatLuminouseO", "MatLuminouseAdd", "WetFloorLumi" };

            // Indicators for which map in Colors' shaders corresponds to each texture map type 
            List<string[]> spcShaders = new List<string[]>();
            spcShaders.Add(new string[] { "Glass1Pro" });
            spcShaders.Add(new string[] { "SpcO", "SpcMul", "SpcO2", "SpcO2Mul", "SpcP", "SpcPMul", "SpcT", "LightSpcO", "RefOMask", "RefOMaskMul", "RefMaskP", "LightRefOMask", "ProOMask", "ProOMaskMul", "ProMaskP", "ProMaskPMul", "LightProOMask", "LightProMaskP", "enBase" });
            spcShaders.Add(new string[] { });
            spcShaders.Add(new string[] { "enMetal", "enMetalPunch" });

            List<string[]> sprShaders = new List<string[]>();
            sprShaders.Add(new string[] { });
            sprShaders.Add(new string[] { "Glass" });
            sprShaders.Add(new string[] { "enMetal", "enMetalPunch", "enGlass2", "enRbox" });
            sprShaders.Add(new string[] { });

            List<string[]> maskShaders = new List<string[]>();
            maskShaders.Add(new string[] { "Punch", "PunchMul", "Trans", "SpcP", "SpcT", "Add", "LuminouseP", "LuminouseT", "LuminouseAdd", "MatLuminouseAdd", "RefMaskP", "ProMaskP", "ProMaskPMul", "LightPunch", "LightProMaskP", "WetFloorP", "WetGlow" });
            maskShaders.Add(new string[] { "PunchMask", "TransMask", "AddMask2" });
            maskShaders.Add(new string[] { "WaterFall", "OilFall" });
            maskShaders.Add(new string[] { });

            List<string[]> refShaders = new List<string[]>();
            refShaders.Add(new string[] { "Ref", "RefT", "GlassOpa", "GlassOpa2", "Glass2", "Glass" });
            refShaders.Add(new string[] { "RefO", "enMetal", "enMetalPunch", "enGlass2" });
            refShaders.Add(new string[] { "RefOMask", "RefOMaskMul", "RefMaskP", "LightRefOMask", "GlassOpa" });
            refShaders.Add(new string[] { });

            List<string[]> proShaders = new List<string[]>();
            proShaders.Add(new string[] { "Glass2Pro" });
            proShaders.Add(new string[] { "ProO", "Glass1Pro", "Glass2Dif", "Choco" });
            proShaders.Add(new string[] { "ProOMask", "ProOMaskMul", "ProMaskP", "ProMaskPMul", "LightProOMask", "LightProMaskP", "Poison" });
            proShaders.Add(new string[] { });

            List<string[]> bumpShaders = new List<string[]>();
            bumpShaders.Add(new string[] { });
            bumpShaders.Add(new string[] { "GlassOpa", "GlassOpa2", "Glass2", "Glass2Pro", "Poison", "GlassLumi" });
            bumpShaders.Add(new string[] { "Glass2Dif", "Choco", "Oil" });
            bumpShaders.Add(new string[] { });

            List<string[]> dispShaders = new List<string[]>();
            dispShaders.Add(new string[] { });
            dispShaders.Add(new string[] { "Water", "WetFloor", "WetFloorLumi", "WetFloorP", "WetGlow", "Poison", "Oil", "OilFall", "enRbox" });
            dispShaders.Add(new string[] { });
            dispShaders.Add(new string[] { });

            List<string[]> emisShaders = new List<string[]>();
            emisShaders.Add(new string[] { });
            emisShaders.Add(new string[] { "OpaAdd", "OpaAdd", "OpaAddMul", "Mul" });
            emisShaders.Add(new string[] { });
            emisShaders.Add(new string[] { });

            //for (int i = 0; i < model.MaterialList.Count; i++)

            var matGroup = model.FindChildrenByName("Materials");

            if (matGroup.Length < 1)
            {
                return;
            }

            foreach (MDL0MaterialNode srcMat in matGroup[0].Children)
            {
                bool isNonStandardName = false;
                bool reflectionType = false; // 0 = spherical, 1 = projection

                if (srcMat.Name.Contains("@LYR(trans)") || srcMat.Name.Contains("@LYR(punch)"))
                    srcMat.Name = srcMat.Name.Remove(srcMat.Name.Length - 11, 11);

                string outfile = outpath + "\\" + srcMat.Name + ".material";

                string[] separatingStrings = { "__" };
                var matNameParts = srcMat.Name.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                string matNameBase = matNameParts[0];
                string matNameShader = matNameParts[1]; // Full original name

                string matPrefix = "";
                string matNameShaderNoPrefix;
                string matNameShaderRoot; // Base type extrapolated from matNameShader
                List<string> matSubTypes = new List<string>(); // Extrapolated from the rest of matNameShader

                string matNameShaderBase = matNameShader; //For processing name

                switch (matNameShaderBase)
                {
                    case string s when s.StartsWith("2x"): //Loading Screen
                        matPrefix = "2x";
                        matNameShaderBase = matNameShaderBase.Substring(2);
                        break;
                    case string s when s.StartsWith("ch"):
                        matPrefix = "ch";
                        matNameShaderBase = matNameShaderBase.Substring(2);
                        break;
                    case string s when s.StartsWith("cmn"):
                        matPrefix = "cmn";
                        matNameShaderBase = matNameShaderBase.Substring(3);
                        break;
                    case string s when s.StartsWith("eff"):
                        matPrefix = "eff";
                        matNameShaderBase = matNameShaderBase.Substring(3);
                        break;
                    case string s when s.StartsWith("en"):
                        matPrefix = "en";
                        matNameShaderBase = matNameShaderBase.Substring(2);
                        break;
                    case string s when s.StartsWith("obj"):
                        matPrefix = "obj";
                        matNameShaderBase = matNameShaderBase.Substring(2);
                        break;
                    default:
                        break;
                }

                matNameShaderNoPrefix = matNameShaderBase;

                // seems to be for ObjColor00 shader
                if (!Char.IsUpper(matNameShaderBase, 0))
                    isNonStandardName = true;

                if (matNameShaderBase.StartsWith("Light"))
                {
                    matSubTypes.Add("Light");
                    matNameShaderBase = matNameShaderBase.Substring(5);
                }

                if (matNameShaderBase.StartsWith("Mat"))
                {
                    matSubTypes.Add("MatLumi");
                    matNameShaderBase = matNameShaderBase.Substring(3);
                }


                // Get the root shader type and subtypes identifiers
                foreach (string shaderType in rootShaders)
                {
                    if (matNameShaderBase.StartsWith(shaderType))
                    {
                        matNameShaderRoot = shaderType;
                        matNameShaderBase = matNameShaderBase.Substring(shaderType.Length);
                    }
                    else if (matNameShaderBase.StartsWith("Opa"))
                    {
                        matNameShaderRoot = "Opacity";
                        matNameShaderBase = matNameShaderBase.Substring(3);
                    }
                    else if (matNameShaderBase.StartsWith("Punch") || matNameShaderBase.StartsWith("Trans"))
                    {
                        matNameShaderRoot = "Opacity";
                        matNameShaderBase = matNameShaderBase.Substring(5);
                    }
                }

                foreach (string shaderSubType in shaderSubTypes)
                {
                    if (matNameShaderBase.Contains(shaderSubType))
                    {
                        matSubTypes.Add(shaderSubType);
                    }
                }

                foreach (string shaderType in rootShaders)
                {
                    if (matNameShaderBase.Contains(shaderType))
                    {
                        matSubTypes.Add(shaderType);
                    }
                    else if (matNameShaderBase.StartsWith("Opa"))
                    {
                        matSubTypes.Add("Opacity");
                    }
                }

                GensMaterial Material = new GensMaterial();

                // add diffuse texture map first
                bool diffusePlaceholder = false;

                if (spcShaders[0].Contains(matNameShader))
                    diffusePlaceholder = true;
                else if (bumpShaders[0].Contains(matNameShader))
                    diffusePlaceholder = true;
                else if (refShaders[0].Contains(matNameShader))
                    diffusePlaceholder = true;
                else if (proShaders[0].Contains(matNameShader))
                    diffusePlaceholder = true;
                else if (dispShaders[0].Contains(matNameShader))
                    diffusePlaceholder = true;
                else if (emisShaders[0].Contains(matNameShader))
                    diffusePlaceholder = true;

                if (diffusePlaceholder)
                {
                    Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, 4, "gi-dummy-black", "diffuse"));
                }
                else if (srcMat.Children.Count > 0)
                {
                    //diffuse
                    Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, 0, srcMat.Children[0].Name, "diffuse"));
                    if (maskShaders[0].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, 4, srcMat.Children[0].Name, "diffuse"));
                }


                // add other known texture maps
                for (int i = 0; i < 4; i++)
                {
                    if (i > 0)
                    {
                        if (maskShaders[i].Contains(matNameShader))
                            Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "opacity"));
                    }
                    if (spcShaders[i].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "gloss"));
                    else if (bumpShaders[i].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "normal"));
                    else if (refShaders[i].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "reflection"));
                    else if (proShaders[i].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "reflection"));
                    else if (dispShaders[i].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "displacement"));
                    else if (emisShaders[i].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "displacement"));
                    else if (sprShaders[i].Contains(matNameShader))
                        Material.Texset.Textures.Add(WriteGensTexture(srcMat.Name, i, srcMat.Children[i].Name, "specular"));
                }

                string gensShaderName = GetColors2GensShaderName(matNameShader);

                Material.ShaderName = gensShaderName;
                Material.SubShaderName = gensShaderName;

                // write parameters

                GensMaterial.Parameter param = new GensMaterial.Parameter();
                param.Name = "diffuse";
                param.ParamFlag1 = 512;
                param.ParamFlag2 = 256;
                if (matSubTypes.Contains("Light"))
                    param.Value = new HedgeLib.Vector4(2, 2, 2, 0);
                else
                    param.Value = new HedgeLib.Vector4(1, 1, 1, 0);
                Material.Parameters.Add(param);

                GensMaterial.Parameter param2 = new GensMaterial.Parameter();
                param2.Name = "ambient";
                param2.ParamFlag1 = 512;
                param2.ParamFlag2 = 256;
                if (matSubTypes.Contains("Light"))
                    param2.Value = new HedgeLib.Vector4(2, 2, 2, 0);
                else
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

        public static GensTexture WriteGensTexture(string matName, int mapID, string texName, string mapType)
        {
            GensTexture tex = new GensTexture();

            tex.Name = matName + "-" + mapID.ToString("D4");
            tex.TextureName = texName;
            tex.Type = mapType;

            return tex;
        }

        public static string GetColors2GensShaderName(string input)
        {
            Dictionary<string, string> shaders = new Dictionary<string, string>();
            shaders["Add"] =               "Common_d";
            shaders["AddMask2"] =          "Common_d";
            shaders["AddMul"] =            "Common_d";
            shaders["AddNoTex"] =          "Common_d";
            shaders["Choco"] =             "Common_d";
            shaders["ClodColor"] =         "Common_d";
            shaders["ClodDepth"] =         "Common_d";
            shaders["Core"] =              "Common_d";
            shaders["Glass1Pro"] =         "Common_dpe";
            shaders["Glass2"] =            "Common_dne1";
            shaders["Glass2Dif"] =         "Common_dne";
            shaders["Glass2Pro"] =         "Common_dne";
            shaders["GlassOpa"] =          "Common_dne1";
            shaders["GlassOpa2"] =         "Common_dne1";
            shaders["Illumination"] =      "Common_d";
            shaders["Illumination2"] =     "Common_d";
            shaders["LightClodColor"] =    "Common_d";
            shaders["LightMonolith"] =     "Common_d";
            shaders["LightMultiCube"] =    "Common_d";
            shaders["LightOpacity"] =      "Common_d";
            shaders["LightProMaskP"] =     "Common_dpe";
            shaders["LightProO"] =         "Common_de";
            shaders["LightProOMask"] =     "Common_dpe";
            shaders["LightPunch"] =        "Common_d";
            shaders["LightRefO"] =         "Common_de1";
            shaders["LightRefOMask"] =     "Common_dpe1";
            shaders["LightSpcO"] =         "Common_dp";
            shaders["LuminouseAdd"] =      "Luminescence_d";
            shaders["LuminouseAddMask"] =  "Luminescence_d";
            shaders["LuminouseO"] =        "Luminescence_d";
            shaders["LuminouseP"] =        "Luminescence_d";
            shaders["LuminouseT"] =        "Luminescence_d";
            shaders["MatLuminouseAdd"] =   "Luminescence_d";
            shaders["MatLuminouseO"] =     "Luminescence_d";
            shaders["MatLuminouseP"] =     "Luminescence_d";
            shaders["MatLuminouseT"] =     "Luminescence_d";
            shaders["Mul"] =               "Common_d";
            shaders["MulNoTex"] =          "Common_d";
            shaders["MultiWater"] =        "Common_d";
            shaders["MultiWaterAdd"] =     "Common_d";
            shaders["Oil"] =               "Common_d";
            shaders["OilFall"] =           "Common_d";
            shaders["OpaAdd"] =            "Common_d";
            shaders["OpaAdd2"] =           "Common_d";
            shaders["OpaAddMul"] =         "Common_d";
            shaders["OpaDecal"] =          "Common_d";
            shaders["OpaDecal2"] =         "Common_d";
            shaders["OpaMul"] =            "Common_d";
            shaders["OpaOpa"] =            "Blend_dd";
            shaders["OpaOpaMul"] =         "Blend_dd";
            shaders["Opacity"] =           "Common_d";
            shaders["Poison"] =            "Common_d";
            shaders["ProDecal"] =          "Common_d";
            shaders["ProMaskP"] =          "Common_dpe";
            shaders["ProMaskPMul"] =       "Common_dpe";
            shaders["ProO"] =              "Common_de";
            shaders["ProOMask"] =          "Common_dpe";
            shaders["ProOMaskMul"] =       "Common_dpe";
            shaders["Punch"] =             "Common_d";
            shaders["PunchMask"] =         "Common_d";
            shaders["PunchMul"] =          "Common_d";
            shaders["Ref"] =               "Common_de1";
            shaders["RefMaskP"] =          "Common_dpe1";
            shaders["RefO"] =              "Common_de1";
            shaders["RefOMask"] =          "Common_dpe1";
            shaders["RefOMask2"] =         "Common_d";
            shaders["RefOMaskMul"] =       "Common_d";
            shaders["RefSpcO"] =           "Common_dpe1";
            shaders["RefT"] =              "Common_de1";
            shaders["SkyAdd"] =            "Common_d";
            shaders["SpcMul"] =            "Common_dp";
            shaders["SpcO"] =              "Common_dp";
            shaders["SpcO2"] =             "Common_dp";
            shaders["SpcO2Mul"] =          "Common_dp";
            shaders["SpcP"] =              "Common_dp";
            shaders["SpcPMul"] =           "Common_dp";
            shaders["SpcT"] =              "Common_dp";
            shaders["StarLightRoad"] =     "Common_d";
            shaders["SubNoTex"] =          "Common_d";
            shaders["Trans"] =             "Common_d";
            shaders["TransMask"] =         "Common_d";
            shaders["TransMaskAdd"] =      "Common_d";
            shaders["TransTMask"] =        "Common_d";
            shaders["Water"] =             "Common_d";
            shaders["WaterFall"] =         "Common_d";
            shaders["WetFloor"] =          "Common_d";
            shaders["WetFloorLumi"] =      "Common_d";
            shaders["WetFloorP"] =         "Common_d";
            shaders["WetGlow"] =           "Common_d";
            // chr
            shaders["chEye"] =             "Common_d";
            shaders["chFur"] =             "Common_d";
            shaders["chFurPunch"] =        "Common_d";
            shaders["chHoming"] =          "Common_d";
            shaders["chMetal"] =           "Common_d";
            shaders["chMetalLumi"] =       "Common_d";
            shaders["chMii"] =             "Common_d";
            shaders["chMouth"] =           "Common_d";
            shaders["chPhantom"] =         "Common_d";
            shaders["chPhantom2"] =        "Common_d";
            shaders["chPhantom3"] =        "Common_d";
            shaders["chPixy1"] =           "Common_d";
            shaders["chPixy2"] =           "Common_d";
            shaders["chPixy3"] =           "Common_d";
            shaders["chPixy4"] =           "Common_d";
            shaders["chPixy5"] =           "Common_d";
            shaders["chPixy6"] =           "Common_d";
            shaders["chPixyEye"] =         "Common_d";
            shaders["chPixyWait1"] =       "Common_d";
            shaders["chPixyWait2"] =       "Common_d";
            shaders["chPixyWait3"] =       "Common_d";
            shaders["chRef"] =             "Common_d";
            shaders["chSuperFur"] =        "Common_d";
            shaders["chSuperFurPunch"] =   "Common_d";
            // cmn
            shaders["cmnAdd"] =            "Common_d";
            shaders["cmnBase"] =           "Common_d";
            shaders["cmnGlassAddLumi"] =   "Common_d";
            shaders["cmnGlassLumi"] =      "Luminescence_dnE";
            shaders["cmnGlassOpa"] =       "Common_d";
            shaders["cmnIgnL"] =           "Common_d";
            shaders["cmnLSPunch"] =        "Common_d";
            shaders["cmnLightSpc"] =       "Common_d";
            shaders["cmnLumiGlassOpa"] =   "Luminescence_d";
            shaders["cmnLuminouse"] =      "Luminescence_d";
            shaders["cmnLuminouseAdd"] =   "Luminescence_d";
            shaders["cmnMulLumiAdd"] =     "Luminescence_d";
            shaders["cmnOpacity"] =        "Common_d";
            shaders["cmnPlasticOpa"] =     "Common_d";
            shaders["cmnPunch"] =          "Common_d";
            shaders["cmnPunchIgnL"] =      "Common_d";
            shaders["cmnRasMask"] =        "Common_d";
            shaders["cmnRbox"] =           "Common_d";
            shaders["cmnRboxLumi"] =       "Common_d";
            shaders["cmnRef"] =            "Common_d";
            shaders["cmnRef3"] =           "Common_d";
            shaders["cmnRef4"] =           "Common_d";
            shaders["cmnRing"] =           "Common_d";
            shaders["cmnTrans"] =          "Common_d";
            shaders["cmnTransIgnL"] =      "Common_d";
            shaders["cmnTransLum"] =       "Common_d";
            // eff
            shaders["effBoost"] =          "Common_d";
            // en
            shaders["enAdd"] =             "Common_d";
            shaders["enBase"] =            "Common_dp";
            shaders["enGlass"] =           "Common_dse1";
            shaders["enGlass2"] =          "Common_dse1";
            shaders["enLuminouseAdd"] =    "Luminescence_d";
            shaders["enLuminouseO"] =      "Luminescence_d";
            shaders["enMatLuminouseAdd"] = "Luminescence_d";
            shaders["enMatLuminouseO"] =   "Luminescence_d";
            shaders["enMatLuminouseO2"] =  "Luminescence_d";
            shaders["enMetal"] =           "Common_dspe";
            shaders["enMetalPunch"] =      "Common_dspe";
            shaders["enPunchIgnL"] =       "Common_d";
            shaders["enRbox"] =            "Common_d";
            shaders["enRboxLumi"] =        "Common_d";
            shaders["enTrans"] =           "Common_d";
            // obj
            shaders["objBase"] =           "Common_d";
            shaders["objGlass1"] =         "Common_d";
            shaders["objGlass2"] =         "Common_d";
            shaders["objHole"] =           "Common_d";
            shaders["objOpacity"] =        "Common_d";
            shaders["objProOMask"] =       "Common_d";
            shaders["objPunch"] =          "Common_d";
            shaders["objRefMaskP"] =       "Common_d";
            shaders["objRefO"] =           "Common_d";
            shaders["objRefOMask"] =       "Common_d";
            shaders["objSpcO"] =           "Common_d";
            shaders["ObjColor00"] =        "Common_d";

            if (shaders.ContainsKey(input))
                return shaders[input];
            else
                return "Common_d";
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

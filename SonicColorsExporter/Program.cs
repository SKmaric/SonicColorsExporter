using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;
using HedgeLib.Headers;
using HedgeLib.Materials;
using HedgeLib.Textures;
using HedgeLib.IO;
using HedgeLib.Exceptions;

namespace SonicColorsExporter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public static void ProcessFile(string infile, string outpath, SettingsFlags flags)
        {
            string ext = Path.GetExtension(infile).ToUpper();

            if (ext == ".ARC")
            {
                U8Node node = NodeFactory.FromFile(null, infile) as U8Node;
                ProcessARC(node, outpath, flags);
            }

            if (ext == ".BRRES")
            {
                BRRESNode node = NodeFactory.FromFile(null, infile) as BRRESNode;
                ProcessBRRES(node, outpath, flags);
            }

            if (ext == ".MDL0")
            {
                MDL0Node node = NodeFactory.FromFile(null, infile) as MDL0Node;
                string outfile = outpath + "\\" + Path.GetFileNameWithoutExtension(infile) + ".dae";
                MDL0Handler.convertMDL0toDAE(node, outfile, flags);
            }

            if (ext == ".SCN0")
            {
                SCN0Node node = NodeFactory.FromFile(null, infile) as SCN0Node;
                SCN0Handler.processSCN0(node, outpath, flags);
            }

            if (ext == ".SRT0")
            {
                SRT0Node node = NodeFactory.FromFile(null, infile) as SRT0Node;
                SRT0Handler.processSRT0(node, outpath, flags);
            }
        }

        public static void ProcessARC(U8Node arc, string outpath, SettingsFlags flags)
        {
            foreach (ARCEntryNode node in arc.Children[0].Children)
            {
                if (node is BRRESNode)
                {
                    BRRESNode brres = node as BRRESNode;
                    ProcessBRRES(brres, outpath, flags);
                }
                else if (node.Name == "particle_list.orc")
                {
                    node.ExportUncompressed(outpath + "\\" + node.Name);
                    BINAReader orcReader = new BINAReader(File.OpenRead(outpath + "\\" + node.Name));
                    BINAHeader orcHeader = orcReader.ReadHeader();

                    var particleList = REFFHandler.ReadEffectNames(orcReader);

                    orcReader.Close();

                    foreach (ARCEntryNode node2 in arc.Children[0].Children)
                    {
                        if (node2 is REFFNode)
                        {
                            REFFNode reff = node2 as REFFNode;
                            //processREFF(reff, outpath, particleList);
                        }
                    }
                }
                else if (node.Name == "stg_gismo_list.orc")
                {
                    node.ExportUncompressed(outpath + "\\" + node.Name);
                    BINAReader orcReader = new BINAReader(File.OpenRead(outpath + "\\" + node.Name));
                    BINAHeader orcHeader = orcReader.ReadHeader();

                    var gismoList = GISMHandler.ReadGismoList(orcReader);

                    orcReader.Close();

                    //writeGismObjectProd(gismoList, outpath + "\\ObjectProduction.phy.xml");
                }
            }
        }

        public static void ProcessBRRES(BRRESNode brres, string outpath, SettingsFlags flags)
        {
            foreach (BRESGroupNode group in brres.Children)
            {
                if (group.Type == BRESGroupNode.BRESGroupType.Models)
                {
                    foreach (MDL0Node model in group.Children)
                    {
                        string outfile = outpath + "\\" + model.Name + ".dae";
                        MDL0Handler.convertMDL0toDAE(model, outfile, flags);
                        //MDL0Handler.convertMDL0toMaterials(model, outpath);
                    }
                }
            }
        }
    }
}

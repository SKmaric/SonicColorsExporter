using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;
using HedgeLib.Headers;
using HedgeLib.IO;
using HedgeLib.Exceptions;


namespace SonicColorsExporter
{
    public partial class MainForm : Form
    {
        public bool scaleMode;
        public bool singleBindMode;
        public bool multimatCombine;
        public bool tagMat;
        public bool tagObj;
        public bool UVOrganize;
        public bool lightmapMatMerge;
        public bool opaAddGeo;

        public MainForm()
        {
            InitializeComponent();
            rad_Multi.Checked = true;
        }

        private void rad_Multi_CheckedChanged(object sender, EventArgs e)
        {
            if (rad_Multi.Checked)
            {
                txt_SrcFile.Enabled = false;
                btn_SrcFile.Enabled = false;
                txt_SrcPath.Enabled = true;
                btn_SrcPath.Enabled = true;
            }
            else
            {
                txt_SrcFile.Enabled = true;
                btn_SrcFile.Enabled = true;
                txt_SrcPath.Enabled = false;
                btn_SrcPath.Enabled = false;
            }
        }

        private void btn_SrcFile_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Open GISM File...",
                Filter = "All Supported Formats (*.arc;*.brres;*.mdl0)|*.arc;*.brres;*.mdl0|" +
                "U8 ARC File Archive (*.arc)|*.arc|" +
                "NW4R Resource Pack (*.brres)|*.brres|" +
                "NW4R Model (*.mdl0)|*.mdl0|" +
                "All files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
                txt_SrcFile.Text = ofd.FileName;
        }

        private void btn_OutPath_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog()
            {
                Description = "Set Output Folder...", // TODO
                SelectedPath = (Directory.Exists(txt_OutPath.Text)) ?
                    txt_OutPath.Text : null
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txt_OutPath.Text = fbd.SelectedPath;
            }
        }

        private void btn_SrcPath_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog()
            {
                Description = "Set Output Folder...", // TODO
                SelectedPath = (Directory.Exists(txt_SrcPath.Text)) ?
                    txt_SrcPath.Text : null
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txt_SrcPath.Text = fbd.SelectedPath;
            }
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            string outpath = txt_OutPath.Text;

            //Options
            scaleMode = chk_ScaleMode.Checked;
            singleBindMode = chk_SingleBind.Checked;
            multimatCombine = chk_MultimatCombine.Checked;
            tagMat = chk_TagMat.Checked;
            tagObj = chk_TagObj.Checked;
            UVOrganize = chk_UVOrganize.Checked;
            lightmapMatMerge = chk_LightmapMatMerge.Checked;
            opaAddGeo = chk_OpaAddGeo.Checked;

            //Check if output path exists
            if (!Directory.Exists(outpath))
            {
                MessageBox.Show("Output directory does not exist.");
                return;
            }

            //Single File Mode
            if (rad_Single.Checked)
            {
                string infile = txt_SrcFile.Text;

                //Check if file exists
                if (!File.Exists(infile))
                {
                    MessageBox.Show("File does not exist.");
                    return;
                }

                progressBar.Value = 0;
                progressBar.Maximum = 1;

                //lbl_currentFile.Text = Path.GetFileName(infile);
                processFile(infile, outpath);

                progressBar.Value = 1;

                MessageBox.Show("Finished. Be sure to convert to FBX using FbxConverterUI before importing to Max.");
                return;

            }
            else //Multi File Mode
            {
                string inpath = txt_SrcPath.Text;

                if (!Directory.Exists(inpath))
                {
                    MessageBox.Show("Source directory does not exist.");
                    return;
                }

                string[] files = Directory.GetFiles(inpath);

                progressBar.Value = 0;
                progressBar.Maximum = files.Length;

                foreach (string infile in files)
                {
                    //lbl_currentFile.Text = Path.GetFileName(infile);
                    processFile(infile, outpath);
                    progressBar.Value++;
                }

                MessageBox.Show("Finished. Be sure to convert to FBX using FbxConverterUI before importing to Max.");
                return;
            }
        }

        public void processFile(string infile, string outpath)
        {
            string ext = Path.GetExtension(infile).ToUpper();

            if (ext == ".ARC")
            {
                U8Node node = NodeFactory.FromFile(null, infile) as U8Node;
                processARC(node, outpath);
            }

            if (ext == ".BRRES")
            {
                BRRESNode node = NodeFactory.FromFile(null, infile) as BRRESNode;
                processBRRES(node, outpath);
            }

            if (ext == ".MDL0")
            {
                MDL0Node node = NodeFactory.FromFile(null, infile) as MDL0Node;
                string outfile = outpath + "\\" + Path.GetFileNameWithoutExtension(infile) + ".dae";
                convertMDL0toDAE(node, outfile);
            }
        }

        public void processARC(U8Node arc, string outpath)
        {
            foreach (ARCEntryNode node in arc.Children[0].Children)
            {
                if (node is BRRESNode)
                {
                    BRRESNode brres = node as BRRESNode;
                    processBRRES(brres, outpath);
                }
                else if (node.Name == "particle_list.orc")
                {
                    node.ExportUncompressed(outpath + "\\" + node.Name);
                    BINAReader orcReader = new BINAReader(File.OpenRead(outpath + "\\" + node.Name));
                    BINAHeader orcHeader = orcReader.ReadHeader();

                    var particleList = ReadEffectNames(orcReader);

                    orcReader.Close();

                    foreach (ARCEntryNode node2 in arc.Children[0].Children)
                    {
                        if (node2 is REFFNode)
                        {
                            REFFNode reff = node2 as REFFNode;
                            processREFF(reff, outpath, particleList);
                        }
                    }
                }
            }
        }

        public void processBRRES(BRRESNode brres, string outpath)
        {
            foreach (BRESGroupNode group in brres.Children)
            {
                if (group.Type == BRESGroupNode.BRESGroupType.Models)
                {
                    foreach (MDL0Node model in group.Children)
                    {
                        string outfile = outpath + "\\" + model.Name + ".dae";
                        convertMDL0toDAE(model, outfile);
                    }
                }
            }
        }

        public void convertMDL0toDAE(MDL0Node model, string outfile)
        {
            model.Export(outfile, scaleMode, singleBindMode, multimatCombine, tagMat, tagObj, UVOrganize, lightmapMatMerge, opaAddGeo);
        }
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

        public void processREFF(REFFNode reff, string outpath, List<string> particleList)
        {
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

                    writeEmitter(emitter, writer, 0);
                    int childNumber = 1;
                    foreach (REFFEntryNode child in effectChildren)
                    {
                        writeEmitter(child.Children[0] as REFFEmitterNode9, writer, childNumber);
                        childNumber++;
                    }

                    writeParticle(particle, writer, 0);
                    childNumber = 1;
                    foreach (REFFEntryNode child in effectChildren)
                    {
                        writeParticle(child.Children[1] as REFFParticleNode, writer, childNumber);
                        childNumber++;
                    }

                    writer.WriteEndElement(); //Effect
                    writer.WriteEndDocument();
                }
            }
        }

        private void writeEmitter(REFFEmitterNode9 emitter, XmlWriter writer, int id)
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
                writer.WriteAttributeString("X", "0");
                writer.WriteAttributeString("Y", "0");
                writer.WriteAttributeString("Z", "0");
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

        private void writeParticle(REFFParticleNode particle, XmlWriter writer, int id)
        {
            writer.WriteStartElement("Particle");
            writer.WriteAttributeString("Id", id.ToString());
            writer.WriteAttributeString("Name", particle.Parent.Name);
            writer.WriteAttributeString("Type", "Quad");

            {
                writer.WriteStartElement("LifeTime");
                writer.WriteAttributeString("Value", "30");
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
                writer.WriteAttributeString("X", "1");
                writer.WriteAttributeString("Y", "1");
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
                writer.WriteAttributeString("R", "1");
                writer.WriteAttributeString("G", "1");
                writer.WriteAttributeString("B", "1");
                writer.WriteAttributeString("A", "1");
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
                    writeAnimation(anim, writer, "ColorA");
                }
            }

            writer.WriteEndElement();
        }

        private void writeAnimation(REFFAnimationNode animation, XmlWriter writer, string type)
        {
            writer.WriteStartElement("Animation");
            writer.WriteAttributeString("Type", type);

            writer.WriteStartElement("StartTime");
            writer.WriteAttributeString("Value", "0");
            writer.WriteEndElement();

            writer.WriteStartElement("EndTime");
            writer.WriteAttributeString("Value", "0");
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
    }
}

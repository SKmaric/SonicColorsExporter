using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;


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
    }
}

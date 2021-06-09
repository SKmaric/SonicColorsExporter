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
            //Single File Mode
            if (rad_Single.Checked)
            {
                string inpath = txt_SrcFile.Text;
                string outpath = txt_OutPath.Text;

                //Check if file exists
                if (!File.Exists(inpath))
                {
                    MessageBox.Show("File does not exist.");
                    return;
                }

                string ext = Path.GetExtension(inpath).ToUpper();
                string outfile = outpath + "\\" + Path.GetFileNameWithoutExtension(inpath) + ".dae";

                if (ext == ".MDL0")
                {
                    MDL0Node node = NodeFactory.FromFile(null, inpath) as MDL0Node;
                    convertMDL0toDAE(node, outfile);
                }
            }
            else //Multi File Mode
            {
                string path = txt_SrcPath.Text;

                if (!Directory.Exists(path))
                {
                    MessageBox.Show("Directory does not exist.");
                    return;
                }
            }
        }

        public void convertMDL0toDAE(MDL0Node model, string outfile)
        {
            model.Export(outfile);
        }
    }
}

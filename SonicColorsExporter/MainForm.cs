﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;


namespace SonicColorsExporter
{
    public partial class MainForm : Form
    {
        public SettingsFlags flags = new SettingsFlags();

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
                Title = "Open File...",
                Filter = "All Supported Formats (*.arc;*.brres;*.mdl0;*.chr0;*.scn0;*.srt0;*.vis0;*.pat0;*.clr0;*.breff)|*.arc;*.brres;*.mdl0;*.chr0;*.scn0;*.srt0;*.vis0;*.pat0;*.clr0;*.breff|" +
                "U8 ARC File Archive (*.arc)|*.arc|" +
                "NW4R Resource Pack (*.brres)|*.brres|" +
                "NW4R Model (*.mdl0)|*.mdl0|" +
                "CHR0 Skeletal Animation (*.chr0)|*.chr0|" +
                "SCN0 Settings (*.scn0)|*.scn0|" +
                "SRT0 UV Animation (*.srt0)|*.srt0|" +
                "VIS0 Animation (*.vis0)|*.vis0|" +
                "PAT0 Animation (*.pat0)|*.pat0|" +
                "CLR0 Animation (*.clr0)|*.clr0|" +
                "BREFF Effect Resource (*.breff)|*.breff|" +
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
            flags.scaleMode = chk_ScaleMode.Checked;
            flags.singleBindMode = chk_SingleBind.Checked;
            flags.multimatCombine = chk_MultimatCombine.Checked;
            flags.tagMat = chk_TagMat.Checked;
            flags.tagObj = chk_TagObj.Checked;
            flags.UVOrganize = chk_UVOrganize.Checked;
            flags.lightmapMatMerge = chk_LightmapMatMerge.Checked;
            flags.opaAddGeo = chk_OpaAddGeo.Checked;
            flags.AnimsXML = chk_AnimAsXML.Checked;
            flags.chr0DAE = chk_CHR0asDAE.Checked;
            flags.flipXUV = true;
            flags.separateFolders = false;

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
                Program.ProcessFile(infile, outpath, flags);

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
                    string curOutPath = outpath;
                    //lbl_currentFile.Text = Path.GetFileName(infile);
                    if (flags.separateFolders)
                    {
                        string curName = Path.GetFileNameWithoutExtension(infile);
                        Directory.CreateDirectory(outpath + "\\" + curName);
                        curOutPath = outpath + "\\" + curName;
                    }
                    Program.ProcessFile(infile, curOutPath, flags);
                    progressBar.Value++;
                }

                MessageBox.Show("Finished. Be sure to convert to FBX using FbxConverterUI before importing to Max.");
                return;
            }
        }
    }
}

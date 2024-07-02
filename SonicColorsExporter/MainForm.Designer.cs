namespace SonicColorsExporter
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_SrcPath = new System.Windows.Forms.Button();
            this.txt_SrcPath = new System.Windows.Forms.TextBox();
            this.btn_SrcFile = new System.Windows.Forms.Button();
            this.btn_Save = new System.Windows.Forms.Button();
            this.lbl__SrcPath = new System.Windows.Forms.Label();
            this.lbl_SrcFile = new System.Windows.Forms.Label();
            this.lbl_OutPath = new System.Windows.Forms.Label();
            this.rad_Multi = new System.Windows.Forms.RadioButton();
            this.rad_Single = new System.Windows.Forms.RadioButton();
            this.lbl_Mode = new System.Windows.Forms.Label();
            this.txt_SrcFile = new System.Windows.Forms.TextBox();
            this.txt_OutPath = new System.Windows.Forms.TextBox();
            this.btn_OutPath = new System.Windows.Forms.Button();
            this.chk_SingleBind = new System.Windows.Forms.CheckBox();
            this.chk_MultimatCombine = new System.Windows.Forms.CheckBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.chk_TagMat = new System.Windows.Forms.CheckBox();
            this.chk_TagObj = new System.Windows.Forms.CheckBox();
            this.lbl_Tags = new System.Windows.Forms.Label();
            this.chk_LightmapMatMerge = new System.Windows.Forms.CheckBox();
            this.chk_OpaAddGeo = new System.Windows.Forms.CheckBox();
            this.chk_UVOrganize = new System.Windows.Forms.CheckBox();
            this.chk_ScaleMode = new System.Windows.Forms.CheckBox();
            this.lbl_currentFile = new System.Windows.Forms.Label();
            this.chk_AnimAsXML = new System.Windows.Forms.CheckBox();
            this.chk_CHR0asDAE = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btn_SrcPath
            // 
            this.btn_SrcPath.Location = new System.Drawing.Point(334, 38);
            this.btn_SrcPath.Name = "btn_SrcPath";
            this.btn_SrcPath.Size = new System.Drawing.Size(75, 23);
            this.btn_SrcPath.TabIndex = 0;
            this.btn_SrcPath.Text = "Browse...";
            this.btn_SrcPath.UseVisualStyleBackColor = true;
            this.btn_SrcPath.Click += new System.EventHandler(this.btn_SrcPath_Click);
            // 
            // txt_SrcPath
            // 
            this.txt_SrcPath.Location = new System.Drawing.Point(85, 40);
            this.txt_SrcPath.Name = "txt_SrcPath";
            this.txt_SrcPath.Size = new System.Drawing.Size(243, 20);
            this.txt_SrcPath.TabIndex = 1;
            // 
            // btn_SrcFile
            // 
            this.btn_SrcFile.Location = new System.Drawing.Point(334, 67);
            this.btn_SrcFile.Name = "btn_SrcFile";
            this.btn_SrcFile.Size = new System.Drawing.Size(75, 23);
            this.btn_SrcFile.TabIndex = 2;
            this.btn_SrcFile.Text = "Browse...";
            this.btn_SrcFile.UseVisualStyleBackColor = true;
            this.btn_SrcFile.Click += new System.EventHandler(this.btn_SrcFile_Click);
            // 
            // btn_Save
            // 
            this.btn_Save.Location = new System.Drawing.Point(265, 189);
            this.btn_Save.Name = "btn_Save";
            this.btn_Save.Size = new System.Drawing.Size(144, 67);
            this.btn_Save.TabIndex = 3;
            this.btn_Save.Text = "Convert";
            this.btn_Save.UseVisualStyleBackColor = true;
            this.btn_Save.Click += new System.EventHandler(this.btn_Save_Click);
            // 
            // lbl__SrcPath
            // 
            this.lbl__SrcPath.AutoSize = true;
            this.lbl__SrcPath.Location = new System.Drawing.Point(12, 43);
            this.lbl__SrcPath.Name = "lbl__SrcPath";
            this.lbl__SrcPath.Size = new System.Drawing.Size(69, 13);
            this.lbl__SrcPath.TabIndex = 4;
            this.lbl__SrcPath.Text = "Source Path:";
            // 
            // lbl_SrcFile
            // 
            this.lbl_SrcFile.AutoSize = true;
            this.lbl_SrcFile.Location = new System.Drawing.Point(12, 72);
            this.lbl_SrcFile.Name = "lbl_SrcFile";
            this.lbl_SrcFile.Size = new System.Drawing.Size(63, 13);
            this.lbl_SrcFile.TabIndex = 5;
            this.lbl_SrcFile.Text = "Source File:";
            // 
            // lbl_OutPath
            // 
            this.lbl_OutPath.AutoSize = true;
            this.lbl_OutPath.Location = new System.Drawing.Point(12, 101);
            this.lbl_OutPath.Name = "lbl_OutPath";
            this.lbl_OutPath.Size = new System.Drawing.Size(67, 13);
            this.lbl_OutPath.TabIndex = 6;
            this.lbl_OutPath.Text = "Output Path:";
            // 
            // rad_Multi
            // 
            this.rad_Multi.AutoSize = true;
            this.rad_Multi.Location = new System.Drawing.Point(55, 7);
            this.rad_Multi.Name = "rad_Multi";
            this.rad_Multi.Size = new System.Drawing.Size(85, 17);
            this.rad_Multi.TabIndex = 7;
            this.rad_Multi.Text = "Multiple Files";
            this.rad_Multi.UseVisualStyleBackColor = true;
            this.rad_Multi.CheckedChanged += new System.EventHandler(this.rad_Multi_CheckedChanged);
            // 
            // rad_Single
            // 
            this.rad_Single.AutoSize = true;
            this.rad_Single.Location = new System.Drawing.Point(146, 7);
            this.rad_Single.Name = "rad_Single";
            this.rad_Single.Size = new System.Drawing.Size(73, 17);
            this.rad_Single.TabIndex = 8;
            this.rad_Single.Text = "Single File";
            this.rad_Single.UseVisualStyleBackColor = true;
            // 
            // lbl_Mode
            // 
            this.lbl_Mode.AutoSize = true;
            this.lbl_Mode.Location = new System.Drawing.Point(12, 9);
            this.lbl_Mode.Name = "lbl_Mode";
            this.lbl_Mode.Size = new System.Drawing.Size(37, 13);
            this.lbl_Mode.TabIndex = 9;
            this.lbl_Mode.Text = "Mode:";
            // 
            // txt_SrcFile
            // 
            this.txt_SrcFile.Location = new System.Drawing.Point(85, 69);
            this.txt_SrcFile.Name = "txt_SrcFile";
            this.txt_SrcFile.Size = new System.Drawing.Size(243, 20);
            this.txt_SrcFile.TabIndex = 10;
            // 
            // txt_OutPath
            // 
            this.txt_OutPath.Location = new System.Drawing.Point(85, 98);
            this.txt_OutPath.Name = "txt_OutPath";
            this.txt_OutPath.Size = new System.Drawing.Size(243, 20);
            this.txt_OutPath.TabIndex = 11;
            // 
            // btn_OutPath
            // 
            this.btn_OutPath.Location = new System.Drawing.Point(334, 96);
            this.btn_OutPath.Name = "btn_OutPath";
            this.btn_OutPath.Size = new System.Drawing.Size(75, 23);
            this.btn_OutPath.TabIndex = 12;
            this.btn_OutPath.Text = "Browse...";
            this.btn_OutPath.UseVisualStyleBackColor = true;
            this.btn_OutPath.Click += new System.EventHandler(this.btn_OutPath_Click);
            // 
            // chk_SingleBind
            // 
            this.chk_SingleBind.AutoSize = true;
            this.chk_SingleBind.Checked = true;
            this.chk_SingleBind.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_SingleBind.Location = new System.Drawing.Point(87, 146);
            this.chk_SingleBind.Name = "chk_SingleBind";
            this.chk_SingleBind.Size = new System.Drawing.Size(116, 17);
            this.chk_SingleBind.TabIndex = 14;
            this.chk_SingleBind.Text = "SingleBind Support";
            this.chk_SingleBind.UseVisualStyleBackColor = true;
            // 
            // chk_MultimatCombine
            // 
            this.chk_MultimatCombine.AutoSize = true;
            this.chk_MultimatCombine.Location = new System.Drawing.Point(12, 169);
            this.chk_MultimatCombine.Name = "chk_MultimatCombine";
            this.chk_MultimatCombine.Size = new System.Drawing.Size(149, 17);
            this.chk_MultimatCombine.TabIndex = 15;
            this.chk_MultimatCombine.Text = "Combine MultiMat Objects";
            this.chk_MultimatCombine.UseVisualStyleBackColor = true;
            this.chk_MultimatCombine.Visible = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 233);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(247, 23);
            this.progressBar.TabIndex = 16;
            // 
            // chk_TagMat
            // 
            this.chk_TagMat.AutoSize = true;
            this.chk_TagMat.Checked = true;
            this.chk_TagMat.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_TagMat.Location = new System.Drawing.Point(116, 192);
            this.chk_TagMat.Name = "chk_TagMat";
            this.chk_TagMat.Size = new System.Drawing.Size(68, 17);
            this.chk_TagMat.TabIndex = 17;
            this.chk_TagMat.Text = "Materials";
            this.chk_TagMat.UseVisualStyleBackColor = true;
            // 
            // chk_TagObj
            // 
            this.chk_TagObj.AutoSize = true;
            this.chk_TagObj.Checked = true;
            this.chk_TagObj.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_TagObj.Location = new System.Drawing.Point(190, 192);
            this.chk_TagObj.Name = "chk_TagObj";
            this.chk_TagObj.Size = new System.Drawing.Size(62, 17);
            this.chk_TagObj.TabIndex = 18;
            this.chk_TagObj.Text = "Objects";
            this.chk_TagObj.UseVisualStyleBackColor = true;
            // 
            // lbl_Tags
            // 
            this.lbl_Tags.AutoSize = true;
            this.lbl_Tags.Location = new System.Drawing.Point(12, 193);
            this.lbl_Tags.Name = "lbl_Tags";
            this.lbl_Tags.Size = new System.Drawing.Size(98, 13);
            this.lbl_Tags.TabIndex = 19;
            this.lbl_Tags.Text = "Add Property Tags:";
            // 
            // chk_LightmapMatMerge
            // 
            this.chk_LightmapMatMerge.AutoSize = true;
            this.chk_LightmapMatMerge.Location = new System.Drawing.Point(167, 169);
            this.chk_LightmapMatMerge.Name = "chk_LightmapMatMerge";
            this.chk_LightmapMatMerge.Size = new System.Drawing.Size(142, 17);
            this.chk_LightmapMatMerge.TabIndex = 20;
            this.chk_LightmapMatMerge.Text = "Merge lightmap materials";
            this.chk_LightmapMatMerge.UseVisualStyleBackColor = true;
            this.chk_LightmapMatMerge.Visible = false;
            // 
            // chk_OpaAddGeo
            // 
            this.chk_OpaAddGeo.AutoSize = true;
            this.chk_OpaAddGeo.Location = new System.Drawing.Point(12, 215);
            this.chk_OpaAddGeo.Name = "chk_OpaAddGeo";
            this.chk_OpaAddGeo.Size = new System.Drawing.Size(205, 17);
            this.chk_OpaAddGeo.TabIndex = 21;
            this.chk_OpaAddGeo.Text = "OpaAdd shader geometry workaround";
            this.chk_OpaAddGeo.UseVisualStyleBackColor = true;
            this.chk_OpaAddGeo.Visible = false;
            // 
            // chk_UVOrganize
            // 
            this.chk_UVOrganize.AutoSize = true;
            this.chk_UVOrganize.Checked = true;
            this.chk_UVOrganize.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_UVOrganize.Location = new System.Drawing.Point(209, 146);
            this.chk_UVOrganize.Name = "chk_UVOrganize";
            this.chk_UVOrganize.Size = new System.Drawing.Size(144, 17);
            this.chk_UVOrganize.TabIndex = 22;
            this.chk_UVOrganize.Text = "Reorganize UV channels";
            this.chk_UVOrganize.UseVisualStyleBackColor = true;
            // 
            // chk_ScaleMode
            // 
            this.chk_ScaleMode.AutoSize = true;
            this.chk_ScaleMode.Checked = true;
            this.chk_ScaleMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_ScaleMode.Location = new System.Drawing.Point(12, 146);
            this.chk_ScaleMode.Name = "chk_ScaleMode";
            this.chk_ScaleMode.Size = new System.Drawing.Size(69, 17);
            this.chk_ScaleMode.TabIndex = 23;
            this.chk_ScaleMode.Text = "Fix Scale";
            this.chk_ScaleMode.UseVisualStyleBackColor = true;
            // 
            // lbl_currentFile
            // 
            this.lbl_currentFile.AutoSize = true;
            this.lbl_currentFile.Location = new System.Drawing.Point(211, 216);
            this.lbl_currentFile.Name = "lbl_currentFile";
            this.lbl_currentFile.Size = new System.Drawing.Size(48, 13);
            this.lbl_currentFile.TabIndex = 24;
            this.lbl_currentFile.Text = "Progress";
            this.lbl_currentFile.Visible = false;
            // 
            // chk_AnimAsXML
            // 
            this.chk_AnimAsXML.AutoSize = true;
            this.chk_AnimAsXML.Location = new System.Drawing.Point(12, 124);
            this.chk_AnimAsXML.Name = "chk_AnimAsXML";
            this.chk_AnimAsXML.Size = new System.Drawing.Size(120, 17);
            this.chk_AnimAsXML.TabIndex = 25;
            this.chk_AnimAsXML.Text = "Save anims as XML";
            this.chk_AnimAsXML.UseVisualStyleBackColor = true;
            // 
            // chk_CHR0asDAE
            // 
            this.chk_CHR0asDAE.AutoSize = true;
            this.chk_CHR0asDAE.Location = new System.Drawing.Point(138, 123);
            this.chk_CHR0asDAE.Name = "chk_CHR0asDAE";
            this.chk_CHR0asDAE.Size = new System.Drawing.Size(122, 17);
            this.chk_CHR0asDAE.TabIndex = 26;
            this.chk_CHR0asDAE.Text = "Save CHR0 as DAE";
            this.chk_CHR0asDAE.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(421, 263);
            this.Controls.Add(this.chk_CHR0asDAE);
            this.Controls.Add(this.chk_AnimAsXML);
            this.Controls.Add(this.lbl_currentFile);
            this.Controls.Add(this.chk_ScaleMode);
            this.Controls.Add(this.chk_UVOrganize);
            this.Controls.Add(this.chk_OpaAddGeo);
            this.Controls.Add(this.chk_LightmapMatMerge);
            this.Controls.Add(this.lbl_Tags);
            this.Controls.Add(this.chk_TagObj);
            this.Controls.Add(this.chk_TagMat);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.chk_MultimatCombine);
            this.Controls.Add(this.chk_SingleBind);
            this.Controls.Add(this.btn_OutPath);
            this.Controls.Add(this.txt_OutPath);
            this.Controls.Add(this.txt_SrcFile);
            this.Controls.Add(this.lbl_Mode);
            this.Controls.Add(this.rad_Single);
            this.Controls.Add(this.rad_Multi);
            this.Controls.Add(this.lbl_OutPath);
            this.Controls.Add(this.lbl_SrcFile);
            this.Controls.Add(this.lbl__SrcPath);
            this.Controls.Add(this.btn_Save);
            this.Controls.Add(this.btn_SrcFile);
            this.Controls.Add(this.txt_SrcPath);
            this.Controls.Add(this.btn_SrcPath);
            this.Name = "MainForm";
            this.Text = "Sonic Colors Exporter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_SrcPath;
        private System.Windows.Forms.TextBox txt_SrcPath;
        private System.Windows.Forms.Button btn_SrcFile;
        private System.Windows.Forms.Button btn_Save;
        private System.Windows.Forms.Label lbl__SrcPath;
        private System.Windows.Forms.Label lbl_SrcFile;
        private System.Windows.Forms.Label lbl_OutPath;
        private System.Windows.Forms.RadioButton rad_Multi;
        private System.Windows.Forms.RadioButton rad_Single;
        private System.Windows.Forms.Label lbl_Mode;
        private System.Windows.Forms.TextBox txt_SrcFile;
        private System.Windows.Forms.TextBox txt_OutPath;
        private System.Windows.Forms.Button btn_OutPath;
        private System.Windows.Forms.CheckBox chk_SingleBind;
        private System.Windows.Forms.CheckBox chk_MultimatCombine;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.CheckBox chk_TagMat;
        private System.Windows.Forms.CheckBox chk_TagObj;
        private System.Windows.Forms.Label lbl_Tags;
        private System.Windows.Forms.CheckBox chk_LightmapMatMerge;
        private System.Windows.Forms.CheckBox chk_OpaAddGeo;
        private System.Windows.Forms.CheckBox chk_UVOrganize;
        private System.Windows.Forms.CheckBox chk_ScaleMode;
        private System.Windows.Forms.Label lbl_currentFile;
        private System.Windows.Forms.CheckBox chk_AnimAsXML;
        private System.Windows.Forms.CheckBox chk_CHR0asDAE;
    }
}


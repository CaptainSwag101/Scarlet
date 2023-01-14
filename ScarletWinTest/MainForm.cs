﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

using Scarlet.IO;
using Scarlet.IO.ImageFormats;
using Scarlet.IO.CompressionFormats;
using Scarlet.IO.ContainerFormats;

namespace ScarletWinTest
{
    public partial class MainForm : Form
    {
        Version programVersion;

        List<FileFormatInfo> imageFormats;

        string currentFilename;
        FileFormat currentFile;

        public MainForm()
        {
            InitializeComponent();

            programVersion = new Version(Application.ProductVersion);

            imageFormats = GetFormatInfos(typeof(ImageFormat));

            SetFormTitle();
            SetOpenDialogFilters();

            tsslStatus.Text = "Ready";
        }

        private void SetFormTitle()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} v{1}.{2}", Application.ProductName, programVersion.Major, programVersion.Minor);
            if (programVersion.Build != 0) builder.AppendFormat(".{0}", programVersion.Build);

            if (currentFile != null)
                builder.AppendFormat(" - [{0}]", Path.GetFileName(currentFilename));

            Text = builder.ToString();
        }

        private void SetOpenDialogFilters()
        {
            List<string> filterList = new List<string>();

            foreach (FileFormatInfo formatInfo in imageFormats.OrderBy(x => x.FormatDescription))
                filterList.Add(string.Format("{0} (*{1})|*{1}", formatInfo.FormatDescription, formatInfo.FileExtension));

            filterList.Insert(0, "All Files (*.*)|*.*");
            ofdOpenFile.Filter = string.Join("|", filterList);
        }

        public List<FileFormatInfo> GetFormatInfos(Type baseFormatType)
        {
            // TODO: very naive, only picks up formats in same assembly as base format type, file extension extraction from regex is iffy, etc...

            List<FileFormatInfo> infos = new List<FileFormatInfo>();

            foreach (Type type in Assembly.GetAssembly(baseFormatType).GetExportedTypes().Where(x => x != baseFormatType && x.BaseType == baseFormatType))
            {
                FileFormat instance = (FileFormat)Activator.CreateInstance(type);

                string description = (instance.GetFormatDescription() ?? string.Format("{0} Format", type.Name)), extension = ".*";

                var fnPatternAttrib = type.GetCustomAttributes(typeof(FilenamePatternAttribute), false).FirstOrDefault();
                if (fnPatternAttrib != null)
                {
                    string pattern = (fnPatternAttrib as FilenamePatternAttribute).Pattern;
                    extension = Path.GetExtension(pattern).Replace("$", "");
                }

                infos.Add(new FileFormatInfo(description, extension, type));
            }

            return infos;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdOpenFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentFile = FileFormat.FromFile<FileFormat>(currentFilename = ofdOpenFile.FileName);

                if (currentFile != null && currentFile is ImageFormat)
                {
                    pbImage.Image = (currentFile as ImageFormat).GetBitmap();

                    tsslStatus.Text = string.Format("Loaded file '{0}'", currentFilename);
                }

                SetFormTitle();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sfdSaveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                pbImage.Image.Save(sfdSaveFile.FileName);
#pragma warning restore CA1416 // Validate platform compatibility

                tsslStatus.Text = string.Format("File saved as '{0}'", sfdSaveFile.FileName);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            var name = (assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false).FirstOrDefault() as AssemblyProductAttribute).Product;
            var version = new Version((assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false).FirstOrDefault() as AssemblyFileVersionAttribute).Version);
            var description = (assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false).FirstOrDefault() as AssemblyDescriptionAttribute).Description;
            var copyright = (assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false).FirstOrDefault() as AssemblyCopyrightAttribute).Copyright;

            StringBuilder aboutBuilder = new StringBuilder();
            aboutBuilder.AppendFormat("{0} v{1}.{2} - {3}", name, version.Major, version.Minor, description);
            aboutBuilder.AppendLine();
            aboutBuilder.AppendLine();
            aboutBuilder.AppendLine(copyright);

            MessageBox.Show(aboutBuilder.ToString(), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

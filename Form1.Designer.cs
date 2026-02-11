namespace PDFtoPS
{
    partial class PDFtoPS
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            comboBoxProfiles = new ComboBox();
            btnConvert = new Button();
            btnAdd = new Button();
            btnSort = new Button();
            btnRemove = new Button();
            button5 = new Button();
            button6 = new Button();
            btnExit = new Button();
            btnBrowse = new Button();
            txtOutputPath = new TextBox();
            groupSettings = new GroupBox();
            groupBox6 = new GroupBox();
            cmbPageSize = new ComboBox();
            groupBox5 = new GroupBox();
            cmbFonts = new ComboBox();
            groupBox4 = new GroupBox();
            cmbColorModel = new ComboBox();
            groupBox3 = new GroupBox();
            cmbJpegQuality = new ComboBox();
            groupBox2 = new GroupBox();
            cmbMonoDPI = new ComboBox();
            groupBox1 = new GroupBox();
            cmbDownsampleType = new ComboBox();
            cmbColorDPI = new ComboBox();
            listViewFiles = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            progressBar1 = new ProgressBar();
            groupSettings.SuspendLayout();
            groupBox6.SuspendLayout();
            groupBox5.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // comboBoxProfiles
            // 
            comboBoxProfiles.ForeColor = SystemColors.InfoText;
            comboBoxProfiles.FormattingEnabled = true;
            comboBoxProfiles.Location = new Point(780, 750);
            comboBoxProfiles.Name = "comboBoxProfiles";
            comboBoxProfiles.Size = new Size(455, 33);
            comboBoxProfiles.TabIndex = 1;
            comboBoxProfiles.Text = "Выберите профиль ...";
            // 
            // btnConvert
            // 
            btnConvert.Location = new Point(937, 1282);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(146, 34);
            btnConvert.TabIndex = 2;
            btnConvert.Text = "Convert Files";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(28, 681);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(112, 34);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "Add ...";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnSort
            // 
            btnSort.Location = new Point(146, 681);
            btnSort.Name = "btnSort";
            btnSort.Size = new Size(112, 34);
            btnSort.TabIndex = 4;
            btnSort.Text = "Sort";
            btnSort.UseVisualStyleBackColor = true;
            btnSort.Click += btnSort_Click;
            // 
            // btnRemove
            // 
            btnRemove.Location = new Point(264, 681);
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new Size(112, 34);
            btnRemove.TabIndex = 5;
            btnRemove.Text = "Remove";
            btnRemove.UseVisualStyleBackColor = true;
            btnRemove.Click += btnRemove_Click;
            // 
            // button5
            // 
            button5.Location = new Point(937, 681);
            button5.Name = "button5";
            button5.Size = new Size(146, 34);
            button5.TabIndex = 6;
            button5.Text = "Move Up";
            button5.UseVisualStyleBackColor = true;
            button5.Click += btnMoveUp_Click;
            // 
            // button6
            // 
            button6.Location = new Point(1089, 681);
            button6.Name = "button6";
            button6.Size = new Size(146, 34);
            button6.TabIndex = 7;
            button6.Text = "Move Down";
            button6.UseVisualStyleBackColor = true;
            button6.Click += btnMoveDown_Click;
            // 
            // btnExit
            // 
            btnExit.Location = new Point(1089, 1282);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(146, 34);
            btnExit.TabIndex = 9;
            btnExit.Text = "Exit";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(780, 1282);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(146, 34);
            btnBrowse.TabIndex = 8;
            btnBrowse.Text = "Save as ...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // txtOutputPath
            // 
            txtOutputPath.Location = new Point(28, 1285);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.Size = new Size(731, 31);
            txtOutputPath.TabIndex = 10;
            // 
            // groupSettings
            // 
            groupSettings.BackColor = SystemColors.Control;
            groupSettings.Controls.Add(groupBox6);
            groupSettings.Controls.Add(groupBox5);
            groupSettings.Controls.Add(groupBox4);
            groupSettings.Controls.Add(groupBox3);
            groupSettings.Controls.Add(groupBox2);
            groupSettings.Controls.Add(groupBox1);
            groupSettings.Location = new Point(28, 812);
            groupSettings.Name = "groupSettings";
            groupSettings.Size = new Size(1207, 435);
            groupSettings.TabIndex = 11;
            groupSettings.TabStop = false;
            groupSettings.Text = "Настройки экспорта";
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(cmbPageSize);
            groupBox6.Location = new Point(476, 267);
            groupBox6.Name = "groupBox6";
            groupBox6.Size = new Size(414, 101);
            groupBox6.TabIndex = 15;
            groupBox6.TabStop = false;
            groupBox6.Text = "Размер страницы";
            // 
            // cmbPageSize
            // 
            cmbPageSize.ForeColor = SystemColors.InfoText;
            cmbPageSize.FormattingEnabled = true;
            cmbPageSize.Location = new Point(27, 42);
            cmbPageSize.Name = "cmbPageSize";
            cmbPageSize.Size = new Size(360, 33);
            cmbPageSize.TabIndex = 0;
            cmbPageSize.Text = "Размер страницы";
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(cmbFonts);
            groupBox5.Location = new Point(476, 154);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(414, 101);
            groupBox5.TabIndex = 15;
            groupBox5.TabStop = false;
            groupBox5.Text = "Шрифты (Fonts)";
            // 
            // cmbFonts
            // 
            cmbFonts.ForeColor = SystemColors.InfoText;
            cmbFonts.FormattingEnabled = true;
            cmbFonts.Location = new Point(26, 42);
            cmbFonts.Name = "cmbFonts";
            cmbFonts.Size = new Size(360, 33);
            cmbFonts.TabIndex = 0;
            cmbFonts.Text = "Обработка шрифтов";
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(cmbColorModel);
            groupBox4.Location = new Point(476, 44);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(414, 101);
            groupBox4.TabIndex = 14;
            groupBox4.TabStop = false;
            groupBox4.Text = "Цвет (Color)";
            // 
            // cmbColorModel
            // 
            cmbColorModel.ForeColor = SystemColors.InfoText;
            cmbColorModel.FormattingEnabled = true;
            cmbColorModel.Location = new Point(27, 42);
            cmbColorModel.Name = "cmbColorModel";
            cmbColorModel.Size = new Size(360, 33);
            cmbColorModel.TabIndex = 0;
            cmbColorModel.Text = "Цветовая модель";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(cmbJpegQuality);
            groupBox3.Location = new Point(23, 309);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(414, 99);
            groupBox3.TabIndex = 14;
            groupBox3.TabStop = false;
            groupBox3.Text = "Качество JPEG (Сжатие)";
            // 
            // cmbJpegQuality
            // 
            cmbJpegQuality.ForeColor = SystemColors.InfoText;
            cmbJpegQuality.FormattingEnabled = true;
            cmbJpegQuality.Location = new Point(27, 39);
            cmbJpegQuality.Name = "cmbJpegQuality";
            cmbJpegQuality.Size = new Size(360, 33);
            cmbJpegQuality.TabIndex = 0;
            cmbJpegQuality.Text = "Качество JPEG";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(cmbMonoDPI);
            groupBox2.Location = new Point(23, 202);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(414, 101);
            groupBox2.TabIndex = 13;
            groupBox2.TabStop = false;
            groupBox2.Text = "Монохромные изображения";
            // 
            // cmbMonoDPI
            // 
            cmbMonoDPI.ForeColor = SystemColors.InfoText;
            cmbMonoDPI.FormattingEnabled = true;
            cmbMonoDPI.Location = new Point(27, 39);
            cmbMonoDPI.Name = "cmbMonoDPI";
            cmbMonoDPI.Size = new Size(360, 33);
            cmbMonoDPI.TabIndex = 0;
            cmbMonoDPI.Text = "Монохромные (PPI)";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cmbDownsampleType);
            groupBox1.Controls.Add(cmbColorDPI);
            groupBox1.Location = new Point(23, 44);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(414, 152);
            groupBox1.TabIndex = 12;
            groupBox1.TabStop = false;
            groupBox1.Text = "Цветные и серые изображения";
            // 
            // cmbDownsampleType
            // 
            cmbDownsampleType.ForeColor = SystemColors.InfoText;
            cmbDownsampleType.FormattingEnabled = true;
            cmbDownsampleType.Location = new Point(27, 91);
            cmbDownsampleType.Name = "cmbDownsampleType";
            cmbDownsampleType.Size = new Size(360, 33);
            cmbDownsampleType.TabIndex = 1;
            cmbDownsampleType.Text = "Метод (Бикубический и т.д.)";
            // 
            // cmbColorDPI
            // 
            cmbColorDPI.ForeColor = SystemColors.InfoText;
            cmbColorDPI.FormattingEnabled = true;
            cmbColorDPI.Location = new Point(27, 42);
            cmbColorDPI.Name = "cmbColorDPI";
            cmbColorDPI.Size = new Size(360, 33);
            cmbColorDPI.TabIndex = 0;
            cmbColorDPI.Text = "Цветные/Серые (PPI)";
            // 
            // listViewFiles
            // 
            listViewFiles.AllowDrop = true;
            listViewFiles.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
            listViewFiles.FullRowSelect = true;
            listViewFiles.GridLines = true;
            listViewFiles.Location = new Point(28, 36);
            listViewFiles.Name = "listViewFiles";
            listViewFiles.Size = new Size(1207, 611);
            listViewFiles.TabIndex = 12;
            listViewFiles.UseCompatibleStateImageBehavior = false;
            listViewFiles.View = View.Details;
            listViewFiles.ColumnClick += listViewFiles_ColumnClick;
            listViewFiles.ItemDrag += listViewFiles_ItemDrag;
            listViewFiles.DragDrop += listViewFiles_DragDrop;
            listViewFiles.DragOver += listViewFiles_DragOver;
            listViewFiles.KeyDown += listViewFiles_KeyDown;
            // 
            // columnHeader1
            // 
            columnHeader1.Tag = "#";
            columnHeader1.Text = "#";
            columnHeader1.Width = 40;
            // 
            // columnHeader2
            // 
            columnHeader2.Tag = "Имя файла";
            columnHeader2.Text = "Имя файла";
            columnHeader2.Width = 750;
            // 
            // columnHeader3
            // 
            columnHeader3.Tag = "Размер";
            columnHeader3.Text = "Размер";
            columnHeader3.Width = 200;
            // 
            // columnHeader4
            // 
            columnHeader4.Tag = "Дата изменения";
            columnHeader4.Text = "Дата изменения";
            columnHeader4.Width = 200;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(28, 1334);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(1207, 34);
            progressBar1.TabIndex = 13;
            // 
            // PDFtoPS
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1265, 1429);
            Controls.Add(progressBar1);
            Controls.Add(listViewFiles);
            Controls.Add(btnConvert);
            Controls.Add(groupSettings);
            Controls.Add(txtOutputPath);
            Controls.Add(btnExit);
            Controls.Add(btnBrowse);
            Controls.Add(button6);
            Controls.Add(button5);
            Controls.Add(btnRemove);
            Controls.Add(btnSort);
            Controls.Add(btnAdd);
            Controls.Add(comboBoxProfiles);
            Name = "PDFtoPS";
            Text = "PDFtoPS";
            groupSettings.ResumeLayout(false);
            groupBox6.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox comboBoxProfiles;
        private Button btnConvert;
        private Button btnAdd;
        private Button btnSort;
        private Button btnRemove;
        private Button button5;
        private Button button6;
        private Button btnExit;
        private Button btnBrowse;
        private TextBox txtOutputPath;
        private GroupBox groupSettings;
        private ListView listViewFiles;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private GroupBox groupBox2;
        private ComboBox cmbMonoDPI;
        private GroupBox groupBox1;
        private ComboBox cmbDownsampleType;
        private ComboBox cmbColorDPI;
        private GroupBox groupBox3;
        private ComboBox cmbJpegQuality;
        private GroupBox groupBox5;
        private ComboBox cmbFonts;
        private GroupBox groupBox4;
        private ComboBox cmbColorModel;
        private GroupBox groupBox6;
        private ComboBox cmbPageSize;
        private ProgressBar progressBar1;
    }
}
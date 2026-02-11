#nullable disable
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PDFtoPS
{
    public partial class PDFtoPS : Form
    {
        // --- 1. СЕКЦИЯ WINDOWS API ---
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const uint LVM_SETINSERTMARK = 0x10A6;
        [StructLayout(LayoutKind.Sequential)]
        public struct LVINSERTMARK
        {
            public uint cbSize;
            public uint dwFlags;
            public int iItem;
            public uint dwReserved;
        }
        private void SetInsertionMark(int index)
        {
            LVINSERTMARK insertMark = new LVINSERTMARK();
            insertMark.cbSize = (uint)Marshal.SizeOf(typeof(LVINSERTMARK));
            insertMark.dwFlags = 0;
            insertMark.iItem = index;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(insertMark));
            Marshal.StructureToPtr(insertMark, ptr, false);
            SendMessage(listViewFiles.Handle, LVM_SETINSERTMARK, IntPtr.Zero, ptr);
            Marshal.FreeHGlobal(ptr);
        }
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);
        [StructLayout(LayoutKind.Sequential)]
        public struct HDITEM
        {
            public uint mask;
            public int cxy;
            public string pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public int fmt;
            public IntPtr lParam;
            public int iImage;
            public int iOrder;
            public uint type;
            public IntPtr pvFilter;
            public uint state;
        }
        private const uint LVM_GETHEADER = 0x101F;
        private const uint HDM_GETITEM = 0x120B;
        private const uint HDM_SETITEM = 0x120C;
        private const int HDI_FORMAT = 0x0004;
        private const int HDF_STRING = 0x4000;
        private const int HDF_SORTUP = 0x0400;
        private const int HDF_SORTDOWN = 0x0200;

        // --- 2. ПЕРЕМЕННЫЕ КЛАССА ---

        // Элементы статус-бара (создадим их программно)
        private StatusStrip statusStrip;
        private ToolStripProgressBar progressBar;
        private ToolStripStatusLabel statusLabel;

        private bool isAscending = true;
        private int lastInsertionIndex = -1;
        private int lastSortColumn = -1;
        private SortOrder lastSortOrder = SortOrder.Ascending;

        private Dictionary<string, string> profiles = new Dictionary<string, string>
        {
            { "Standard (PS Level 3)", "-sDEVICE=ps2write -dLanguageLevel=3" },
            { "Legacy (PS Level 2)", "-sDEVICE=ps2write -dLanguageLevel=2" },
            { "Grayscale", "-sDEVICE=ps2write -dLanguageLevel=3" }
        };

        // --- 3. КОНСТРУКТОР ---
        public PDFtoPS()
        {
            InitializeComponent();

            SetWindowTheme(listViewFiles.Handle, "explorer", null);
            IntPtr hHeader = SendMessage(listViewFiles.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            SetWindowTheme(hHeader, "explorer", null);
            PropertyInfo dbProperty = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            dbProperty.SetValue(listViewFiles, true, null);

            comboBoxProfiles.Items.Clear();
            comboBoxProfiles.Items.AddRange(profiles.Keys.ToArray());
            if (comboBoxProfiles.Items.Count > 0) comboBoxProfiles.SelectedIndex = 0;
            listViewFiles.AllowDrop = true;

            InitInterfaceComponents(); // Инициализация статус-бара и списков

            comboBoxProfiles.SelectedIndexChanged += comboBoxProfiles_SelectedIndexChanged;
            if (comboBoxProfiles.Items.Count > 0) comboBoxProfiles.SelectedIndex = 0;
        }

        // --- 4. МЕТОДЫ ГЕНЕРАЦИИ АРГУМЕНТОВ ---

        private string GetImageSettingsArgs()
        {
            List<string> args = new List<string>();

            // УДАЛИЛИ: args.Add("-dPDFSETTINGS=/prepress"); 
            // Эта команда конфликтовала с нашими ручными настройками и вызывала ошибку 255.
            // Наши ручные настройки ниже достаточно мощные сами по себе.

            // 1. ЦВЕТНЫЕ И СЕРЫЕ
            if (cmbColorDPI.SelectedIndex > 0)
            {
                string dpiText = cmbColorDPI.SelectedItem.ToString().Split(' ')[0];
                string method = "/" + cmbDownsampleType.SelectedItem.ToString();

                args.Add("-dDownsampleColorImages=true");
                args.Add($"-dColorImageResolution={dpiText}");
                args.Add($"-dColorImageDownsampleType={method}");

                args.Add("-dDownsampleGrayImages=true");
                args.Add($"-dGrayImageResolution={dpiText}");
                args.Add($"-dGrayImageDownsampleType={method}");
            }

            // 2. МОНОХРОМНЫЕ (ЧЕРТЕЖИ)
            if (cmbMonoDPI.SelectedIndex > 0)
            {
                string monoDpiText = cmbMonoDPI.SelectedItem.ToString();

                // Если выбрано 1200 - отключаем даунсемплинг. 
                // Картинка пойдет в оригинальном качестве (хоть 2400, хоть 5000 dpi).
                // Это лучшее решение для CTP.
                if (monoDpiText == "1200")
                {
                    args.Add("-dDownsampleMonoImages=false");
                }
                else
                {
                    args.Add("-dDownsampleMonoImages=true");
                    args.Add($"-dMonoImageResolution={monoDpiText}");
                    args.Add("-dMonoImageDownsampleType=/Bicubic");
                }
            }

            // 3. СЖАТИЕ
            if (cmbJpegQuality.SelectedIndex == 0) // Maximum
            {
                args.Add("-sColorImageFilter=/FlateEncode");
                args.Add("-sGrayImageFilter=/FlateEncode");
            }
            else
            {
                args.Add("-sColorImageFilter=/DCTEncode");
                args.Add("-sGrayImageFilter=/DCTEncode");
            }

            return string.Join(" ", args);
        }
        private string GetFontArgs()
        {
            if (cmbFonts.SelectedIndex == 0) return "-dEmbedAllFonts=true -dSubsetFonts=true";
            else return "-dNoOutputFonts"; // Кривые
        }

        private string GetColorArgs()
        {
            switch (cmbColorModel.SelectedIndex)
            {
                case 1: return "-sProcessColorModel=DeviceCMYK -sColorConversionStrategy=CMYK -dOverrideICC=true";
                case 2: return "-sProcessColorModel=DeviceGray -sColorConversionStrategy=Gray";
                default: return "-sColorConversionStrategy=LeaveColorUnchanged";
            }
        }

        private string GetPageSizeArgs()
        {
            if (cmbPageSize.SelectedIndex == 0) return "";
            switch (cmbPageSize.SelectedIndex)
            {
                case 1: // CTP 510x400
                    return "-dDEVICEWIDTHpoints=1446 -dDEVICEHEIGHTpoints=1134 -dFIXEDMEDIA";
                case 2: // A4
                    return "-sPAPERSIZE=a4 -dFIXEDMEDIA";
                case 3: // A3
                    return "-sPAPERSIZE=a3 -dFIXEDMEDIA";
            }
            return "";
        }

        // --- ЛОГИКА ИНТЕРФЕЙСА ---

        private void InitInterfaceComponents()
        {
            // 2. ЗАПОЛНЯЕМ СПИСКИ
            cmbColorDPI.Items.Clear();
            cmbColorDPI.Items.AddRange(new object[] { "Без изменений", "300 (Print)", "150 (Office)", "72 (Screen)" });
            cmbColorDPI.SelectedIndex = 1;

            cmbDownsampleType.Items.Clear();
            cmbDownsampleType.Items.AddRange(new object[] { "Bicubic", "Average", "Subsample" });
            cmbDownsampleType.SelectedIndex = 0;

            cmbMonoDPI.Items.Clear();
            cmbMonoDPI.Items.AddRange(new object[] { "Без изменений", "1200", "600", "300" });
            cmbMonoDPI.SelectedIndex = 1;

            cmbJpegQuality.Items.Clear();
            cmbJpegQuality.Items.AddRange(new object[] { "Maximum", "High", "Medium", "Low" });
            cmbJpegQuality.SelectedIndex = 0;

            cmbFonts.Items.Clear();
            cmbFonts.Items.AddRange(new object[] { "Встраивать шрифты (Standard)", "Преобразовать в кривые (Safe Mode)" });
            cmbFonts.SelectedIndex = 1;

            cmbColorModel.Items.Clear();
            cmbColorModel.Items.AddRange(new object[] { "Без изменений", "Принудительно CMYK (Print)", "Градации серого (Gray)" });
            cmbColorModel.SelectedIndex = 1;

            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(new object[] {
                "Как в документе (From PDF)",
                "CTP Plate (510 x 400 mm)",
                "A4 (210 x 297 mm)",
                "A3 (297 x 420 mm)"
            });
            cmbPageSize.SelectedIndex = 0;
        }

        private void comboBoxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem.ToString();
            // Цвет
            if (selectedProfile.Contains("Grayscale")) cmbColorModel.SelectedIndex = 2;
            else cmbColorModel.SelectedIndex = 1;

            // Разрешение
            if (selectedProfile.Contains("Standard") || selectedProfile.Contains("Level 3"))
            {
                cmbColorDPI.SelectedIndex = 1; cmbDownsampleType.SelectedIndex = 0; cmbMonoDPI.SelectedIndex = 1;
            }
            else if (selectedProfile.Contains("Legacy") || selectedProfile.Contains("Level 2"))
            {
                cmbColorDPI.SelectedIndex = 2; cmbDownsampleType.SelectedIndex = 1; cmbMonoDPI.SelectedIndex = 2;
            }
            else if (selectedProfile.Contains("Grayscale"))
            {
                cmbColorDPI.SelectedIndex = 1; cmbDownsampleType.SelectedIndex = 0; cmbMonoDPI.SelectedIndex = 1;
            }
        }

        // --- КНОПКИ И СОБЫТИЯ ---

        private void btnConvert_Click(object sender, EventArgs e)
        {
            string gsPath = @"C:\Program Files\gs\gs10.03.1\bin\gswin64c.exe";
            if (!File.Exists(gsPath)) { MessageBox.Show("Ghostscript не найден!"); return; }
            if (listViewFiles.Items.Count == 0) return;

            string outputDir = txtOutputPath.Text;
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir)) { MessageBox.Show("Выберите папку!"); return; }

            // --- НАСТРОЙКА ВАШЕГО PROGRESSBAR ---
            progressBar1.Minimum = 0;
            progressBar1.Maximum = listViewFiles.Items.Count;
            progressBar1.Value = 0;
            progressBar1.Step = 1;
            progressBar1.Visible = true; // Показываем бар

            // --- НАСТРОЙКИ КОНВЕРТАЦИИ ---
            string pdfSettings = GetPdfSettings();
            string colorArgs = "-sProcessColorModel=DeviceCMYK -sColorConversionStrategy=CMYK";
            string sizeArgs = GetPageSizeArgs();
            string pdfFontArgs = "-dEmbedAllFonts=true -dSubsetFonts=true";
            string psFontArgs = GetFontArgs();

            int successCount = 0;
            string originalTitle = this.Text; // Запоминаем название программы

            // Временная папка
            string tempWorkDir = Path.Combine(Path.GetTempPath(), "PDFtoPS_Work");
            if (!Directory.Exists(tempWorkDir)) Directory.CreateDirectory(tempWorkDir);

            foreach (ListViewItem item in listViewFiles.Items)
            {
                string inputPath = item.Tag.ToString();
                string fileName = Path.GetFileNameWithoutExtension(inputPath);

                string safeInput = Path.Combine(tempWorkDir, $"in_{Guid.NewGuid().ToString().Substring(0, 8)}.pdf");
                string safeNorm = Path.Combine(tempWorkDir, $"norm_{Guid.NewGuid().ToString().Substring(0, 8)}.pdf");
                string finalPsPath = Path.Combine(outputDir, fileName + ".ps");

                try
                {
                    File.Copy(inputPath, safeInput, true);

                    // ШАГ 1: НОРМАЛИЗАЦИЯ
                    // Пишем статус в заголовок окна
                    this.Text = $"Нормализация: {fileName}...";
                    Application.DoEvents(); // Чтобы интерфейс не завис

                    string pass1Args = $"-dNOPAUSE -dBATCH -sDEVICE=pdfwrite -dCompatibilityLevel=1.4 " +
                                       $"{pdfSettings} {colorArgs} {pdfFontArgs} " +
                                       $"-sOutputFile=\"{safeNorm}\" \"{safeInput}\"";

                    if (!RunGhostscript(gsPath, pass1Args, out string err1))
                    {
                        throw new Exception($"Ошибка pdfwrite (Pass 1):\n{err1}");
                    }

                    // ШАГ 2: ГЕНЕРАЦИЯ PS
                    this.Text = $"Генерация PS: {fileName}...";
                    Application.DoEvents();

                    string profileArgs = profiles[comboBoxProfiles.SelectedItem.ToString()];

                    string pass2Args = $"-dNOPAUSE -dBATCH {profileArgs} -r2400 {sizeArgs} {psFontArgs} " +
                                       $"-sOutputFile=\"{finalPsPath}\" \"{safeNorm}\"";

                    if (RunGhostscript(gsPath, pass2Args, out string err2))
                    {
                        successCount++;
                    }
                    else
                    {
                        throw new Exception($"Ошибка ps2write (Pass 2):\n{err2}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Файл: {fileName}\n\n{ex.Message}", "Сбой", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    try { if (File.Exists(safeInput)) File.Delete(safeInput); } catch { }
                    try { if (File.Exists(safeNorm)) File.Delete(safeNorm); } catch { }
                }

                // Двигаем прогресс-бар
                progressBar1.PerformStep();
            }

            try { Directory.Delete(tempWorkDir, true); } catch { }

            // Возвращаем заголовок и сбрасываем бар
            this.Text = originalTitle;
            progressBar1.Value = 0;

            MessageBox.Show($"Готово! Успешно: {successCount} из {listViewFiles.Items.Count}");
            if (successCount > 0) Process.Start("explorer.exe", outputDir);
        }

        // --- НОВЫЙ МЕТОД ДЛЯ ОПРЕДЕЛЕНИЯ ПРОФИЛЯ ---
        private string GetPdfSettings()
        {
            // Определяем пресет на основе выбора DPI в интерфейсе.
            // /printer = 300 dpi (High Quality)
            // /prepress = Без даунсемплинга (Original Quality)
            // /ebook = 150 dpi
            // /screen = 72 dpi

            // Смотрим на cmbColorDPI (индекс 1 это "300 (Print)")
            int dpiIndex = cmbColorDPI.SelectedIndex;

            switch (dpiIndex)
            {
                case 0: return "-dPDFSETTINGS=/prepress"; // Без изменений (высокое качество)
                case 1: return "-dPDFSETTINGS=/printer";  // 300 dpi (СТАНДАРТ) - самое стабильное
                case 2: return "-dPDFSETTINGS=/ebook";    // 150 dpi
                case 3: return "-dPDFSETTINGS=/screen";   // 72 dpi
                default: return "-dPDFSETTINGS=/printer";
            }
        }

        // ОСТАВЛЯЕМ МЕТОД ЗАПУСКА БЕЗ ИЗМЕНЕНИЙ
        private bool RunGhostscript(string gsPath, string arguments, out string errorMsg)
        {
            errorMsg = "";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = gsPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (Process p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    errorMsg = $"ExitCode: {p.ExitCode}\nStdErr: {error}\nStdOut: {output}";
                    Clipboard.SetText($"CMD: \"{gsPath}\" {arguments}\n\nERR: {errorMsg}");
                    return false;
                }
                return true;
            }
        }

        // --- ОСТАЛЬНЫЕ МЕТОДЫ СПИСКА (без изменений) ---
        private void UpdateRowNumbers() { for (int i = 0; i < listViewFiles.Items.Count; i++) listViewFiles.Items[i].Text = (i + 1).ToString(); }
        private string GetFileSize(string path) { try { long b = new FileInfo(path).Length; return b >= 1048576 ? (b / 1048576.0).ToString("0.##") + " MB" : (b / 1024.0).ToString("0.##") + " KB"; } catch { return "0 KB"; } }
        private void AddFileToListView(string path) { if (Path.GetExtension(path).ToLower() != ".pdf") return; FileInfo fi = new FileInfo(path); ListViewItem item = new ListViewItem(""); item.SubItems.Add(fi.Name); item.SubItems.Add(GetFileSize(path)); item.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm")); item.Tag = path; listViewFiles.Items.Add(item); UpdateRowNumbers(); }
        private void btnAdd_Click(object sender, EventArgs e) { using (OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "PDF Files|*.pdf" }) { if (ofd.ShowDialog() == DialogResult.OK) foreach (string f in ofd.FileNames) AddFileToListView(f); } }
        private void btnRemove_Click(object sender, EventArgs e) { if (listViewFiles.SelectedItems.Count == 0) return; int l = listViewFiles.SelectedIndices[0]; foreach (ListViewItem i in listViewFiles.SelectedItems.Cast<ListViewItem>().ToList()) i.Remove(); UpdateRowNumbers(); if (listViewFiles.Items.Count > 0) { if (l >= listViewFiles.Items.Count) l = listViewFiles.Items.Count - 1; listViewFiles.Items[l].Selected = true; listViewFiles.Items[l].Focused = true; } }
        private void listViewFiles_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back) { e.Handled = true; btnRemove_Click(sender, e); } }
        private void btnSort_Click(object sender, EventArgs e) { var items = listViewFiles.Items.Cast<ListViewItem>().ToList(); items = isAscending ? items.OrderBy(i => i.SubItems[1].Text).ToList() : items.OrderByDescending(i => i.SubItems[1].Text).ToList(); isAscending = !isAscending; listViewFiles.Items.Clear(); listViewFiles.Items.AddRange(items.ToArray()); UpdateRowNumbers(); }
        private void listViewFiles_ColumnClick(object sender, ColumnClickEventArgs e) { if (e.Column == 0) return; if (e.Column == lastSortColumn) lastSortOrder = (lastSortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending; else { lastSortOrder = SortOrder.Ascending; lastSortColumn = e.Column; } SetSortArrow(e.Column, lastSortOrder); var items = listViewFiles.Items.Cast<ListViewItem>().ToList(); items = lastSortOrder == SortOrder.Ascending ? items.OrderBy(x => x.SubItems[e.Column].Text).ToList() : items.OrderByDescending(x => x.SubItems[e.Column].Text).ToList(); listViewFiles.BeginUpdate(); listViewFiles.Items.Clear(); listViewFiles.Items.AddRange(items.ToArray()); UpdateRowNumbers(); listViewFiles.EndUpdate(); }
        private void listViewFiles_ItemDrag(object sender, ItemDragEventArgs e) => listViewFiles.DoDragDrop(e.Item, DragDropEffects.Move);
        private void listViewFiles_DragOver(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Move; Point cp = listViewFiles.PointToClient(new Point(e.X, e.Y)); ListViewItem h = listViewFiles.GetItemAt(cp.X, cp.Y); if (h != null) { if (h.Index != lastInsertionIndex) { lastInsertionIndex = h.Index; SetInsertionMark(h.Index); } } else SetInsertionMark(listViewFiles.Items.Count); }
        private void listViewFiles_DragLeave(object sender, EventArgs e) { lastInsertionIndex = -1; SetInsertionMark(-1); }
        private void listViewFiles_DragDrop(object sender, DragEventArgs e) { lastInsertionIndex = -1; SetInsertionMark(-1); if (e.Data.GetDataPresent(DataFormats.FileDrop)) { foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop)) AddFileToListView(f); } else if (e.Data.GetDataPresent(typeof(ListViewItem))) { Point cp = listViewFiles.PointToClient(new Point(e.X, e.Y)); ListViewItem drag = (ListViewItem)e.Data.GetData(typeof(ListViewItem)); ListViewItem drop = listViewFiles.GetItemAt(cp.X, cp.Y); if (drop != null) { if (drag.Index != drop.Index) { listViewFiles.Items.Remove(drag); listViewFiles.Items.Insert(drop.Index, drag); } } else { listViewFiles.Items.Remove(drag); listViewFiles.Items.Add(drag); } UpdateRowNumbers(); } }
        private void btnMoveUp_Click(object sender, EventArgs e) { if (listViewFiles.SelectedItems.Count == 0) return; ListViewItem i = listViewFiles.SelectedItems[0]; int x = i.Index; if (x > 0) { listViewFiles.Items.RemoveAt(x); listViewFiles.Items.Insert(x - 1, i); UpdateRowNumbers(); } }
        private void btnMoveDown_Click(object sender, EventArgs e) { if (listViewFiles.SelectedItems.Count == 0) return; ListViewItem i = listViewFiles.SelectedItems[0]; int x = i.Index; if (x < listViewFiles.Items.Count - 1) { listViewFiles.Items.RemoveAt(x); listViewFiles.Items.Insert(x + 1, i); UpdateRowNumbers(); } }
        private void btnBrowse_Click(object sender, EventArgs e) { using (FolderBrowserDialog f = new FolderBrowserDialog()) if (f.ShowDialog() == DialogResult.OK) txtOutputPath.Text = f.SelectedPath; }
        private void btnExit_Click(object sender, EventArgs e) => Application.Exit();
        private void SetSortArrow(int columnIndex, SortOrder order) { IntPtr h = SendMessage(listViewFiles.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero); for (int i = 0; i < listViewFiles.Columns.Count; i++) { HDITEM t = new HDITEM(); t.mask = HDI_FORMAT; IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(t)); Marshal.StructureToPtr(t, p, false); SendMessage(h, HDM_GETITEM, (IntPtr)i, p); t = (HDITEM)Marshal.PtrToStructure(p, typeof(HDITEM)); t.fmt &= ~(HDF_SORTUP | HDF_SORTDOWN); t.fmt |= HDF_STRING; if (i == columnIndex) { if (order == SortOrder.Ascending) t.fmt |= HDF_SORTUP; else t.fmt |= HDF_SORTDOWN; } Marshal.StructureToPtr(t, p, false); SendMessage(h, HDM_SETITEM, (IntPtr)i, p); Marshal.FreeHGlobal(p); } }
    }
}
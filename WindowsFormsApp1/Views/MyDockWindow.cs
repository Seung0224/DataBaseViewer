using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Zuby.ADGV;

namespace WindowsFormsApp1
{
    internal class MyDockWindow : DockContent
    {
        private string baseTitle;
        DateTime startDate, endDate;

        public AdvancedDataGridView GridView { get; private set; }

        private bool isNoteMode;
        private TextBox noteBox;

        public MyDockWindow(string title, DateTime startDate, DateTime endDate, bool isNote = false)
        {
            this.baseTitle = title;
            this.isNoteMode = isNote;
            this.startDate = startDate;
            this.endDate = endDate;


            this.Text = title + Form1.GetDateRangeString(startDate, endDate);

            if (isNoteMode)
            {
                noteBox = new TextBox
                {
                    Multiline = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Arial", 10),
                    ScrollBars = ScrollBars.Both,
                    AcceptsReturn = true,
                    AcceptsTab = true
                };
                Controls.Add(noteBox);
                LoadNoteText();
            }
            else
            {
                GridView = new AdvancedDataGridView
                {
                    Dock = DockStyle.Fill,
                    FilterAndSortEnabled = true,
                    AutoGenerateColumns = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };

                Controls.Add(GridView);
            }
        }
        public void UpdateTitle(string tableName, DateTime start, DateTime end)
        {
            this.Text = tableName + Form1.GetDateRangeString(start, end);
        }

        public void SetDataSource(object data)
        {
            GridView.DataSource = data;
        }

        public void ApplyGridStyle()
        {
            GridView.SetDoubleBuffered();

            GridView.RowTemplate.Height = 28;
            GridView.DefaultCellStyle.Font = new Font("Arial", 10);
            GridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            GridView.ColumnHeadersDefaultCellStyle.BackColor = Color.SteelBlue;
            GridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            GridView.EnableHeadersVisualStyles = false;
            GridView.DefaultCellStyle.SelectionBackColor = Color.LightSkyBlue;
            GridView.DefaultCellStyle.SelectionForeColor = Color.Black;
            GridView.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            GridView.GridColor = Color.LightGray;
            GridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            GridView.RowHeadersVisible = false;
            GridView.ColumnHeaderMouseClick += GridView_ColumnHeaderMouseClick;
            GridView.CellClick += GridView_CellClick;
        }
        private void GridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            foreach (DataGridViewColumn col in GridView.Columns)
            {
                col.HeaderCell.Style.BackColor = Color.SteelBlue; // 기본색으로 초기화
            }

            GridView.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.LimeGreen; // 클릭한 컬럼 강조
        }
        private void GridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 행 클릭 시 헤더 강조 색 원래대로
            if (e.RowIndex >= 0)
            {
                ResetAllHeaderColors();
            }
        }
        private void ResetAllHeaderColors()
        {
            foreach (DataGridViewColumn col in GridView.Columns)
            {
                col.HeaderCell.Style.BackColor = Color.SteelBlue; // 기본 스타일 색
            }
        }

        public void EnableHeaderContextMenu()
        {
            var menu = new ContextMenuStrip();

            var renameItem = new ToolStripMenuItem("이름 바꾸기");
            var deleteItem = new ToolStripMenuItem("열 삭제");
            var insertItem = new ToolStripMenuItem("열 추가");

            int clickedColumnIndex = -1;

            GridView.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    clickedColumnIndex = e.ColumnIndex;
                    menu.Show(Cursor.Position);
                }
            };

            renameItem.Click += (s, e) =>
            {
                if (clickedColumnIndex < 0) return;
                var col = GridView.Columns[clickedColumnIndex];
                string newName = Prompt.ShowDialog("새 이름:", "열 이름 바꾸기", col.HeaderText);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    col.HeaderText = newName;
                }
            };

            deleteItem.Click += (s, e) =>
            {
                if (clickedColumnIndex < 0) return;
                var col = GridView.Columns[clickedColumnIndex];
                GridView.Columns.Remove(col);
            };

            insertItem.Click += (s, e) =>
            {
                string newColName = Prompt.ShowDialog("새 이름:", "열 추가", $"NUM_{GridView.Columns.Count}");
                if (string.IsNullOrWhiteSpace(newColName)) return;

                var newCol = new DataGridViewTextBoxColumn
                {
                    HeaderText = newColName,
                    Name = newColName,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
                };

                GridView.Columns.Add(newCol);

                // 기존 행에 빈 값 추가
                foreach (DataGridViewRow row in GridView.Rows)
                {
                    if (!row.IsNewRow)
                        row.Cells[newCol.Index].Value = "";
                }
            };

            menu.Items.AddRange(new ToolStripItem[] { renameItem, insertItem, deleteItem });
        }
        public static class Prompt
        {
            public static string ShowDialog(string text, string caption, string defaultValue = "")
            {
                Form prompt = new Form()
                {
                    Width = 400,
                    Height = 150,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = caption,
                    StartPosition = FormStartPosition.CenterScreen
                };

                Label textLabel = new Label() { Left = 10, Top = 20, Text = text };
                TextBox textBox = new TextBox() { Left = 10, Top = 50, Width = 360, Text = defaultValue };
                Button confirmation = new Button() { Text = "확인", Left = 280, Width = 90, Top = 80, DialogResult = DialogResult.OK };

                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : defaultValue;
            }
        }

        protected override string GetPersistString()
        {
            return $"{GetType().FullName}|{baseTitle}|{this.isNoteMode}|{startDate:yyyyMMdd}|{endDate:yyyyMMdd}";
        }

        public void SaveNoteText()
        {
            if (!isNoteMode) return;

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LayoutData");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"Note.txt");
            File.WriteAllText(path, noteBox.Text);
        }

        private void LoadNoteText()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LayoutData");
            var path = Path.Combine(folder, $"Note_{this.Text}.txt");
            if (File.Exists(path))
            {
                noteBox.Text = File.ReadAllText(path);
            }
        }
    }
}

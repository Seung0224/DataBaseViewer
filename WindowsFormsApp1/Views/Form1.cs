using BrightIdeasSoftware;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        string dbPath = "";
        string layoutPath = @"D:\layout.xml";

        private string GetDbPathForDate(DateTime date)
        {
            return $@"D:\TEST_MODEL\{date:yyyyMMdd}\History.db";
        }

        #region Fields
        private ContextMenuStrip treeContextMenu;
        private TreeNodeModel rightClickedNode;

        private readonly VS2015BlueTheme _blueTheme = new VS2015BlueTheme();
        private readonly VS2015LightTheme _lightTheme = new VS2015LightTheme();
        private readonly VS2015DarkTheme _theme = new VS2015DarkTheme();

        public class TreeNodeModel
        {
            public string Text { get; set; }
            public List<TreeNodeModel> Children { get; set; } = new List<TreeNodeModel>();
        }
        #endregion

        #region Initialization
        public Form1()
        {
            dbPath = GetDbPathForDate(DateTime.Today);
            InitializeComponent();

            treeContextMenu = new ContextMenuStrip();
            treeContextMenu.Items.Add("📈 Graph View", null, ShowGraphDock);

            treeContextMenu.Items.Add("📋 DataBase View", null, ShowDbDock);
            treeContextMenu.Items.Add("📝 Notes", null, ShowMemoDock);

            TLV1.MouseUp += TLV1_MouseUp;
        
            dockPanel1.Theme = _theme;
        }
        #endregion

        #region Functions
        
        private DataTable MergeDataTables(List<DataTable> tables)
        {
            if (tables == null || tables.Count == 0)
                return new DataTable();

            var result = new DataTable();
            foreach (DataColumn col in tables[0].Columns)
                result.Columns.Add(col.ColumnName, col.DataType);
            result.Constraints.Clear();

            foreach (var tbl in tables)
                foreach (DataRow row in tbl.Rows)
                    result.Rows.Add(row.ItemArray);

            return result;
        }

        // 오늘 날짜면 " (MM-dd)" 형식, 범위면 " (MM-dd~MM-dd)" 형식으로 반환
        public static string GetDateRangeString(DateTime start, DateTime end)
        {
            // 선택 범위가 뒤집혔을 경우 정렬
            if (start > end)
            {
                var temp = start;
                start = end;
                end = temp;
            }

            return start == end
                ? $" ({start:MM-dd})"
                : $" ({start:MM-dd}~{end:MM-dd})";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(layoutPath))
                {
                    dockPanel1.LoadFromXml(layoutPath, DeserializeDockContent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("레이아웃 로드 실패: " + ex.Message);
            }

            if (!File.Exists(dbPath))
            {
                MessageBox.Show("DB 파일이 존재하지 않습니다: " + dbPath);
                return;
            }

            try
            {
                var treeItems = LoadTreeFromDatabase(dbPath);

                TLV1.CanExpandGetter = obj =>
                {
                    var node = obj as TreeNodeModel;
                    return node != null && node.Children.Any();
                };

                TLV1.ChildrenGetter = obj =>
                {
                    var node = obj as TreeNodeModel;
                    return node.Children;
                };

                TLV1.HideSelection = false;
                TLV1.Columns.Clear();
                TLV1.Columns.Add(new OLVColumn("DataBase Table", "Text") { Width = 600 });

                TLV1.RowHeight = 28;
                TLV1.FullRowSelect = true;
                TLV1.HotItemStyle = new HotItemStyle
                {
                    BackColor = Color.LightBlue,
                    ForeColor = Color.Black,
                    Font = new Font("Arial", 10, FontStyle.Bold)
                };

                var headerStyle = new HeaderFormatStyle();
                headerStyle.SetBackColor(Color.SteelBlue);
                headerStyle.SetForeColor(Color.White);
                
                TLV1.HeaderFormatStyle = headerStyle;

                TLV1.HeaderUsesThemes = false;

                TLV1.Roots = treeItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show("데이터 로딩 오류: " + ex.Message);
            }
        }

        private void ShowMemoDock(object sender, EventArgs e)
        {
            if (rightClickedNode == null) return;

            string title = $"Notes: {rightClickedNode.Text}";
            var memoWindow = new MyDockWindow(title, DateTime.Today, DateTime.Today, true);
            memoWindow.Show(dockPanel1, DockState.Document);
            memoWindow.Text = title + GetDateRangeString(DateTime.Today, DateTime.Today);
        }
        public static string RemoveLeadingNonAlphanumeric(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            int index = 0;
            while (index < input.Length && !char.IsLetterOrDigit(input[index]))
            {
                index++;
            }

            return input.Substring(index);
        }

        private void ShowDbDock(object sender, EventArgs e)
        {
            if (rightClickedNode == null) return;

            string tableName = RemoveLeadingNonAlphanumeric(rightClickedNode.Text);
            DateTime start = MC1.SelectionStart.Date;
            DateTime end = MC1.SelectionEnd.Date;

            // 단일 날짜이고 저장된 날짜가 오늘이 아니면 오늘로 대체
            if (start == end && start != DateTime.Today)
            {
                start = DateTime.Today;
                end = DateTime.Today;
            }

            try
            {
                List<DataTable> tables = new List<DataTable>();
                List<DateTime> missingDates = new List<DateTime>();

                for (var dt = start; dt <= end; dt = dt.AddDays(1))
                {
                    string path = GetDbPathForDate(dt);
                    if (!File.Exists(path))
                    {
                        missingDates.Add(dt);
                        continue;
                    }
                    tables.Add(LoadTableData(path, tableName));
                }

                if (missingDates.Any())
                {
                    string msg = string.Join(", ", missingDates.Select(d => d.ToString("yyyy-MM-dd")));
                    MessageBox.Show($"다음 날짜에 DB가 존재하지 않습니다: {msg}", "DB 누락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (!tables.Any())
                {
                    MessageBox.Show($"[{tableName}] 데이터가 없습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var finalTable = MergeDataTables(tables);
                var dockWindow = new MyDockWindow(tableName, start, end, false);
                dockWindow.SetDataSource(finalTable);
                dockWindow.ApplyGridStyle();
                dockWindow.EnableHeaderContextMenu();
                dockWindow.UpdateTitle(tableName, start, end);
                dockWindow.Show(dockPanel1, DockState.Document);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[{tableName}] 데이터 로딩 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowGraphDock(object sender, EventArgs e)
        {
            if (rightClickedNode == null) return;

            string tableName = RemoveLeadingNonAlphanumeric(rightClickedNode.Text);
            DateTime start = MC1.SelectionStart.Date;
            DateTime end = MC1.SelectionEnd.Date;

            // 단일 날짜이고 저장된 날짜가 오늘이 아니면 오늘로 대체
            if (start == end && start != DateTime.Today)
            {
                start = DateTime.Today;
                end = DateTime.Today;
            }

            try
            {
                List<DateTime> missingDates = new List<DateTime>();

                if (tableName.Equals("AlignInfos", StringComparison.OrdinalIgnoreCase))
                {
                    var tables = new List<DataTable>();
                    for (var d = start; d <= end; d = d.AddDays(1))
                    {
                        string path = GetDbPathForDate(d);
                        if (!File.Exists(path))
                        {
                            missingDates.Add(d);
                            continue;
                        }
                        tables.Add(LoadTableData(path, tableName));
                    }

                    if (missingDates.Any())
                    {
                        string msg = string.Join(", ", missingDates.Select(d => d.ToString("yyyy-MM-dd")));
                        MessageBox.Show($"다음 날짜에 DB가 존재하지 않습니다: {msg}", "DB 누락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    if (!tables.Any())
                    {
                        MessageBox.Show("선택된 기간에 AlignInfos 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var merged = MergeDataTables(tables);
                    var winA = new MyChartDockWindow(tableName, merged, start, end);
                    winA.UpdateTitle(tableName, start, end);
                    winA.Show(dockPanel1, DockState.Document);
                }
                else if (tableName.Equals("ProductInfos", StringComparison.OrdinalIgnoreCase))
                {
                    var dateLabels = new List<string>();
                    var okCounts = new List<int>();
                    var ngCounts = new List<int>();

                    for (var d = start; d <= end; d = d.AddDays(1))
                    {
                        string path = GetDbPathForDate(d);
                        if (!File.Exists(path))
                        {
                            missingDates.Add(d);
                            continue;
                        }

                        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}"))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "SELECT COUNT(*) FROM ProductInfos WHERE Judge = 'OK'";
                                int ok = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                                cmd.CommandText = "SELECT COUNT(*) FROM ProductInfos WHERE Judge = 'NG'";
                                int ng = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                                dateLabels.Add(d.ToString("yyyy-MM-dd"));
                                okCounts.Add(ok);
                                ngCounts.Add(ng);
                            }
                        }
                    }

                    if (missingDates.Any())
                    {
                        string msg = string.Join(", ", missingDates.Select(d => d.ToString("yyyy-MM-dd")));
                        MessageBox.Show($"다음 날짜에 DB가 존재하지 않습니다: {msg}", "DB 누락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    if (!dateLabels.Any())
                    {
                        MessageBox.Show("선택된 기간에 ProductInfos 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var winP = new MyPDChartDockWindow(tableName, dateLabels, okCounts, ngCounts, start, end);
                    winP.UpdateTitle(tableName, start, end);
                    winP.Show(dockPanel1, DockState.Document);
                }
                else
                {
                    MessageBox.Show("이 항목은 그래프를 지원하지 않습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[{tableName}] 그래프 생성 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveLayout()
        {
            try
            {
                dockPanel1.SaveAsXml(layoutPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("레이아웃 저장 실패: " + ex.Message);
            }
        }

        private IDockContent DeserializeDockContent(string persistString)
        {
            var parts = persistString.Split('|');
            var typeName = parts[0];
            var baseTitle = parts[1];

            // 1) MyDockWindow (데이터/메모 창)
            if (typeName == typeof(MyDockWindow).FullName)
            {
                var isNote = bool.TryParse(parts[2], out var noteFlag) && noteFlag;
                DateTime start = parts.Length > 3 ? DateTime.ParseExact(parts[3], "yyyyMMdd", null) : DateTime.Today;
                DateTime end = parts.Length > 4 ? DateTime.ParseExact(parts[4], "yyyyMMdd", null) : start;

                // 날짜 갱신: 메모는 제외
                if (!isNote && start == end && start != DateTime.Today)
                {
                    start = DateTime.Today;
                    end = DateTime.Today;
                }

                var dock = new MyDockWindow(baseTitle, start, end, isNote);
                dock.UpdateTitle(baseTitle, start, end);

                if (!isNote)
                {
                    var tables = new List<DataTable>();
                    for (var d = start; d <= end; d = d.AddDays(1))
                    {
                        string path = GetDbPathForDate(d);
                        if (File.Exists(path))
                            tables.Add(LoadTableData(path, baseTitle));
                    }

                    var merged = MergeDataTables(tables);
                    dock.SetDataSource(merged);
                    dock.ApplyGridStyle();
                    dock.EnableHeaderContextMenu();
                }

                return dock;
            }

            // 2) MyChartDockWindow (AlignInfos 차트)
            else if (typeName == typeof(MyChartDockWindow).FullName)
            {
                DateTime start = DateTime.ParseExact(parts[2], "yyyyMMdd", null);
                DateTime end = DateTime.ParseExact(parts[3], "yyyyMMdd", null);
                bool autoUpdate = parts.Length > 4 && bool.TryParse(parts[4], out var flag1) && flag1;

                if (autoUpdate && start == end && start != DateTime.Today)
                {
                    start = DateTime.Today;
                    end = DateTime.Today;
                }

                var tables = new List<DataTable>();
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    string path = GetDbPathForDate(d);
                    if (File.Exists(path))
                        tables.Add(LoadTableData(path, baseTitle));
                }

                var merged = MergeDataTables(tables);
                var win = new MyChartDockWindow(baseTitle, merged, start, end, autoUpdate);
                win.UpdateTitle(baseTitle, start, end);
                return win;
            }

            // 3) MyPDChartDockWindow (ProductInfos 차트)
            else if (typeName == typeof(MyPDChartDockWindow).FullName)
            {
                DateTime start = DateTime.ParseExact(parts[2], "yyyyMMdd", null);
                DateTime end = DateTime.ParseExact(parts[3], "yyyyMMdd", null);
                bool autoUpdate = parts.Length > 4 && bool.TryParse(parts[4], out var flag2) && flag2;

                if (autoUpdate && start == end && start != DateTime.Today)
                {
                    start = DateTime.Today;
                    end = DateTime.Today;
                }

                var dateLabels = new List<string>();
                var okCounts = new List<int>();
                var ngCounts = new List<int>();

                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    string path = GetDbPathForDate(d);
                    if (!File.Exists(path)) continue;

                    using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT COUNT(*) FROM ProductInfos WHERE Judge = 'OK'";
                            int ok = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                            cmd.CommandText = "SELECT COUNT(*) FROM ProductInfos WHERE Judge = 'NG'";
                            int ng = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                            dateLabels.Add(d.ToString("yyyy-MM-dd"));
                            okCounts.Add(ok);
                            ngCounts.Add(ng);
                        }
                    }
                }

                var win = new MyPDChartDockWindow(baseTitle, dateLabels, okCounts, ngCounts, start, end, autoUpdate);
                win.UpdateTitle(baseTitle, start, end);
                return win;
            }

            return null;
        }

        private void TLV1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = TLV1.OlvHitTest(e.X, e.Y);
                if (hit.Item != null)
                {
                    TLV1.SelectedObject = hit.RowObject;
                    rightClickedNode = hit.RowObject as TreeNodeModel;
                    treeContextMenu.Show(TLV1, e.Location);
                }
            }
        }

        private List<TreeNodeModel> LoadTreeFromDatabase(string dbPath)
        {
            var result = new List<TreeNodeModel>();

            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);

                            string emoji = "📄";
                            if (tableName.IndexOf("Align", StringComparison.OrdinalIgnoreCase) >= 0) emoji = "📐";
                            else if (tableName.IndexOf("Product", StringComparison.OrdinalIgnoreCase) >= 0) emoji = "📦";
                            else if (tableName.IndexOf("Result", StringComparison.OrdinalIgnoreCase) >= 0) emoji = "📊";

                            var tableNode = new TreeNodeModel { Text = emoji + " " + tableName };

                            using (var dataCmd = connection.CreateCommand())
                            {
                                // 데이터 조회 쿼리: 최대 10개 행만 가져오기
                                dataCmd.CommandText = $"SELECT * FROM [{tableName}] LIMIT 10";

                                using (var dataReader = dataCmd.ExecuteReader())
                                {
                                    while (dataReader.Read())
                                    {
                                        var cols = new List<string>();
                                        for (int i = 0; i < dataReader.FieldCount; i++)
                                        {
                                            var name = dataReader.GetName(i);
                                            var value = dataReader.IsDBNull(i) ? "null" : dataReader.GetValue(i).ToString();
                                            cols.Add($"{name}: {value}");
                                        }

                                        tableNode.Children.Add(new TreeNodeModel { Text = string.Join(", ", cols) });
                                    }
                                }
                            }

                            result.Add(tableNode);
                        }
                    }
                }
            }

            return result;
        }
        private DataTable LoadTableData(string dbPath, string tableName)
        {
            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT * FROM [{tableName}] LIMIT 100";

                using (var reader = cmd.ExecuteReader())
                {
                    var original = new DataTable();
                    original.Load(reader);

                    // 새 테이블 생성: 모든 컬럼 AllowDBNull = true
                    var result = new DataTable();
                    foreach (DataColumn col in original.Columns)
                    {
                        var newCol = new DataColumn(col.ColumnName, typeof(string));
                        newCol.AllowDBNull = true;
                        result.Columns.Add(newCol);
                    }

                    string csvColumnName = null;

                    // 쉼표 포함된 열 찾기
                    foreach (DataColumn col in original.Columns)
                    {
                        foreach (DataRow row in original.Rows)
                        {
                            var val = Convert.ToString(row[col.ColumnName]);
                            if (!string.IsNullOrWhiteSpace(val) && val.Contains(","))
                            {
                                csvColumnName = col.ColumnName;
                                break;
                            }
                        }
                        if (csvColumnName != null) break;
                    }

                    if (csvColumnName == null)
                        return original;

                    // 쉼표 최대 개수
                    int maxParts = 0;
                    foreach (DataRow row in original.Rows)
                    {
                        string raw = Convert.ToString(row[csvColumnName]) ?? "";
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        int parts = raw.Split(',').Length;
                        if (parts > maxParts)
                            maxParts = parts;
                    }

                    // NUM_0 ~ NUM_n 컬럼 추가
                    for (int i = 0; i < maxParts; i++)
                    {
                        result.Columns.Add(new DataColumn($"NUM_{i}", typeof(string)) { AllowDBNull = true });
                    }

                    // 행 데이터 복사 및 분해
                    foreach (DataRow oldRow in original.Rows)
                    {
                        var newRow = result.NewRow();

                        foreach (DataColumn col in original.Columns)
                        {
                            newRow[col.ColumnName] = Convert.ToString(oldRow[col.ColumnName]) ?? "";
                        }

                        string raw = Convert.ToString(oldRow[csvColumnName]);
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            var parts = raw.Split(',');
                            for (int i = 0; i < parts.Length; i++)
                            {
                                newRow[$"NUM_{i}"] = parts[i].Trim();
                            }
                        }

                        result.Rows.Add(newRow);
                    }

                    return result;
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveLayout();

            foreach (var content in dockPanel1.Contents)
            {
                if (content is MyDockWindow dock)
                {
                    dock.SaveNoteText();
                }
            }
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            // 랜덤 생성기
            Random rand = new Random();

            double x = rand.NextDouble() * 100; // 0 ~ 100 사이 실수
            double y = rand.NextDouble() * 100;
            double t = rand.NextDouble() * 100;

            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                INSERT INTO AlignInfos (InspectionTime, AlignX, AlignY, AlignT)
                VALUES (@time, @x, @y, @t);
            ";

                    cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@x", Math.Round(x, 2));
                    cmd.Parameters.AddWithValue("@y", Math.Round(y, 2));
                    cmd.Parameters.AddWithValue("@t", Math.Round(t, 2));

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show($"AlignInfos 삽입 완료!\\nX={x:F2}, Y={y:F2}, T={t:F2}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
        //    Random rand = new Random();

        //    string judge = rand.Next(2) == 0 ? "OK" : "NG";

        //    // 시간 포맷은 yyyy-MM-dd HH:mm:ss 형식으로 현재 시간 기준 생성
        //    string materialInputTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        //    string processingTime = DateTime.Now.AddSeconds(rand.Next(1, 10)).ToString("yyyy-MM-dd HH:mm:ss");

        //    using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
        //    {
        //        conn.Open();

        //        using (var cmd = conn.CreateCommand())
        //        {
        //            cmd.CommandText = @"
        //        INSERT INTO ProductInfos (Judge, MaterialInputTime, ProcessingTimeMs)
        //        VALUES (@judge, @inputTime, @procTime);
        //    ";

        //            cmd.Parameters.AddWithValue("@judge", judge);
        //            cmd.Parameters.AddWithValue("@inputTime", materialInputTime);
        //            cmd.Parameters.AddWithValue("@procTime", processingTime);

        //            cmd.ExecuteNonQuery();
        //        }

        //        MessageBox.Show($"ProductInfos 삽입 완료!\nJudge={judge}");
        //    }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Random rand = new Random();

            string judge = rand.Next(2) == 0 ? "OK" : "NG";

            // 시간 포맷은 yyyy-MM-dd HH:mm:ss 형식으로 현재 시간 기준 생성
            string materialInputTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string processingTime = DateTime.Now.AddSeconds(rand.Next(1, 10)).ToString("yyyy-MM-dd HH:mm:ss");

            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    INSERT INTO ProductInfos (Judge, MaterialInputTime, ProcessingTimeMs)
                    VALUES (@judge, @inputTime, @procTime);
                ";

                    cmd.Parameters.AddWithValue("@judge", judge);
                    cmd.Parameters.AddWithValue("@inputTime", materialInputTime);
                    cmd.Parameters.AddWithValue("@procTime", processingTime);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show($"ProductInfos 삽입 완료!\nJudge={judge}");
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
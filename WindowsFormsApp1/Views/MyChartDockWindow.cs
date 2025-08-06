using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using System.Windows.Media;
using WeifenLuo.WinFormsUI.Docking;

namespace WindowsFormsApp1
{
    internal class MyChartDockWindow : DockContent
    {
        private string baseTitle;
        private DateTime startDate, endDate;

        private LiveCharts.WinForms.CartesianChart chartX, chartY, chartT;
        private GearedValues<ObservablePoint> listX, listY, listT;
        private bool autoUpdateDate = true;
        private CheckBox chkAutoUpdate;

        public MyChartDockWindow(string title, DataTable sourceTable, DateTime startDate, DateTime endDate, bool autoUpdateDate = true)
        {
            this.baseTitle = title;
            this.DockAreas = DockAreas.Document;
            this.startDate = startDate;
            this.endDate = endDate;
            this.autoUpdateDate = autoUpdateDate;

            this.Text = title + Form1.GetDateRangeString(startDate, endDate);

            chartX = CreateStyledChart("X");
            chartY = CreateStyledChart("Y");
            chartT = CreateStyledChart("T");

            var chartArea = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };

            // 체크박스 추가
            chkAutoUpdate = new CheckBox();
            chkAutoUpdate.Text = "AutoDate";
            chkAutoUpdate.Checked = autoUpdateDate;
            chkAutoUpdate.AutoSize = true;
            chkAutoUpdate.Dock = DockStyle.Top;
            chkAutoUpdate.CheckedChanged += (s, e) =>
            {
                this.autoUpdateDate = chkAutoUpdate.Checked;
            };

            chartArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            chartArea.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            chartArea.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            chartArea.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

            chartArea.Controls.Add(chkAutoUpdate, 0, 0);
            chartArea.Controls.Add(chartX, 0, 1);
            chartArea.Controls.Add(chartY, 0, 2);
            chartArea.Controls.Add(chartT, 0, 3);

            this.Controls.Add(chartArea);

            if (sourceTable != null)
            {
                SetChartData(sourceTable);
            }
        }

        private LiveCharts.WinForms.CartesianChart CreateStyledChart(string label)
        {
            var chart = new LiveCharts.WinForms.CartesianChart
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
                ForeColor = System.Drawing.Color.White,
                DisableAnimations = true,
                Hoverable = false,
                Zoom = ZoomingOptions.Xy,
                Pan = PanningOptions.Xy
            };

            chart.AxisX.Add(new Axis
            {
                Title = $"Align{label}",
                Separator = new Separator { Stroke = System.Windows.Media.Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } },
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.LightGray
            });

            chart.AxisY.Add(new Axis
            {
                Title = $"Value ({label})",
                Separator = new Separator { Stroke = System.Windows.Media.Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } },
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.LightGray
            });

            return chart;
        }

        public void UpdateTitle(string tableName, DateTime start, DateTime end)
        {
            this.Text = tableName + Form1.GetDateRangeString(start, end);
        }

        private void SetChartData(DataTable table)
        {
            listX = new GearedValues<ObservablePoint>();
            listY = new GearedValues<ObservablePoint>();
            listT = new GearedValues<ObservablePoint>();

            var rawX = new List<ObservablePoint>(table.Rows.Count);
            var rawY = new List<ObservablePoint>(table.Rows.Count);
            var rawT = new List<ObservablePoint>(table.Rows.Count);

            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];

                double x = TryParse(row, "AlignX");
                double y = TryParse(row, "AlignY");
                double t = TryParse(row, "AlignT");

                rawX.Add(new ObservablePoint(i + 1, x));
                rawY.Add(new ObservablePoint(i + 1, y));
                rawT.Add(new ObservablePoint(i + 1, t));
            }

            listX.AddRange(rawX);
            listY.AddRange(rawY);
            listT.AddRange(rawT);

            AddSeries(chartX, "AlignX", listX, System.Windows.Media.Brushes.Orange);
            AddSeries(chartY, "AlignY", listY, System.Windows.Media.Brushes.LightBlue);
            AddSeries(chartT, "AlignT", listT, System.Windows.Media.Brushes.LimeGreen);

            AddStatLines(chartX, rawX.Select(p => p.Y));
            AddStatLines(chartY, rawY.Select(p => p.Y));
            AddStatLines(chartT, rawT.Select(p => p.Y));
        }

        private double TryParse(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName))
            {
                var val = row[columnName]?.ToString();
                if (double.TryParse(val, out double result))
                    return result;
            }
            return 0;
        }

        private void AddSeries(LiveCharts.WinForms.CartesianChart chart, string title, GearedValues<ObservablePoint> values, System.Windows.Media.Brush Color)
        {
            chart.Series = new SeriesCollection
            {
                new GLineSeries
                {
                    Title = title,
                    Values = values,
                    StrokeThickness = 2,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    Foreground = System.Windows.Media.Brushes.White
                }
            };
        }

        private void AddStatLines(LiveCharts.WinForms.CartesianChart chart, IEnumerable<double> values)
        {
            if (!values.Any()) return;

            double max = values.Max();
            double min = values.Min();
            double avg = values.Average();
            double std = Math.Sqrt(values.Select(v => Math.Pow(v - avg, 2)).Average());

            chart.AxisY[0].Sections.Clear();
            chart.AxisY[0].Sections.AddRange(new[]
            {
                CreateLine(max, System.Windows.Media.Brushes.Red),        // MAX
                CreateLine(min, System.Windows.Media.Brushes.Green),      // MIN
                CreateLine(avg, System.Windows.Media.Brushes.Orange),     // AVG
            });
        }

        private AxisSection CreateLine(double value, System.Windows.Media.Brush color)
        {
            return new AxisSection
            {
                Value = value,
                SectionWidth = 0,
                Stroke = color,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection(new double[] { 4 })
            };
        }

        protected override string GetPersistString()
        {
            return $"{GetType().FullName}|{baseTitle}|{startDate:yyyyMMdd}|{endDate:yyyyMMdd}|{autoUpdateDate}";
        }

        public bool ShouldAutoUpdateDate()
        {
            return autoUpdateDate;
        }
    }
}

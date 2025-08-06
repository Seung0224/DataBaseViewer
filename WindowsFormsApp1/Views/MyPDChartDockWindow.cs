using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using WeifenLuo.WinFormsUI.Docking;

namespace WindowsFormsApp1
{
    internal class MyPDChartDockWindow : DockContent
    {
        string baseTitle;
        private TableLayoutPanel layoutPanel;
        private DateTime startDate, endDate;
        private LiveCharts.WinForms.CartesianChart chartPD;
        private bool autoUpdateDate = true;
        private CheckBox chkAutoUpdate;

        public MyPDChartDockWindow(string title, List<string> dateLabels, List<int> okCounts, List<int> ngCounts, DateTime startDate, DateTime endDate, bool autoUpdateDate = true)
        {
            this.baseTitle = title;

            if (autoUpdateDate && startDate == endDate && startDate != DateTime.Today)
            {
                startDate = DateTime.Today;
                endDate = DateTime.Today;
            }

            this.startDate = startDate;
            this.endDate = endDate;
            this.autoUpdateDate = autoUpdateDate;

            this.Text = title + Form1.GetDateRangeString(startDate, endDate);
            DockAreas = DockAreas.Document;

            chartPD = CreateStyledChart();

            layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            // 체크박스 생성
            chkAutoUpdate = new CheckBox();
            chkAutoUpdate.Text = "AutoDate";
            chkAutoUpdate.Checked = autoUpdateDate;
            chkAutoUpdate.AutoSize = true;
            chkAutoUpdate.Dock = DockStyle.Top;
            chkAutoUpdate.CheckedChanged += (s, e) =>
            {
                this.autoUpdateDate = chkAutoUpdate.Checked;
            };

            layoutPanel.Controls.Add(chkAutoUpdate, 0, 0);
            layoutPanel.Controls.Add(chartPD, 0, 1);
            Controls.Add(layoutPanel);

            BindAggregatedData(dateLabels, okCounts, ngCounts);
        }

        private LiveCharts.WinForms.CartesianChart CreateStyledChart()
        {
            var chart = new LiveCharts.WinForms.CartesianChart
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
                ForeColor = System.Drawing.Color.White,
                DisableAnimations = true,
                Hoverable = false,
                DataTooltip = null,
            };

            // X축
            var xAxis = new Axis
            {
                Title = "Date",
                Separator = new Separator
                {
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                },
                FontSize = 14,
                Foreground = Brushes.White
            };
            chart.AxisX.Add(xAxis);

            // Y축
            var yAxis = new Axis
            {
                Title = "Count",
                Separator = new Separator
                {
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                },
                FontSize = 14,
                Foreground = Brushes.White
            };
            chart.AxisY.Add(yAxis);

            return chart;
        }

        public void UpdateTitle(string tableName, DateTime start, DateTime end)
        {
            this.Text = tableName + Form1.GetDateRangeString(start, end);
        }

        private void BindAggregatedData(List<string> labels, List<int> okValues, List<int> ngValues)
        {
            chartPD.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title      = "OK",
                    Values     = new ChartValues<int>(okValues),
                    DataLabels = true,
                    LabelPoint = p => $"OK: {p.Y}",
                    Foreground = Brushes.White
                },
                new ColumnSeries
                {
                    Title      = "NG",
                    Values     = new ChartValues<int>(ngValues),
                    DataLabels = true,
                    LabelPoint = p => $"NG: {p.Y}",
                    Foreground = Brushes.White
                }
            };

            var xAx = chartPD.AxisX[0];
            xAx.Labels = labels.ToArray();
            xAx.Foreground = Brushes.White;
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

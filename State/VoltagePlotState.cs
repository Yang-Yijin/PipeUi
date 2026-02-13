////using Microsoft.AspNetCore.Components.Forms;
////using PipeUi.Interfaces;
////using PipeUi.Models;

////namespace PipeUi.State
////{
////    public class VoltagePlotState
////    {
////        private readonly IVoltageService _service;

////        public VoltagePlotState(IVoltageService service)
////        {
////            _service = service;
////        }

////        // 让 View 订阅刷新（MVVM 常用）
////        public event Action? OnChange;
////        private void NotifyChanged() => OnChange?.Invoke();

////        public VoltageLogModel? Model { get; private set; }
////        public VoltageStatistics? Stats { get; private set; }
////        public List<VoltageAnomaly>? Anomalies { get; private set; }

////        public string Message { get; private set; } = "";
////        public double Threshold { get; set; } = 10;

////        // 给 View 用的“待绘图数据”
////        public bool HasPendingChart { get; private set; } = false;
////        public double[] PendingTimes { get; private set; } = Array.Empty<double>();
////        public double[] PendingVoltages { get; private set; } = Array.Empty<double>();

////        public async Task LoadFileAsync(InputFileChangeEventArgs e)
////        {
////            try
////            {
////                var file = e.File;
////                if (file is null)
////                {
////                    Message = "No file selected.";
////                    NotifyChanged();
////                    return;
////                }

////                using var stream = file.OpenReadStream(10 * 1024 * 1024);
////                using var ms = new MemoryStream();
////                await stream.CopyToAsync(ms);
////                ms.Position = 0;

////                Model = _service.LoadDataFromStream(ms);
////                if (Model != null && string.IsNullOrEmpty(Model.FileName))
////                    Model.FileName = file.Name;

////                if (Model == null || Model.DataPoints.Count == 0)
////                {
////                    Message = "Loaded 0 points";
////                    Stats = null;
////                    HasPendingChart = false;
////                    NotifyChanged();
////                    return;
////                }

////                // 统计：你原来 GetStatistics() 只看 CurrentAV，我保留这种行为
////                Stats = Model.GetStatistics();
////                Message = $"Loaded {Model.DataPoints.Count} points";

////                // ✅ 关键修复：成对过滤，保证 times 和 voltages 长度一致
////                // ✅ 兼容图一/图二：优先用 MovingAverageAV（如果存在有效值），否则用 CurrentAV
////                bool hasMA = Model.DataPoints.Any(p => !double.IsNaN(p.MovingAverageAV));

////                var pairs = Model.DataPoints
////                    .Select(p => new
////                    {
////                        t = p.Time,
////                        v = hasMA && !double.IsNaN(p.MovingAverageAV) ? p.MovingAverageAV : p.CurrentAV
////                    })
////                    .Where(x => !double.IsNaN(x.t) && !double.IsNaN(x.v) &&
////                                !double.IsInfinity(x.t) && !double.IsInfinity(x.v))
////                    .ToArray();

////                if (pairs.Length < 2)
////                {
////                    Message = "No valid data to display";
////                    HasPendingChart = false;
////                    NotifyChanged();
////                    return;
////                }

////                PendingTimes = pairs.Select(x => x.t).ToArray();
////                PendingVoltages = pairs.Select(x => x.v).ToArray();
////                HasPendingChart = true;

////                NotifyChanged();
////            }
////            catch (Exception ex)
////            {
////                Message = $"Error: {ex.Message}";
////                HasPendingChart = false;
////                NotifyChanged();
////            }
////        }

////        public void Detect()
////        {
////            if (Model == null) return;

////            Anomalies = _service.DetectAnomalies(Model, Threshold);
////            NotifyChanged();
////        }

////        // View 画完图后调用，避免重复画
////        public void MarkChartDrawn()
////        {
////            HasPendingChart = false;
////        }
////    }
////}

//using Microsoft.AspNetCore.Components.Forms;
//using PipeUi.Interfaces;
//using PipeUi.Models;

//namespace PipeUi.State
//{
//    public class VoltagePlotState
//    {
//        private readonly IVoltageService _service;

//        public VoltagePlotState(IVoltageService service)
//        {
//            _service = service;
//        }

//        // View 订阅刷新
//        public event Action? OnChange;
//        private void NotifyChanged() => OnChange?.Invoke();

//        public VoltageLogModel? Model { get; private set; }
//        public VoltageStatistics? Stats { get; private set; }
//        public List<VoltageAnomaly>? Anomalies { get; private set; }

//        public string Message { get; private set; } = "";
//        public double Threshold { get; set; } = 10;

//        // 给 View 用的“待绘图数据”
//        public bool HasPendingChart { get; private set; } = false;
//        public double[] PendingTimes { get; private set; } = Array.Empty<double>();
//        public double[] PendingVoltages { get; private set; } = Array.Empty<double>();

//        // ✅ 图表可切换列（Andy 说的 switch columns）
//        public string SelectedSeries { get; private set; } = Series.Voltage;

//        public static class Series
//        {
//            public const string Voltage = "Voltage";
//            public const string CurrentX = "Current X";
//            public const string CurrentY = "Current Y";
//            public const string CurrentZ = "Current Z";
//            public const string CorrectedX = "Corrected X";
//            public const string CorrectedY = "Corrected Y";
//            public const string CorrectedZ = "Corrected Z";
//            public const string MovingAverageVoltage = "MovingAverage Voltage"; // 如果旧文件有
//        }

//        public IReadOnlyList<string> SeriesOptions { get; } = new List<string>
//        {
//            Series.Voltage,
//            Series.MovingAverageVoltage,
//            Series.CurrentX,
//            Series.CurrentY,
//            Series.CurrentZ,
//            Series.CorrectedX,
//            Series.CorrectedY,
//            Series.CorrectedZ
//        };

//        public async Task LoadFileAsync(InputFileChangeEventArgs e)
//        {
//            try
//            {
//                var file = e.File;
//                if (file is null)
//                {
//                    Message = "No file selected.";
//                    NotifyChanged();
//                    return;
//                }

//                using var stream = file.OpenReadStream(10 * 1024 * 1024);
//                using var ms = new MemoryStream();
//                await stream.CopyToAsync(ms);
//                ms.Position = 0;

//                Model = _service.LoadDataFromStream(ms);
//                if (Model != null && string.IsNullOrEmpty(Model.FileName))
//                    Model.FileName = file.Name;

//                if (Model == null || Model.DataPoints.Count == 0)
//                {
//                    Message = "Loaded 0 points";
//                    Stats = null;
//                    HasPendingChart = false;
//                    NotifyChanged();
//                    return;
//                }

//                Stats = Model.GetStatistics();
//                Message = $"Loaded {Model.DataPoints.Count} points";

//                // ✅ 加载完以后按当前选择的 series 准备绘图数据
//                PrepareChartData();

//                NotifyChanged();
//            }
//            catch (Exception ex)
//            {
//                Message = $"Error: {ex.Message}";
//                HasPendingChart = false;
//                NotifyChanged();
//            }
//        }

//        // ✅ 下拉切换时调用：更新 series + 重新准备绘图数据
//        public void ChangeSeries(string series)
//        {
//            SelectedSeries = series;
//            PrepareChartData();
//            NotifyChanged();
//        }

//        // ✅ anomaly detection：用 Voltage（如果要改成对某列检测也可以后面做）
//        public void Detect()
//        {
//            if (Model == null) return;
//            Anomalies = _service.DetectAnomalies(Model, Threshold);
//            NotifyChanged();
//        }

//        // View 画完图后调用，避免重复画
//        public void MarkChartDrawn()
//        {
//            HasPendingChart = false;
//        }

//        public string GetYAxisLabel()
//        {
//            return SelectedSeries switch
//            {
//                var s when s == Series.Voltage => "Voltage (V)",
//                var s when s == Series.MovingAverageVoltage => "Moving Avg Voltage (V)",
//                _ => SelectedSeries // 其它先直接显示名字
//            };
//        }


//        // -------------------------
//        // Internal helpers
//        // -------------------------

//        private void PrepareChartData()
//        {
//            if (Model == null || Model.DataPoints.Count == 0)
//            {
//                HasPendingChart = false;
//                return;
//            }

//            // 选择要画的值
//            Func<VoltageDataPoint, double> selector = SelectedSeries switch
//            {
//                var s when s == Series.Voltage => p => p.Voltage,                 // ✅ 新格式主电压
//                var s when s == Series.MovingAverageVoltage => p => p.MovingAverageAV,
//                var s when s == Series.CurrentX => p => p.CurrentX,
//                var s when s == Series.CurrentY => p => p.CurrentY,
//                var s when s == Series.CurrentZ => p => p.CurrentZ,
//                var s when s == Series.CorrectedX => p => p.CorrectedX,
//                var s when s == Series.CorrectedY => p => p.CorrectedY,
//                var s when s == Series.CorrectedZ => p => p.CorrectedZ,
//                _ => p => p.Voltage
//            };

//            // ✅ 成对过滤，保证 times 和 values 长度一致
//            var pairs = Model.DataPoints
//                .Select(p => new { t = p.Time, v = selector(p) })
//                .Where(x =>
//                    !double.IsNaN(x.t) && !double.IsInfinity(x.t) &&
//                    !double.IsNaN(x.v) && !double.IsInfinity(x.v))
//                .ToArray();

//            if (pairs.Length < 2)
//            {
//                Message = $"No valid data to display for '{SelectedSeries}'.";
//                HasPendingChart = false;
//                return;
//            }

//            PendingTimes = pairs.Select(x => x.t).ToArray();
//            PendingVoltages = pairs.Select(x => x.v).ToArray();
//            HasPendingChart = true;
//        }
//    }
//}
// ===== v3 开始 (被 v4 替代) =====
//using Microsoft.AspNetCore.Components.Forms;
//using PipeUi.Interfaces;
//using PipeUi.Models;
//
//namespace PipeUi.State
//{
//    public class VoltagePlotState
//    {
//        private readonly IVoltageService _service;
//
//        public VoltagePlotState(IVoltageService service)
//        {
//            _service = service;
//        }
//
//        public event Action? OnChange;
//        private void NotifyChanged() => OnChange?.Invoke();
//
//        public VoltageLogModel? Model { get; private set; }
//        public VoltageStatistics? Stats { get; private set; }
//        public List<VoltageAnomaly>? Anomalies { get; private set; }
//
//        public string Message { get; private set; } = "";
//        public double Threshold { get; set; } = 10;
//
//        public bool HasPendingChart { get; private set; } = false;
//        public double[] PendingTimes { get; private set; } = Array.Empty<double>();
//        public double[] PendingValues { get; private set; } = Array.Empty<double>();
//
//        public string ChartTitle { get; private set; } = "Voltage vs Time";
//        public string YAxisLabel { get; private set; } = "Voltage (V)";
//
//        public string SelectedSeries { get; private set; } = Series.Voltage;
//
//        public static class Series
//        {
//            public const string Voltage = "Voltage";
//            public const string MovingAverageVoltage = "MovingAverage Voltage";
//            public const string CurrentX = "Current X";
//            public const string CurrentY = "Current Y";
//            public const string CurrentZ = "Current Z";
//            public const string CorrectedX = "Corrected X";
//            public const string CorrectedY = "Corrected Y";
//            public const string CorrectedZ = "Corrected Z";
//        }
//
//        public IReadOnlyList<string> SeriesOptions { get; } = new List<string>
//        {
//            Series.Voltage,
//            Series.MovingAverageVoltage,
//            Series.CurrentX,
//            Series.CurrentY,
//            Series.CurrentZ,
//            Series.CorrectedX,
//            Series.CorrectedY,
//            Series.CorrectedZ
//        };
//
//        public async Task LoadFileAsync(InputFileChangeEventArgs e)
//        {
//            try
//            {
//                var file = e.File;
//                if (file is null) { Message = "No file selected."; NotifyChanged(); return; }
//
//                using var stream = file.OpenReadStream(10 * 1024 * 1024);
//                using var ms = new MemoryStream();
//                await stream.CopyToAsync(ms);
//                ms.Position = 0;
//
//                Model = _service.LoadDataFromStream(ms);
//                if (Model != null && string.IsNullOrEmpty(Model.FileName)) Model.FileName = file.Name;
//                if (Model == null || Model.DataPoints.Count == 0)
//                { Message = "Loaded 0 points"; Stats = null; HasPendingChart = false; NotifyChanged(); return; }
//
//                Stats = Model.GetStatistics();
//                Message = $"Loaded {Model.DataPoints.Count} points";
//                PrepareChartData();
//                NotifyChanged();
//            }
//            catch (Exception ex) { Message = $"Error: {ex.Message}"; HasPendingChart = false; NotifyChanged(); }
//        }
//
//        public void ChangeSeries(string series) { SelectedSeries = series; PrepareChartData(); NotifyChanged(); }
//        public void Detect() { if (Model == null) return; Anomalies = _service.DetectAnomalies(Model, Threshold); NotifyChanged(); }
//        public void MarkChartDrawn() { HasPendingChart = false; }
//
//        private void PrepareChartData()
//        {
//            if (Model == null || Model.DataPoints.Count == 0) { HasPendingChart = false; return; }
//            Func<VoltageDataPoint, double> selector = SelectedSeries switch
//            {
//                var s when s == Series.Voltage => p => p.Voltage,
//                var s when s == Series.MovingAverageVoltage => p => p.MovingAverageAV,
//                var s when s == Series.CurrentX => p => p.CurrentX,
//                var s when s == Series.CurrentY => p => p.CurrentY,
//                var s when s == Series.CurrentZ => p => p.CurrentZ,
//                var s when s == Series.CorrectedX => p => p.CorrectedX,
//                var s when s == Series.CorrectedY => p => p.CorrectedY,
//                var s when s == Series.CorrectedZ => p => p.CorrectedZ,
//                _ => p => p.Voltage
//            };
//            ChartTitle = $"{SelectedSeries} vs Time";
//            YAxisLabel = SelectedSeries == Series.Voltage || SelectedSeries == Series.MovingAverageVoltage ? "Voltage (V)" : SelectedSeries;
//            var pairs = Model.DataPoints
//                .Select(p => new { t = p.Time, v = selector(p) })
//                .Where(x => !double.IsNaN(x.t) && !double.IsInfinity(x.t) && !double.IsNaN(x.v) && !double.IsInfinity(x.v))
//                .ToArray();
//            if (pairs.Length < 2) { Message = $"No valid data to display for '{SelectedSeries}'."; HasPendingChart = false; return; }
//            if (Message.StartsWith("No valid data to display")) Message = "";
//            PendingTimes = pairs.Select(x => x.t).ToArray();
//            PendingValues = pairs.Select(x => x.v).ToArray();
//            HasPendingChart = true;
//        }
//    }
//}
// ===== v3 结束 =====

// ===== v4 被 v5 替代 =====

// // ===== v5 旧写法（压缩版）已注释，见上方 =====

// ===== v5 简化写法：逻辑拆开、每行一件事、LINQ 换成 foreach =====
using Microsoft.AspNetCore.Components.Forms;
using PipeUi.Interfaces;
using PipeUi.Models;

namespace PipeUi.State
{
    public class VoltagePlotState
    {
        private readonly IVoltageService _service;

        // 简化写法：构造函数用花括号，不用 => 表达式体
        public VoltagePlotState(IVoltageService service)
        {
            _service = service;
        }

        public event Action? OnChange;

        // 简化写法：通知方法用花括号
        private void NotifyChanged()
        {
            if (OnChange != null)
            {
                OnChange.Invoke();
            }
        }

        public VoltageLogModel? Model { get; private set; }
        public VoltageStatistics? Stats { get; private set; }
        public List<VoltageAnomaly>? Anomalies { get; private set; }
        public string Message { get; private set; } = "";
        public double Threshold { get; set; } = 10;

        // 绘图数据（统一用多系列，单系列 = 只有一条线）
        public bool HasPendingChart { get; private set; }
        public List<ChartSeriesData> PendingSeries { get; private set; } = new();
        public string ChartTitle { get; private set; } = "Voltage vs Time";
        public string YAxisLabel { get; private set; } = "Voltage (V)";

        // 系列选择
        public string SelectedSeries { get; private set; } = "Voltage";
        public bool MultiSeriesMode { get; set; }
        public HashSet<string> SelectedMultiSeries { get; set; } = new();
        public IReadOnlyList<string> SeriesOptions { get; } = new[]
        {
            "Voltage", "MovingAverage Voltage",
            "Current X", "Current Y", "Current Z",
            "Corrected X", "Corrected Y", "Corrected Z"
        };

        // 轴范围（每个变量单独声明，不用 _xMin, _xMax, ... 挤一行）
        public string XMinText { get; set; } = "";
        public string XMaxText { get; set; } = "";
        public string YMinText { get; set; } = "";
        public string YMaxText { get; set; } = "";
        private double? _xMin;
        private double? _xMax;
        private double? _yMin;
        private double? _yMax;

        // 采样分析
        public List<SamplingAnomaly>? SamplingAnomalies { get; private set; }
        public double[]? DeltaTimes { get; private set; }
        public double MedianDeltaTime { get; private set; }

        // --- 公开方法 ---

        public async Task LoadFileAsync(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file is null)
                {
                    Message = "No file selected.";
                    NotifyChanged();
                    return;
                }

                using var stream = file.OpenReadStream(10 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;

                Model = _service.LoadDataFromStream(ms);

                if (Model != null && string.IsNullOrEmpty(Model.FileName))
                {
                    Model.FileName = file.Name;
                }

                if (Model == null || Model.DataPoints.Count == 0)
                {
                    Message = "Loaded 0 points";
                    Stats = null;
                    HasPendingChart = false;
                    NotifyChanged();
                    return;
                }

                Stats = Model.GetStatistics();
                Message = $"Loaded {Model.DataPoints.Count} points";

                // 重置轴范围（每个赋值单独一行）
                XMinText = "";
                XMaxText = "";
                YMinText = "";
                YMaxText = "";
                _xMin = null;
                _xMax = null;
                _yMin = null;
                _yMax = null;

                // 重置采样分析
                SamplingAnomalies = null;
                DeltaTimes = null;

                PrepareChartData();
                NotifyChanged();
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                HasPendingChart = false;
                NotifyChanged();
            }
        }

        // // 旧写法：一行塞多个语句
        // public void ChangeSeries(string s) { SelectedSeries = s; PrepareChartData(); NotifyChanged(); }

        // 简化写法：每个方法展开写
        public void ChangeSeries(string series)
        {
            SelectedSeries = series;
            PrepareChartData();
            NotifyChanged();
        }

        public void MarkChartDrawn()
        {
            HasPendingChart = false;
        }

        public void Detect()
        {
            if (Model == null)
            {
                return;
            }
            Anomalies = _service.DetectAnomalies(Model, Threshold);
            NotifyChanged();
        }

        public void ApplyRange()
        {
            // // 旧写法：三元表达式 + out var
            // _xMin = double.TryParse(XMinText, out var a) ? a : null;

            // 简化写法：先 TryParse，再判断
            if (double.TryParse(XMinText, out double xMinResult))
            {
                _xMin = xMinResult;
            }
            else
            {
                _xMin = null;
            }

            if (double.TryParse(XMaxText, out double xMaxResult))
            {
                _xMax = xMaxResult;
            }
            else
            {
                _xMax = null;
            }

            if (double.TryParse(YMinText, out double yMinResult))
            {
                _yMin = yMinResult;
            }
            else
            {
                _yMin = null;
            }

            if (double.TryParse(YMaxText, out double yMaxResult))
            {
                _yMax = yMaxResult;
            }
            else
            {
                _yMax = null;
            }

            PrepareChartData();
            NotifyChanged();
        }

        public void ResetRange()
        {
            XMinText = "";
            XMaxText = "";
            YMinText = "";
            YMaxText = "";
            _xMin = null;
            _xMax = null;
            _yMin = null;
            _yMax = null;
            PrepareChartData();
            NotifyChanged();
        }

        public void ToggleMultiSeries(string series, bool on)
        {
            if (on)
            {
                SelectedMultiSeries.Add(series);
            }
            else
            {
                SelectedMultiSeries.Remove(series);
            }
            PrepareChartData();
            NotifyChanged();
        }

        public void SetMultiSeriesMode(bool on)
        {
            MultiSeriesMode = on;
            if (on && SelectedMultiSeries.Count == 0)
            {
                SelectedMultiSeries.Add("Corrected X");
                SelectedMultiSeries.Add("Corrected Y");
                SelectedMultiSeries.Add("Corrected Z");
            }
            PrepareChartData();
            NotifyChanged();
        }

        public void AnalyzeSampling()
        {
            if (Model == null || Model.DataPoints.Count < 2)
            {
                return;
            }

            // // 旧写法：LINQ 取时间
            // var times = Model.DataPoints.Select(p => p.Time).Where(t => double.IsFinite(t)).ToArray();

            // 简化写法：用 foreach 收集有效时间
            var timeList = new List<double>();
            foreach (var point in Model.DataPoints)
            {
                if (double.IsFinite(point.Time))
                {
                    timeList.Add(point.Time);
                }
            }
            double[] times = timeList.ToArray();

            if (times.Length < 2)
            {
                Message = "Not enough valid time values.";
                NotifyChanged();
                return;
            }

            // 计算相邻时间差
            var deltas = new double[times.Length - 1];
            for (int i = 0; i < deltas.Length; i++)
            {
                deltas[i] = times[i + 1] - times[i];
            }
            DeltaTimes = deltas;

            // 计算中位数
            double[] sorted = deltas.OrderBy(d => d).ToArray();
            if (sorted.Length % 2 == 0)
            {
                // 偶数个：取中间两个的平均
                MedianDeltaTime = (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0;
            }
            else
            {
                // 奇数个：取中间那个
                MedianDeltaTime = sorted[sorted.Length / 2];
            }

            // // 旧写法：LINQ 链式
            // SamplingAnomalies = deltas
            //     .Select((d, i) => new SamplingAnomaly { ... })
            //     .Where(a => ...)
            //     .ToList();

            // 简化写法：用 foreach 找异常点
            SamplingAnomalies = new List<SamplingAnomaly>();
            for (int i = 0; i < deltas.Length; i++)
            {
                double dt = deltas[i];
                // 如果时间差超过中位数的2倍，或不到0.5倍，就算异常
                bool isTooLarge = dt > MedianDeltaTime * 2;
                bool isTooSmall = dt < MedianDeltaTime * 0.5;

                if (MedianDeltaTime > 0 && (isTooLarge || isTooSmall))
                {
                    var anomaly = new SamplingAnomaly();
                    anomaly.Index = i;
                    anomaly.Time = times[i];
                    anomaly.DeltaTime = dt;
                    anomaly.ExpectedDelta = MedianDeltaTime;
                    SamplingAnomalies.Add(anomaly);
                }
            }

            Message = $"Sampling: median Δt={MedianDeltaTime:G4}s, {SamplingAnomalies.Count} anomalies.";
            NotifyChanged();
        }

        // --- 核心：统一准备绘图数据 ---

        private void PrepareChartData()
        {
            if (Model == null || Model.DataPoints.Count == 0)
            {
                HasPendingChart = false;
                return;
            }

            // 判断是否所有 Time 都无效（如果是，用索引代替时间轴）
            bool useIndex = true;
            foreach (var point in Model.DataPoints)
            {
                if (double.IsFinite(point.Time))
                {
                    useIndex = false;
                    break;
                }
            }

            // 要画哪些系列
            List<string> names;
            if (MultiSeriesMode && SelectedMultiSeries.Count > 0)
            {
                names = SelectedMultiSeries.ToList();
            }
            else
            {
                names = new List<string> { SelectedSeries };
            }

            // 设置图表标题
            if (names.Count == 1)
            {
                ChartTitle = $"{names[0]} vs Time";
            }
            else
            {
                ChartTitle = "Multi-Series vs Time";
            }

            // 设置 Y 轴标签
            if (names.Count == 1 && (names[0] == "Voltage" || names[0] == "MovingAverage Voltage"))
            {
                YAxisLabel = "Voltage (V)";
            }
            else
            {
                YAxisLabel = "Value";
            }

            // // 旧写法：LINQ 链式 + Func<> 选择器
            // var sel = GetSelector(name);
            // var filtered = Model.DataPoints
            //     .Select((p, i) => (t: useIdx ? (double)i : p.Time, v: sel(p)))
            //     .Where(x => double.IsFinite(x.t) && double.IsFinite(x.v))
            //     .Where(x => (!_xMin.HasValue || x.t >= _xMin) && (!_xMax.HasValue || x.t <= _xMax))
            //     .Where(x => (!_yMin.HasValue || x.v >= _yMin) && (!_yMax.HasValue || x.v <= _yMax))
            //     .ToArray();

            // 简化写法：用 for 循环手动过滤
            PendingSeries.Clear();

            foreach (string name in names)
            {
                var timeList = new List<double>();
                var valueList = new List<double>();

                for (int i = 0; i < Model.DataPoints.Count; i++)
                {
                    var point = Model.DataPoints[i];

                    // 时间值：用索引或实际时间
                    double t = useIndex ? (double)i : point.Time;
                    // 数值：根据系列名取对应字段
                    double v = GetValueBySeriesName(point, name);

                    // 跳过无效数据
                    if (!double.IsFinite(t) || !double.IsFinite(v))
                    {
                        continue;
                    }

                    // 应用轴范围过滤
                    if (_xMin.HasValue && t < _xMin)
                    {
                        continue;
                    }
                    if (_xMax.HasValue && t > _xMax)
                    {
                        continue;
                    }
                    if (_yMin.HasValue && v < _yMin)
                    {
                        continue;
                    }
                    if (_yMax.HasValue && v > _yMax)
                    {
                        continue;
                    }

                    timeList.Add(t);
                    valueList.Add(v);
                }

                if (timeList.Count >= 2)
                {
                    var seriesData = new ChartSeriesData();
                    seriesData.Name = name;
                    seriesData.Times = timeList.ToArray();
                    seriesData.Values = valueList.ToArray();
                    PendingSeries.Add(seriesData);
                }
            }

            HasPendingChart = PendingSeries.Count > 0;

            if (!HasPendingChart)
            {
                Message = "No valid data for selected series.";
            }
            else if (Message.StartsWith("No valid data"))
            {
                Message = "";
            }
        }

        // // 旧写法：GetSelector 返回 Func + switch 表达式，多个 case 压一行
        // private static Func<VoltageDataPoint, double> GetSelector(string name) => name switch
        // {
        //     "Voltage" => p => p.Voltage,
        //     "Current X" => p => p.CurrentX, "Current Y" => p => p.CurrentY, ...
        //     _ => p => p.Voltage
        // };

        // 简化写法：直接返回 double，用 if/else 代替 switch 表达式
        private static double GetValueBySeriesName(VoltageDataPoint point, string seriesName)
        {
            if (seriesName == "Voltage")
            {
                return point.Voltage;
            }
            else if (seriesName == "MovingAverage Voltage")
            {
                return point.MovingAverageAV;
            }
            else if (seriesName == "Current X")
            {
                return point.CurrentX;
            }
            else if (seriesName == "Current Y")
            {
                return point.CurrentY;
            }
            else if (seriesName == "Current Z")
            {
                return point.CurrentZ;
            }
            else if (seriesName == "Corrected X")
            {
                return point.CorrectedX;
            }
            else if (seriesName == "Corrected Y")
            {
                return point.CorrectedY;
            }
            else if (seriesName == "Corrected Z")
            {
                return point.CorrectedZ;
            }
            else
            {
                return point.Voltage;
            }
        }
    }

    public class ChartSeriesData
    {
        public string Name { get; set; } = "";
        public double[] Times { get; set; } = Array.Empty<double>();
        public double[] Values { get; set; } = Array.Empty<double>();
    }
}

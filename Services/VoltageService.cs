//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Globalization;
//using System.Linq;
//using PipeUi.Models;
//using PipeUi.Interfaces;

//namespace PipeUi.Services
//{
//    public class VoltageService : IVoltageService
//    {
//        public VoltageLogModel LoadDataFromStream(Stream stream)
//        {
//            var model = new VoltageLogModel();
//            using var reader = new StreamReader(stream);
//            var lines = new List<string>();

//            while (!reader.EndOfStream)
//            {
//                var line = reader.ReadLine();
//                if (!string.IsNullOrWhiteSpace(line))
//                    lines.Add(line);
//            }

//            if (lines.Count < 2) return model;

//            // 检测格式：新格式（BeginHeader）还是旧格式
//            if (lines[0].Trim().Equals("BeginHeader", StringComparison.OrdinalIgnoreCase))
//            {
//                return ParseNewFormat(lines, model);
//            }
//            else
//            {
//                return ParseOldFormat(lines, model);
//            }
//        }
//        private static string[] SplitCsvRow(string line)
//        {
//            // 你的文件没有引号/转义需求的话，用这个足够稳
//            // 关键：保留空项，这样列数不会乱；再由后续 Trim + 过滤决定用哪些
//            return line.Split(',', StringSplitOptions.None);
//        }

//        private VoltageLogModel ParseNewFormat(List<string> lines, VoltageLogModel model)
//        {
//            int headerEndIndex = -1;

//            // 解析 Header 部分
//            for (int i = 0; i < lines.Count; i++)
//            {
//                var line = lines[i].Trim();

//                if (line.Equals("BeginHeader", StringComparison.OrdinalIgnoreCase))
//                    continue;

//                if (line.Equals("EndHeader", StringComparison.OrdinalIgnoreCase))
//                {
//                    headerEndIndex = i;
//                    break;
//                }

//                // 解析参数行（格式：ParameterName, Value）
//                var parts = SplitCsvRow(line);
//                if (parts.Length >= 2)
//                {
//                    var key = parts[0].Trim();
//                    var value = parts[1].Trim();

//                    model.HeaderParameters[key] = value;

//                    // 映射到已知字段
//                    switch (key.ToLower())
//                    {
//                        case "filename":
//                            model.FileName = value;
//                            break;
//                        case "targetvoltage":
//                            model.TargetVoltage = ParseDouble(value, 120.0);
//                            break;
//                        case "gainz":
//                            model.GainZ = ParseDouble(value, 0.01);
//                            break;
//                        case "thresholdz":
//                            model.ThresholdZ = ParseDouble(value, 0.1);
//                            break;
//                        case "gainy":
//                            model.GainY = ParseDouble(value, 0.1);
//                            break;
//                        case "thresholdy":
//                            model.ThresholdY = ParseDouble(value, 0.0);
//                            break;
//                        case "transferheight":
//                            model.TransferHeight = ParseDouble(value, 0.0);
//                            break;
//                        case "pierceheight":
//                            model.PierceHeight = ParseDouble(value, 0.0);
//                            break;
//                        case "cutheight":
//                            model.CutHeight = ParseDouble(value, 0.0);
//                            break;
//                        case "piercetime":
//                            model.PierceTime = ParseDouble(value, 0.0);
//                            break;
//                        case "filtertype":
//                            model.FilterType = value;
//                            break;
//                        case "movingaverage":
//                            model.MovingAverage = (int)ParseDouble(value, 0.0);
//                            break;
//                    }
//                }
//            }

//            if (headerEndIndex == -1 || headerEndIndex + 1 >= lines.Count)
//                return model;

//            // 解析列名
//            var columnLine = lines[headerEndIndex + 1];
//            model.ColumnNames = SplitCsvRow(columnLine).Select(c => c.Trim()).ToList();

//            // 解析数据行
//            for (int i = headerEndIndex + 2; i < lines.Count; i++)
//            {
//                var dataPoint = ParseDataRow(lines[i], model.ColumnNames);
//                if (dataPoint != null)
//                    model.DataPoints.Add(dataPoint);
//            }

//            return model;
//        }

//        private VoltageLogModel ParseOldFormat(List<string> lines, VoltageLogModel model)
//        {
//            int dataStartIndex = 0;

//            // 尝试找到列名行（包含 "Time" 或 "FilteredVoltage" 的行）
//            for (int i = 0; i < Math.Min(15, lines.Count); i++)
//            {
//                var line = lines[i].ToLower();
//                if (line.Contains("time") && (line.Contains("voltage") || line.Contains("y") || line.Contains("z")))
//                {
//                    // 找到列名行
//                    model.ColumnNames = lines[i].Split(',').Select(c => c.Trim()).ToList();
//                    dataStartIndex = i + 1;

//                    // 解析前面的配置行
//                    if (i >= 11)
//                    {
//                        model.TargetVoltage = ParseConfigValue(lines[0], 120.0);
//                        model.GainZ = ParseConfigValue(lines[1], 0.01);
//                        model.ThresholdZ = ParseConfigValue(lines[2], 0.1);
//                        model.GainY = ParseConfigValue(lines[3], 0.1);
//                        model.ThresholdY = ParseConfigValue(lines[4], 0.0);
//                        model.TransferHeight = ParseConfigValue(lines[5], 0.0);
//                        model.PierceHeight = ParseConfigValue(lines[6], 0.0);
//                        model.CutHeight = ParseConfigValue(lines[7], 0.0);
//                        model.PierceTime = ParseConfigValue(lines[8], 0.0);

//                        var filterParts = lines[9].Split(',');
//                        model.FilterType = filterParts.Length > 1 ? filterParts[1].Trim() : "";

//                        model.MovingAverage = (int)ParseConfigValue(lines[10], 0.0);
//                    }
//                    break;
//                }
//            }

//            // 如果没找到列名行，使用默认列名
//            if (model.ColumnNames.Count == 0)
//            {
//                model.ColumnNames = new List<string> { "Time", "FilteredVoltage", "Y", "Z" };
//                dataStartIndex = 13; // 假设从第13行开始是数据
//            }

//            // 解析数据行
//            for (int i = dataStartIndex; i < lines.Count; i++)
//            {
//                var dataPoint = ParseDataRow(lines[i], model.ColumnNames);
//                if (dataPoint != null)
//                    model.DataPoints.Add(dataPoint);
//            }

//            return model;
//        }

//        //private VoltageDataPoint ParseDataRow(string line, List<string> columnNames)
//        //{
//        //    var parts = line.Split(',');
//        //    if (parts.Length < columnNames.Count)
//        //        return null;

//        //    var dataPoint = new VoltageDataPoint();

//        //    try
//        //    {
//        //        for (int i = 0; i < columnNames.Count && i < parts.Length; i++)
//        //        {
//        //            var columnName = columnNames[i].ToLower().Replace(" ", "");
//        //            var value = ParseDouble(parts[i], double.NaN);

//                    //            switch (columnName)
//                    //            {
//                    //                case "time":
//                    //                case "sampletime":
//                    //                    dataPoint.Time = value;
//                    //                    break;
//                    //                case "x":
//                    //                    dataPoint.X = value;
//                    //                    break;
//                    //                case "y":
//                    //                    dataPoint.Y = value;
//                    //                    break;
//                    //                case "z":
//                    //                    dataPoint.Z = value;
//                    //                    break;
//                    //                case "corrz":
//                    //                    dataPoint.CorrZ = value;
//                    //                    break;
//                    //                case "coory":
//                    //                case "coordy":
//                    //                    dataPoint.CoorY = value;
//                    //                    break;
//                    //                case "currentav":
//                    //                case "filteredvoltage":
//                    //                    dataPoint.CurrentAV = value;
//                    //                    break;
//                    //                case "movingaverageav":
//                    //                    dataPoint.MovingAverageAV = value;
//                    //                    break;
//                    //            }
//                    //        }

//                    //        return dataPoint;
//                    //    }
//                    //    catch
//                    //    {
//                    //        return null;
//                    //    }
//                    //}
//        private VoltageDataPoint? ParseDataRow(string line, List<string> columnNames)
//        {
//            var parts = SplitCsvRow(line);

//            // 跳过像 ",,,,," 这种空行
//            if (parts.All(p => string.IsNullOrWhiteSpace(p)))
//                return null;

//            var dataPoint = new VoltageDataPoint();

//            try
//            {
//                // 只遍历“有效列名”对应的列；columnNames 你已经做了 Trim + 过滤空
//                int n = Math.Min(columnNames.Count, parts.Length);

//                for (int i = 0; i < n; i++)
//                {
//                    var columnName = columnNames[i].ToLower().Replace(" ", "").Trim();
//                    var value = ParseDouble(parts[i], double.NaN);

//                    switch (columnName)
//                    {
//                        case "time":
//                        case "sampletime":
//                            dataPoint.Time = value;
//                            break;

//                        case "x":
//                            dataPoint.X = value;
//                            break;

//                        case "y":
//                        case "yposition":
//                            dataPoint.Y = value;
//                            break;

//                        case "z":
//                        case "zposition":
//                            dataPoint.Z = value;
//                            break;

//                        case "corrz":
//                            dataPoint.CorrZ = value;
//                            break;

//                        case "coory":
//                        case "coordy":
//                        case "ycommanded":   // 新格式
//                            dataPoint.CoorY = value;
//                            break;

//                        case "currentav":
//                        case "filteredvoltage":
//                        case "voltage":      // 新格式 Voltage 列
//                            dataPoint.CurrentAV = value;
//                            break;

//                        case "movingaverageav":
//                            dataPoint.MovingAverageAV = value;
//                            break;

//                            // "zcommanded" 你模型里没有字段；暂时忽略即可
//                    }
//                }

//                // 至少要有 time 和 voltage（或其中之一）才算有效
//                if (double.IsNaN(dataPoint.Time) && double.IsNaN(dataPoint.CurrentAV))
//                    return null;

//                return dataPoint;
//            }
//            catch
//            {
//                return null;
//            }
//        }


//        public List<VoltageAnomaly> DetectAnomalies(VoltageLogModel model, double threshold)
//        {
//            var anomalies = new List<VoltageAnomaly>();
//            foreach (var point in model.DataPoints)
//            {
//                if (double.IsNaN(point.CurrentAV))
//                    continue;

//                double deviation = Math.Abs(point.CurrentAV - model.TargetVoltage);
//                if (deviation > threshold)
//                {
//                    anomalies.Add(new VoltageAnomaly
//                    {
//                        Time = point.Time,
//                        Voltage = point.CurrentAV,
//                        Deviation = deviation
//                    });
//                }
//            }
//            return anomalies;
//        }

//        private double ParseConfigValue(string line, double defaultValue)
//        {
//            var parts = line.Split(',');
//            if (parts.Length < 2) return defaultValue;
//            return ParseDouble(parts[1], defaultValue);
//        }

//        private double ParseDouble(string value, double defaultValue)
//        {
//            if (string.IsNullOrWhiteSpace(value))
//                return defaultValue;

//            value = value.Trim();

//            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
//                return result;

//            return defaultValue;
//        }
//    }
//}


//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using PipeUi.Interfaces;
//using PipeUi.Models;

//namespace PipeUi.Services
//{
//    public class VoltageService : IVoltageService
//    {
//        public VoltageLogModel LoadDataFromStream(Stream stream)
//        {
//            var model = new VoltageLogModel();
//            using var reader = new StreamReader(stream);
//            var lines = new List<string>();

//            while (!reader.EndOfStream)
//            {
//                var line = reader.ReadLine();
//                // ✅ 这里不要过滤空行：新格式 EndHeader 后可能会有空行
//                lines.Add(line ?? "");
//            }

//            // 找到第一个非空行判断格式
//            var firstNonEmpty = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
//            if (string.IsNullOrWhiteSpace(firstNonEmpty))
//                return model;

//            if (firstNonEmpty.Trim().Equals("BeginHeader", StringComparison.OrdinalIgnoreCase))
//                return ParseNewFormat(lines, model);

//            return ParseOldFormat(lines, model);
//        }

//        private static string[] SplitCsvRow(string line)
//        {
//            // ✅ 保留空项（很多 CSV 会有 ,,,, 这种）
//            return (line ?? "").Split(',', StringSplitOptions.None);
//        }

//        private VoltageLogModel ParseNewFormat(List<string> lines, VoltageLogModel model)
//        {
//            int headerStart = -1;
//            int headerEnd = -1;

//            // 找 BeginHeader / EndHeader
//            for (int i = 0; i < lines.Count; i++)
//            {
//                var t = (lines[i] ?? "").Trim();
//                if (headerStart < 0 && t.Equals("BeginHeader", StringComparison.OrdinalIgnoreCase))
//                    headerStart = i;

//                if (t.Equals("EndHeader", StringComparison.OrdinalIgnoreCase))
//                {
//                    headerEnd = i;
//                    break;
//                }
//            }

//            if (headerStart < 0 || headerEnd < 0 || headerEnd <= headerStart)
//                return model;

//            // 解析 Header：Key,Value
//            for (int i = headerStart + 1; i < headerEnd; i++)
//            {
//                var raw = lines[i] ?? "";
//                if (string.IsNullOrWhiteSpace(raw)) continue;

//                var parts = SplitCsvRow(raw);
//                if (parts.Length < 2) continue;

//                var key = (parts[0] ?? "").Trim();
//                var value = (parts[1] ?? "").Trim();
//                if (string.IsNullOrWhiteSpace(key)) continue;

//                model.HeaderParameters[key] = value;

//                switch (key.ToLowerInvariant().Replace(" ", ""))
//                {
//                    case "filename":
//                        model.FileName = value;
//                        break;

//                    case "trackingenabled":
//                        model.TrackingEnabled = ParseBoolNullable(value);
//                        break;

//                    case "targetvoltage":
//                        model.TargetVoltage = ParseDouble(value, model.TargetVoltage);
//                        break;

//                    case "gainz":
//                        model.GainZ = ParseDouble(value, model.GainZ);
//                        break;

//                    case "thresholdz":
//                        model.ThresholdZ = ParseDouble(value, model.ThresholdZ);
//                        break;

//                    case "gainy":
//                        model.GainY = ParseDouble(value, model.GainY);
//                        break;

//                    case "thresholdy":
//                        model.ThresholdY = ParseDouble(value, model.ThresholdY);
//                        break;

//                    case "transferheight":
//                        model.TransferHeight = ParseDouble(value, model.TransferHeight);
//                        break;

//                    case "pierceheight":
//                        model.PierceHeight = ParseDouble(value, model.PierceHeight);
//                        break;

//                    case "cutheight":
//                        model.CutHeight = ParseDouble(value, model.CutHeight);
//                        break;

//                    case "piercetime":
//                        model.PierceTime = ParseDouble(value, model.PierceTime);
//                        break;

//                    case "filtertype":
//                        model.FilterType = value;
//                        break;

//                    case "movingaverage":
//                        model.MovingAverage = (int)ParseDouble(value, model.MovingAverage);
//                        break;
//                }
//            }

//            // ✅ EndHeader 后：找到第一个非空行作为 column header
//            int colIndex = headerEnd + 1;
//            while (colIndex < lines.Count && string.IsNullOrWhiteSpace(lines[colIndex]))
//                colIndex++;

//            if (colIndex >= lines.Count)
//                return model;

//            var columnLine = lines[colIndex] ?? "";
//            model.ColumnNames = SplitCsvRow(columnLine)
//                .Select(c => (c ?? "").Trim())
//                // ✅ 列名允许空，但我们保持长度对齐；解析时会自动忽略空列名
//                .ToList();

//            // 数据行从 colIndex+1 开始
//            for (int i = colIndex + 1; i < lines.Count; i++)
//            {
//                var raw = lines[i] ?? "";
//                if (string.IsNullOrWhiteSpace(raw)) continue;

//                var dp = ParseDataRow(raw, model.ColumnNames);
//                if (dp != null)
//                    model.DataPoints.Add(dp);
//            }

//            return model;
//        }

//        private VoltageLogModel ParseOldFormat(List<string> lines, VoltageLogModel model)
//        {
//            int dataStartIndex = 0;

//            // 尝试找到列名行（包含 time 且包含 voltage/y/z 等）
//            for (int i = 0; i < Math.Min(20, lines.Count); i++)
//            {
//                var line = (lines[i] ?? "").ToLowerInvariant();
//                if (line.Contains("time") && (line.Contains("voltage") || line.Contains("y") || line.Contains("z")))
//                {
//                    model.ColumnNames = SplitCsvRow(lines[i] ?? "").Select(c => (c ?? "").Trim()).ToList();
//                    dataStartIndex = i + 1;

//                    // 旧格式：前几行可能是 config
//                    // 你原逻辑保留（如果不符合就默认）
//                    if (i >= 11)
//                    {
//                        model.TargetVoltage = ParseConfigValue(lines, 0, model.TargetVoltage);
//                        model.GainZ = ParseConfigValue(lines, 1, model.GainZ);
//                        model.ThresholdZ = ParseConfigValue(lines, 2, model.ThresholdZ);
//                        model.GainY = ParseConfigValue(lines, 3, model.GainY);
//                        model.ThresholdY = ParseConfigValue(lines, 4, model.ThresholdY);
//                        model.TransferHeight = ParseConfigValue(lines, 5, model.TransferHeight);
//                        model.PierceHeight = ParseConfigValue(lines, 6, model.PierceHeight);
//                        model.CutHeight = ParseConfigValue(lines, 7, model.CutHeight);
//                        model.PierceTime = ParseConfigValue(lines, 8, model.PierceTime);

//                        var filterParts = SplitCsvRow(lines[9] ?? "");
//                        model.FilterType = filterParts.Length > 1 ? (filterParts[1] ?? "").Trim() : "";

//                        model.MovingAverage = (int)ParseConfigValue(lines, 10, model.MovingAverage);
//                    }
//                    break;
//                }
//            }

//            // 没找到列名行则兜底
//            if (model.ColumnNames.Count == 0)
//            {
//                model.ColumnNames = new List<string> { "Time", "FilteredVoltage", "Y", "Z" };
//                dataStartIndex = 0;
//            }

//            for (int i = dataStartIndex; i < lines.Count; i++)
//            {
//                var raw = lines[i] ?? "";
//                if (string.IsNullOrWhiteSpace(raw)) continue;

//                var dp = ParseDataRow(raw, model.ColumnNames);
//                if (dp != null)
//                    model.DataPoints.Add(dp);
//            }

//            return model;
//        }

//        private VoltageDataPoint? ParseDataRow(string line, List<string> columnNames)
//        {
//            var parts = SplitCsvRow(line);

//            // 跳过 ",,,,," 这种空行
//            if (parts.All(p => string.IsNullOrWhiteSpace(p)))
//                return null;

//            var dp = new VoltageDataPoint();

//            int n = Math.Min(columnNames.Count, parts.Length);

//            for (int i = 0; i < n; i++)
//            {
//                var rawCol = columnNames[i] ?? "";
//                var col = rawCol
//                    .ToLowerInvariant()
//                    .Replace(" ", "")
//                    .Trim();

//                // 空列名直接跳过（有些 CSV 会在 header 用 ,,,, 填充）
//                if (string.IsNullOrWhiteSpace(col))
//                    continue;

//                var val = ParseDouble(parts[i] ?? "", double.NaN);

//                switch (col)
//                {
//                    // Time
//                    case "time":
//                    case "sampletime":
//                        dp.Time = val;
//                        break;

//                    // Voltage signal (new: Voltage, old: FilteredVoltage/CurrentAV)
//                    case "voltage":
//                    case "filteredvoltage":
//                    case "currentav":
//                        dp.Voltage = val;
//                        break;

//                    // ===== New format: Current X/Y/Z =====
//                    case "currentx":
//                        dp.CurrentX = val;
//                        break;
//                    case "currenty":
//                        dp.CurrentY = val;
//                        break;
//                    case "currentz":
//                        dp.CurrentZ = val;
//                        break;

//                    // ===== New format: Corrected X/Y/Z =====
//                    case "correctedx":
//                        dp.CorrectedX = val;
//                        break;
//                    case "correctedy":
//                        dp.CorrectedY = val;
//                        break;
//                    case "correctedz":
//                        dp.CorrectedZ = val;
//                        break;

//                    // ===== Older/newer variants seen before =====
//                    case "x":
//                        dp.CurrentX = val;
//                        break;

//                    case "y":
//                    case "yposition":
//                        dp.YPosition = val;
//                        // 你之前也把 YPosition 当 Y 用，这里保留别名
//                        dp.CurrentY = double.IsNaN(dp.CurrentY) ? val : dp.CurrentY;
//                        break;

//                    case "z":
//                    case "zposition":
//                        dp.ZPosition = val;
//                        dp.CurrentZ = double.IsNaN(dp.CurrentZ) ? val : dp.CurrentZ;
//                        break;

//                    case "ycommanded":
//                    case "coory":
//                    case "coordy":
//                        dp.YCommanded = val;
//                        // 你旧字段 CoorY 指 CorrectedY，这里同步
//                        dp.CorrectedY = double.IsNaN(dp.CorrectedY) ? val : dp.CorrectedY;
//                        break;

//                    case "zcommanded":
//                        dp.ZCommanded = val;
//                        // 有些旧文件把“命令值”当 correctedz，这里可选同步（不强制）
//                        if (double.IsNaN(dp.CorrectedZ)) dp.CorrectedZ = val;
//                        break;

//                    case "corrz":
//                        dp.CorrectedZ = val;
//                        break;

//                    case "movingaverageav":
//                        dp.MovingAverageAV = val;
//                        break;
//                }
//            }

//            // 至少要有 Time + Voltage 才算有效点（你新格式就是这样）
//            if (double.IsNaN(dp.Time) || double.IsNaN(dp.Voltage))
//                return null;

//            return dp;
//        }

//        public List<VoltageAnomaly> DetectAnomalies(VoltageLogModel model, double threshold)
//        {
//            var anomalies = new List<VoltageAnomaly>();

//            foreach (var point in model.DataPoints)
//            {
//                if (double.IsNaN(point.Voltage))
//                    continue;

//                double deviation = Math.Abs(point.Voltage - model.TargetVoltage);

//                if (deviation > threshold)
//                {
//                    anomalies.Add(new VoltageAnomaly
//                    {
//                        Time = point.Time,
//                        Voltage = point.Voltage,
//                        Deviation = deviation
//                    });
//                }
//            }

//            return anomalies;
//        }

//        // ---------- helpers ----------

//        private static double ParseConfigValue(List<string> lines, int idx, double defaultValue)
//        {
//            if (idx < 0 || idx >= lines.Count) return defaultValue;

//            var parts = SplitCsvRow(lines[idx] ?? "");
//            if (parts.Length < 2) return defaultValue;

//            return ParseDouble(parts[1] ?? "", defaultValue);
//        }

//        private static double ParseDouble(string value, double defaultValue)
//        {
//            if (string.IsNullOrWhiteSpace(value))
//                return defaultValue;

//            value = value.Trim();

//            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
//                return result;

//            return defaultValue;
//        }

//        private static bool? ParseBoolNullable(string value)
//        {
//            if (string.IsNullOrWhiteSpace(value))
//                return null;

//            value = value.Trim();

//            if (bool.TryParse(value, out var b))
//                return b;

//            if (value == "1") return true;
//            if (value == "0") return false;

//            return null;
//        }
//    }
//}
// ===== 旧版（支持多格式）被 v5 替代 =====

// ===== v5: 只支持 BeginHeader 格式 =====

// // 旧写法（压缩版）
// using System.Globalization;
// using PipeUi.Interfaces;
// using PipeUi.Models;
// namespace PipeUi.Services
// {
//     public class VoltageService : IVoltageService
//     {
//         public VoltageLogModel LoadDataFromStream(Stream stream)
//         {
//             var model = new VoltageLogModel();
//             using var reader = new StreamReader(stream);
//             var lines = new List<string>();
//             while (!reader.EndOfStream) lines.Add(reader.ReadLine() ?? "");
//             if (lines.Count < 3) return model;
//             int hStart = lines.FindIndex(l => l.Trim().Equals("BeginHeader", StringComparison.OrdinalIgnoreCase));
//             int hEnd = lines.FindIndex(l => l.Trim().Equals("EndHeader", StringComparison.OrdinalIgnoreCase));
//             if (hStart < 0 || hEnd <= hStart) return model;
//             for (int i = hStart + 1; i < hEnd; i++)
//             {
//                 var parts = lines[i].Split(',');
//                 if (parts.Length < 2) continue;
//                 var key = parts[0].Trim();
//                 var val = parts[1].Trim();
//                 if (key == "") continue;
//                 model.HeaderParameters[key] = val;
//                 switch (key.ToLowerInvariant())
//                 {
//                     case "targetvoltage": model.TargetVoltage = P(val, model.TargetVoltage); break;
//                     case "gainz": model.GainZ = P(val, model.GainZ); break;
//                     case "gainy": model.GainY = P(val, model.GainY); break;
//                     case "thresholdz": model.ThresholdZ = P(val, model.ThresholdZ); break;
//                     case "thresholdy": model.ThresholdY = P(val, model.ThresholdY); break;
//                     case "trackingenabled": model.TrackingEnabled = val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1"; break;
//                 }
//             }
//             int col = hEnd + 1;
//             while (col < lines.Count && string.IsNullOrWhiteSpace(lines[col])) col++;
//             if (col >= lines.Count) return model;
//             model.ColumnNames = lines[col].Split(',').Select(c => c.Trim()).ToList();
//             var colMap = new Dictionary<string, int>();
//             for (int i = 0; i < model.ColumnNames.Count; i++)
//                 colMap[model.ColumnNames[i].ToLowerInvariant().Replace(" ", "")] = i;
//             for (int i = col + 1; i < lines.Count; i++)
//             {
//                 if (string.IsNullOrWhiteSpace(lines[i])) continue;
//                 var parts = lines[i].Split(',');
//                 var dp = new VoltageDataPoint();
//                 double V(string k) => colMap.TryGetValue(k, out var idx) && idx < parts.Length ? P(parts[idx], double.NaN) : double.NaN;
//                 dp.Time = V("time"); dp.Voltage = V("voltage");
//                 dp.CurrentX = V("currentx"); dp.CurrentY = V("currenty"); dp.CurrentZ = V("currentz");
//                 dp.CorrectedX = V("correctedx"); dp.CorrectedY = V("correctedy"); dp.CorrectedZ = V("correctedz");
//                 dp.MovingAverageAV = V("movingaverageav");
//                 if (!double.IsNaN(dp.Time) && !double.IsNaN(dp.Voltage))
//                     model.DataPoints.Add(dp);
//             }
//             return model;
//         }
//         public List<VoltageAnomaly> DetectAnomalies(VoltageLogModel model, double threshold)
//         {
//             return model.DataPoints
//                 .Where(p => !double.IsNaN(p.Voltage) && Math.Abs(p.Voltage - model.TargetVoltage) > threshold)
//                 .Select(p => new VoltageAnomaly { Time = p.Time, Voltage = p.Voltage, Deviation = Math.Abs(p.Voltage - model.TargetVoltage) })
//                 .ToList();
//         }
//         private static double P(string v, double d) =>
//             double.TryParse(v?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : d;
//     }
// }

// 简化写法：函数名写全、每个赋值单独一行、用 foreach 代替 LINQ、局部函数拆出来
using System.Globalization;
using PipeUi.Interfaces;
using PipeUi.Models;

namespace PipeUi.Services
{
    public class VoltageService : IVoltageService
    {
        public VoltageLogModel LoadDataFromStream(Stream stream)
        {
            var model = new VoltageLogModel();
            using var reader = new StreamReader(stream);

            // 读取所有行
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine() ?? "";
                lines.Add(line);
            }

            if (lines.Count < 3)
            {
                return model;
            }

            // 找 BeginHeader / EndHeader 的位置
            int hStart = -1;
            int hEnd = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.Equals("BeginHeader", StringComparison.OrdinalIgnoreCase))
                {
                    hStart = i;
                }
                if (trimmed.Equals("EndHeader", StringComparison.OrdinalIgnoreCase))
                {
                    hEnd = i;
                    break;
                }
            }

            if (hStart < 0 || hEnd <= hStart)
            {
                return model;
            }

            // 解析 Header 部分（每行格式：Key,Value）
            for (int i = hStart + 1; i < hEnd; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts.Length < 2)
                {
                    continue;
                }

                string key = parts[0].Trim();
                string val = parts[1].Trim();
                if (key == "")
                {
                    continue;
                }

                model.HeaderParameters[key] = val;

                // 根据 key 设置对应属性
                string keyLower = key.ToLowerInvariant();
                switch (keyLower)
                {
                    case "targetvoltage":
                        model.TargetVoltage = ParseDouble(val, model.TargetVoltage);
                        break;
                    case "gainz":
                        model.GainZ = ParseDouble(val, model.GainZ);
                        break;
                    case "gainy":
                        model.GainY = ParseDouble(val, model.GainY);
                        break;
                    case "thresholdz":
                        model.ThresholdZ = ParseDouble(val, model.ThresholdZ);
                        break;
                    case "thresholdy":
                        model.ThresholdY = ParseDouble(val, model.ThresholdY);
                        break;
                    case "trackingenabled":
                        bool isTrue = val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1";
                        model.TrackingEnabled = isTrue;
                        break;
                }
            }

            // 列名行：EndHeader 后面的第一个非空行
            int col = hEnd + 1;
            while (col < lines.Count)
            {
                if (!string.IsNullOrWhiteSpace(lines[col]))
                {
                    break;
                }
                col++;
            }

            if (col >= lines.Count)
            {
                return model;
            }

            // 解析列名（用逗号分隔，去掉空格）
            string[] columnParts = lines[col].Split(',');
            model.ColumnNames = new List<string>();
            for (int i = 0; i < columnParts.Length; i++)
            {
                model.ColumnNames.Add(columnParts[i].Trim());
            }

            // 建立列名→索引映射（方便后面通过列名找数据）
            var colMap = new Dictionary<string, int>();
            for (int i = 0; i < model.ColumnNames.Count; i++)
            {
                string colName = model.ColumnNames[i].ToLowerInvariant().Replace(" ", "");
                colMap[colName] = i;
            }

            // 解析数据行
            for (int i = col + 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] parts = lines[i].Split(',');
                var dp = new VoltageDataPoint();

                // 根据列名从 parts 中取值
                dp.Time = GetColumnValue(colMap, parts, "time");
                dp.Voltage = GetColumnValue(colMap, parts, "voltage");
                dp.CurrentX = GetColumnValue(colMap, parts, "currentx");
                dp.CurrentY = GetColumnValue(colMap, parts, "currenty");
                dp.CurrentZ = GetColumnValue(colMap, parts, "currentz");
                dp.CorrectedX = GetColumnValue(colMap, parts, "correctedx");
                dp.CorrectedY = GetColumnValue(colMap, parts, "correctedy");
                dp.CorrectedZ = GetColumnValue(colMap, parts, "correctedz");
                dp.MovingAverageAV = GetColumnValue(colMap, parts, "movingaverageav");

                // 只有 Time 和 Voltage 都有效才加入
                if (!double.IsNaN(dp.Time) && !double.IsNaN(dp.Voltage))
                {
                    model.DataPoints.Add(dp);
                }
            }

            return model;
        }

        public List<VoltageAnomaly> DetectAnomalies(VoltageLogModel model, double threshold)
        {
            // // 旧写法（LINQ 链式）
            // return model.DataPoints
            //     .Where(p => !double.IsNaN(p.Voltage) && Math.Abs(p.Voltage - model.TargetVoltage) > threshold)
            //     .Select(p => new VoltageAnomaly { Time = p.Time, Voltage = p.Voltage, Deviation = Math.Abs(p.Voltage - model.TargetVoltage) })
            //     .ToList();

            // 简化写法：用 foreach 循环代替 LINQ
            var anomalies = new List<VoltageAnomaly>();

            foreach (var point in model.DataPoints)
            {
                if (double.IsNaN(point.Voltage))
                {
                    continue;
                }

                double deviation = Math.Abs(point.Voltage - model.TargetVoltage);
                if (deviation > threshold)
                {
                    var anomaly = new VoltageAnomaly();
                    anomaly.Time = point.Time;
                    anomaly.Voltage = point.Voltage;
                    anomaly.Deviation = deviation;
                    anomalies.Add(anomaly);
                }
            }

            return anomalies;
        }

        // // 旧写法：单字母函数名 P，表达式体
        // private static double P(string v, double d) =>
        //     double.TryParse(v?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : d;

        // 简化写法：函数名写全，逻辑拆开
        private static double ParseDouble(string value, double defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            string trimmed = value.Trim();
            bool success = double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out double result);

            if (success)
            {
                return result;
            }
            return defaultValue;
        }

        // 简化写法：把局部函数 V() 提取成独立方法，名字写清楚
        private static double GetColumnValue(Dictionary<string, int> colMap, string[] parts, string columnName)
        {
            // 如果列名存在于映射中，且索引在 parts 范围内，就解析值
            if (colMap.TryGetValue(columnName, out int index) && index < parts.Length)
            {
                return ParseDouble(parts[index], double.NaN);
            }
            return double.NaN;
        }
    }
}

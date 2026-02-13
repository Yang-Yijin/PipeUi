//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace PipeUi.Models
//{
//    public class VoltageLogModel
//    {
//        // Header info
//        public string FileName { get; set; } = "";
//        public Dictionary<string, string> HeaderParameters { get; set; } = new Dictionary<string, string>();

//        // Config (保留原有的，向后兼容)
//        public double TargetVoltage { get; set; } = 120.0;
//        public double GainZ { get; set; } = 0.01;
//        public double ThresholdZ { get; set; } = 0.1;
//        public double GainY { get; set; } = 0.1;
//        public double ThresholdY { get; set; } = 0.0;
//        public double TransferHeight { get; set; } = 0.0;
//        public double PierceHeight { get; set; } = 0.0;
//        public double CutHeight { get; set; } = 0.0;
//        public double PierceTime { get; set; } = 0.0;
//        public string FilterType { get; set; } = "";
//        public int MovingAverage { get; set; } = 0;

//        // Data
//        public List<string> ColumnNames { get; set; } = new List<string>();
//        public List<VoltageDataPoint> DataPoints { get; set; } = new List<VoltageDataPoint>();

//        public VoltageStatistics GetStatistics()
//        {
//            if (DataPoints.Count == 0)
//                return new VoltageStatistics();

//            var voltages = DataPoints.Select(dp => dp.CurrentAV).Where(v => !double.IsNaN(v)).ToList();

//            if (voltages.Count == 0)
//                return new VoltageStatistics();

//            return new VoltageStatistics
//            {
//                Count = DataPoints.Count,
//                MinVoltage = voltages.Min(),
//                MaxVoltage = voltages.Max(),
//                AverageVoltage = voltages.Average()
//            };
//        }
//    }

//    public class VoltageDataPoint
//    {
//        public double Time { get; set; }              // Sample time
//        public double X { get; set; } = double.NaN;   // x (可能不存在)
//        public double Y { get; set; } = double.NaN;   // y
//        public double Z { get; set; } = double.NaN;   // z
//        public double CorrZ { get; set; } = double.NaN;      // CorrZ (可能不存在)
//        public double CoorY { get; set; } = double.NaN;      // CoorY (可能不存在)
//        public double CurrentAV { get; set; } = double.NaN;  // current AV
//        public double MovingAverageAV { get; set; } = double.NaN; // moving average AV (可能不存在)

//        // 向后兼容：旧的 FilteredVoltage 映射到 CurrentAV
//        public double FilteredVoltage
//        {
//            get => CurrentAV;
//            set => CurrentAV = value;
//        }
//    }

//    public class VoltageStatistics
//    {
//        public int Count { get; set; }
//        public double MinVoltage { get; set; }
//        public double MaxVoltage { get; set; }
//        public double AverageVoltage { get; set; }
//    }

//    public class VoltageAnomaly
//    {
//        public double Time { get; set; }
//        public double Voltage { get; set; }
//        public double Deviation { get; set; }
//    }
//}
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace PipeUi.Models
//{
//    public class VoltageLogModel
//    {
//        // Header info
//        public string FileName { get; set; } = "";
//        public Dictionary<string, string> HeaderParameters { get; set; } = new();

//        // ✅ New CSV header fields
//        public bool? TrackingEnabled { get; set; } = null;

//        // Config (keep + backward compatible)
//        public double TargetVoltage { get; set; } = 120.0;
//        public double GainZ { get; set; } = 0.01;
//        public double ThresholdZ { get; set; } = 0.1;
//        public double GainY { get; set; } = 0.1;
//        public double ThresholdY { get; set; } = 0.0;

//        public double TransferHeight { get; set; } = 0.0;
//        public double PierceHeight { get; set; } = 0.0;
//        public double CutHeight { get; set; } = 0.0;
//        public double PierceTime { get; set; } = 0.0;
//        public string FilterType { get; set; } = "";
//        public int MovingAverage { get; set; } = 0;

//        // Data
//        public List<string> ColumnNames { get; set; } = new();
//        public List<VoltageDataPoint> DataPoints { get; set; } = new();

//        public VoltageStatistics GetStatistics()
//        {
//            if (DataPoints.Count == 0)
//                return new VoltageStatistics();

//            // ✅ 统一统计用 Voltage（但兼容旧 CurrentAV）
//            var voltages = DataPoints
//                .Select(dp => dp.Voltage)
//                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
//                .ToList();

//            if (voltages.Count == 0)
//                return new VoltageStatistics();

//            return new VoltageStatistics
//            {
//                Count = DataPoints.Count,
//                MinVoltage = voltages.Min(),
//                MaxVoltage = voltages.Max(),
//                AverageVoltage = voltages.Average()
//            };
//        }
//    }

//    public class VoltageDataPoint
//    {
//        // ✅ Common
//        public double Time { get; set; } = double.NaN;

//        // ✅ New format main signal
//        public double Voltage { get; set; } = double.NaN;

//        // ✅ New format: Current X/Y/Z
//        public double CurrentX { get; set; } = double.NaN;
//        public double CurrentY { get; set; } = double.NaN;
//        public double CurrentZ { get; set; } = double.NaN;

//        // ✅ New format: Corrected X/Y/Z
//        public double CorrectedX { get; set; } = double.NaN;
//        public double CorrectedY { get; set; } = double.NaN;
//        public double CorrectedZ { get; set; } = double.NaN;

//        // ✅ Older/newer variant fields that appeared before
//        public double YPosition { get; set; } = double.NaN;
//        public double ZPosition { get; set; } = double.NaN;
//        public double YCommanded { get; set; } = double.NaN;
//        public double ZCommanded { get; set; } = double.NaN;

//        // ✅ Some files may have moving average voltage
//        public double MovingAverageAV { get; set; } = double.NaN;

//        // ==========================================================
//        // Backward compatibility aliases (so your old code still works)
//        // ==========================================================

//        // Old code uses X/Y/Z as positional/current values
//        public double X { get => CurrentX; set => CurrentX = value; }
//        public double Y { get => CurrentY; set => CurrentY = value; }
//        public double Z { get => CurrentZ; set => CurrentZ = value; }

//        // Old code uses CurrentAV / FilteredVoltage as the voltage signal
//        public double CurrentAV { get => Voltage; set => Voltage = value; }

//        public double FilteredVoltage
//        {
//            get => Voltage;
//            set => Voltage = value;
//        }

//        // Old code had CorrZ / CoorY naming
//        public double CorrZ { get => CorrectedZ; set => CorrectedZ = value; }
//        public double CoorY { get => CorrectedY; set => CorrectedY = value; }
//    }

//    public class VoltageStatistics
//    {
//        public int Count { get; set; }
//        public double MinVoltage { get; set; }
//        public double MaxVoltage { get; set; }
//        public double AverageVoltage { get; set; }
//    }

//    public class VoltageAnomaly
//    {
//        public double Time { get; set; }
//        public double Voltage { get; set; }
//        public double Deviation { get; set; }
//    }
//}
using System;
using System.Collections.Generic;
using System.Linq;

namespace PipeUi.Models
{
    public class VoltageLogModel
    {
        public string FileName { get; set; } = "";
        public Dictionary<string, string> HeaderParameters { get; set; } = new();

        public bool? TrackingEnabled { get; set; } = null;

        public double TargetVoltage { get; set; } = 120.0;
        public double GainZ { get; set; } = 0.01;
        public double ThresholdZ { get; set; } = 0.1;
        public double GainY { get; set; } = 0.1;
        public double ThresholdY { get; set; } = 0.0;

        public double TransferHeight { get; set; } = 0.0;
        public double PierceHeight { get; set; } = 0.0;
        public double CutHeight { get; set; } = 0.0;
        public double PierceTime { get; set; } = 0.0;
        public string FilterType { get; set; } = "";
        public int MovingAverage { get; set; } = 0;

        public List<string> ColumnNames { get; set; } = new();
        public List<VoltageDataPoint> DataPoints { get; set; } = new();

        public VoltageStatistics GetStatistics()
        {
            if (DataPoints.Count == 0)
                return new VoltageStatistics();

            var voltages = DataPoints
                .Select(dp => dp.Voltage)
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                .ToList();

            if (voltages.Count == 0)
                return new VoltageStatistics();

            return new VoltageStatistics
            {
                Count = DataPoints.Count,
                MinVoltage = voltages.Min(),
                MaxVoltage = voltages.Max(),
                AverageVoltage = voltages.Average()
            };
        }
    }

    public class VoltageDataPoint
    {
        public double Time { get; set; } = double.NaN;

        public double Voltage { get; set; } = double.NaN;

        public double CurrentX { get; set; } = double.NaN;
        public double CurrentY { get; set; } = double.NaN;
        public double CurrentZ { get; set; } = double.NaN;

        public double CorrectedX { get; set; } = double.NaN;
        public double CorrectedY { get; set; } = double.NaN;
        public double CorrectedZ { get; set; } = double.NaN;

        public double MovingAverageAV { get; set; } = double.NaN;

        // backward-compatible aliases
        public double X { get => CurrentX; set => CurrentX = value; }
        public double Y { get => CurrentY; set => CurrentY = value; }
        public double Z { get => CurrentZ; set => CurrentZ = value; }

        public double CurrentAV { get => Voltage; set => Voltage = value; }
        public double FilteredVoltage { get => Voltage; set => Voltage = value; }

        public double CorrZ { get => CorrectedZ; set => CorrectedZ = value; }
        public double CoorY { get => CorrectedY; set => CorrectedY = value; }
    }

    public class VoltageStatistics
    {
        public int Count { get; set; }
        public double MinVoltage { get; set; }
        public double MaxVoltage { get; set; }
        public double AverageVoltage { get; set; }
    }

    public class VoltageAnomaly
    {
        public double Time { get; set; }
        public double Voltage { get; set; }
        public double Deviation { get; set; }
    }

    public class SamplingAnomaly
    {
        public int Index { get; set; }
        public double Time { get; set; }
        public double DeltaTime { get; set; }
        public double ExpectedDelta { get; set; }
    }
}

using System.IO;
using System.Collections.Generic;
using PipeUi.Models;

namespace PipeUi.Interfaces
{
    public interface IVoltageService
    {
        VoltageLogModel LoadDataFromStream(Stream stream);
        List<VoltageAnomaly> DetectAnomalies(VoltageLogModel model, double threshold);
    }
}

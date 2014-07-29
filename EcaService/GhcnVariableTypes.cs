using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EcaService
{
    public static class GhcnVariableTypes
    {
        public static EcaVariable precip() { return new EcaVariable { UrlCode = "gdcnprcp", VariableCode = "PR", VariableName = "precipitation", DataFileName = "gdcnprcp", VariableID = 8 }; }
        public static EcaVariable tmin() { return new EcaVariable { UrlCode = "gdcntmin", VariableCode = "TN", VariableName = "minimum temperature", DataFileName = "gdcntmin", VariableID = 3 }; }
        public static EcaVariable tmax() { return new EcaVariable { UrlCode = "gdcntmin", VariableCode = "TN", VariableName = "minimum temperature", DataFileName = "gdcntmin", VariableID = 3 }; }
        public static EcaVariable snow() { return new EcaVariable { UrlCode = "gdcnsnwd", VariableCode = "SN", VariableName = "snow depth", DataFileName = "gdcnsnwd", VariableID = 9 }; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HydroDesktop.Interfaces.ObjectModel;
using System.IO;

namespace eca2sqlite
{
    class DataValueParser
    {
        public StationLookup SiteLookup { get; set; }

        public Source Source { get; set; }

        public QualityControlLevel QualityControl { get; set; }

        public Method TemperatureMethod { get; set; }

        public Method PrecipitationMethod { get; set; }

        public Qualifier ValidQualifier { get; set; }
        public Qualifier SuspectQualifier { get; set; }
        public Qualifier MissingQualifier { get; set; }

        public DataValueParser()
        {
            Source = Source.Unknown;
            Source.Description = "European Climate Assessment Dataset";
            Source.Citation = "Klein Tank, A.M.G. and Coauthors, 2002. Daily dataset of 20th-century surface " +
            "air temperature and precipitation series for the European Climate Assessment. " +
            "Int. J. of Climatol., 22, 1441-1453. ";
            Source.Link = "http://eca.knmi.nl";

            QualityControl = QualityControlLevel.Unknown;
            QualityControl.Definition = "Quality Controlled Data";

            TemperatureMethod = Method.Unknown;
            TemperatureMethod.Description = "Mean temperature calculated as weighted average of TN, TX and observations at 06, 12 and 18 UT (5 values)";

            PrecipitationMethod = Method.Unknown;

            SuspectQualifier = new Qualifier("1", "suspect");
            MissingQualifier = new Qualifier("9", "missing");
            ValidQualifier = new Qualifier("0", "valid");
        }

        public static Variable GetTemperatureVariable()
        {
            Variable v = new Variable();
            v.Code = "ECA:TG";
            v.DataType = "Average";
            v.GeneralCategory = "Climate";
            v.IsCategorical = false;
            v.IsRegular = true;
            v.TimeSupport = 1.0;
            v.TimeUnit = Unit.UnknownTimeUnit;
            v.TimeUnit.Name = "Day";
            v.TimeUnit.Abbreviation = "d";
            v.VariableUnit = Unit.Unknown;
            v.VariableUnit.Name = "Degrees Celsius";
            v.VariableUnit.Abbreviation = "degC";
            v.VariableUnit.UnitsType = "temperature";
            v.NoDataValue = -9999;
            v.ValueType = "Derived value";
            v.SampleMedium = "Air";
            v.Name = "Temperature";
            v.Speciation = "Not Applicable";

            return v;
        }

        public static Variable GetPrecipitationVariable()
        {
            Variable v = new Variable();
            v.Code = "ECA:RR";
            v.DataType = "Incremental";
            v.GeneralCategory = "Climate";
            v.IsCategorical = false;
            v.IsRegular = true;
            v.TimeSupport = 1.0;
            v.TimeUnit = Unit.UnknownTimeUnit;
            v.TimeUnit.Name = "Day";
            v.TimeUnit.Abbreviation = "d";
            v.VariableUnit = Unit.Unknown;
            v.VariableUnit.Name = "millimeter";
            v.VariableUnit.Abbreviation = "mm";
            v.VariableUnit.UnitsType = "temperature";
            v.NoDataValue = -9999;
            v.ValueType = "Derived value";
            v.SampleMedium = "Precipitation";
            v.Name = "Precipitation";
            v.Speciation = "Not Applicable";

            return v;
        }
        
        public Series ParseSeries(Variable var, string textFileName)
        {
            Series s = new Series();
            
            string line = String.Empty;
            int startLine = 22;
            using (StreamReader r = new StreamReader(textFileName))
            {
                int counter = 0;
                while ((line = r.ReadLine()) != null)
                {
                    counter++;

                    if (counter == startLine)
                    {
                        int staid = ParseStationId(line);
                        s.Site = SiteLookup.FindSiteByID(staid);
                        s.Variable = var;
                        s.Method = (s.Variable.Code == "ECA:TG") ? TemperatureMethod : PrecipitationMethod;
                        s.Source = Source;
                        s.QualityControlLevel = QualityControl;
                        
                    }

                    if (counter >= startLine)
                    {
                        DataValue v = ParseDataValue(line);
                        s.AddDataValue(v);
                    }
                }
            }
            return s;
        }

        private DataValue ParseDataValue(string line)
        {
            DateTime date = ParseDate(line.Substring(14, 8));
            int dv = int.Parse(line.Substring(23, 5));
            double val = (dv == -9999) ? dv : dv / 10.0;
            DataValue newDataValue = new DataValue(val, date, 0);
            //newDataValue.Qualifier = ValidQualifier;
            //int qualifier = int.Parse(line.Substring(29, 5));
            //if (qualifier == 0)
            //    newDataValue.Qualifier = ValidQualifier;
            //else if (qualifier == 1)
            //    newDataValue.Qualifier = SuspectQualifier;
            //else
            //    newDataValue.Qualifier = MissingQualifier;

            return newDataValue;
        }

        private DateTime ParseDate(string str)
        {
            return new DateTime(int.Parse(str.Substring(0,4)), int.Parse(str.Substring(4,2)), int.Parse(str.Substring(6,2)));
        }

        private int ParseStationId(string line)
        {
            return int.Parse(line.Substring(0, 6));
        }
    }
}

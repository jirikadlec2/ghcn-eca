using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HydroDesktop.Interfaces;
using HydroDesktop.Database;
using HydroDesktop.Interfaces.ObjectModel;
using System.IO;

namespace eca2sqlite
{
    public class EcaDbCreator
    {
        public EcaDbCreator(string sqliteDbPath)
        {
            dbPath = sqliteDbPath; 
        }
        
        public string dbPath { get; set; }
        
        public string tgDir = "E:\\dev\\eca\\eca_blend_tg\\";
        public string rrDir = "E:\\dev\\eca\\eca_blend_rr\\";

        private int fiftyYearValueCount = (int)((DateTime.Now.Date.Subtract(new DateTime(1963, 1, 1))).TotalDays);
        
        public bool IsSuitableTgSeries(Series tgSeries)
        {
            if (tgSeries.Site.Name.ToLower().StartsWith("praha"))
                return true;
            
            if (tgSeries.Site.County == "RU" || tgSeries.Site.County == "NL" || tgSeries.Site.County == "DK" || tgSeries.Site.County == "NO")
                return false;

            if (tgSeries.Site.County != "DE" || tgSeries.Site.County != "CZ")
                return false;
            
            if (tgSeries.DataValueList[tgSeries.ValueCount - 1].Value == tgSeries.Variable.NoDataValue)
                return false;

            if (tgSeries.ValueCount < fiftyYearValueCount)
                return false;

            string pcpFile = string.Format("{0}RR_STAID{1}.txt", rrDir, tgSeries.Site.Id.ToString("000000"));
            if (!File.Exists(pcpFile))
                return false;

            return true;
        }

        public bool IsSuitableRRSeries(Series tgSeries, Series rrSeries)
        {
            double nd = rrSeries.Variable.NoDataValue;
            //if (rrSeries.DataValueList[rrSeries.ValueCount - 1].Value == nd && rrSeries.DataValueList[rrSeries.ValueCount - 2].Value == nd)
            //    return false;

            if (rrSeries.ValueCount < fiftyYearValueCount)
                return false;

            return true;
        }

        public void ExportStations()
        {
            
            string connString = SQLiteHelper.GetSQLiteConnectionString(dbPath);

            IRepositoryManager manager = RepositoryFactory.Instance.Get<IRepositoryManager>(DatabaseTypes.SQLite, connString);

            StationLookup stations = new StationLookup();
            stations.CreateStationLookup("E:\\dev\\eca\\eca_blend_station_all.txt");

            //temperature stations
            DataValueParser p = new DataValueParser();
            p.SiteLookup = stations;
            Variable temperature = DataValueParser.GetTemperatureVariable();
            Variable precipitation = DataValueParser.GetPrecipitationVariable();

            Theme ecaTheme = new Theme { Name = "ECA_Temperature" };

            foreach (string file in Directory.GetFiles(tgDir))
            {
                string fn = Path.GetFileName(file);
                if (fn.StartsWith("TG_"))
                {
                    try
                    {
                        Series tgs = p.ParseSeries(temperature, file);

                        Console.WriteLine(String.Format("checking {0} {1} {2}", tgs.Variable.Code, tgs.Site.Code, tgs.Site.Name));

                        if (IsSuitableTgSeries(tgs))
                        {
                            string pcpFile = string.Format("{0}RR_STAID{1}.txt", rrDir, tgs.Site.Id.ToString("000000"));

                            Series rrs = p.ParseSeries(precipitation, pcpFile);

                            if (IsSuitableRRSeries(tgs, rrs))
                            {
                                manager.SaveSeries(tgs, ecaTheme, OverwriteOptions.Copy);
                                manager.SaveSeries(rrs, ecaTheme, OverwriteOptions.Copy);
                                Console.WriteLine(string.Format("saved {0} {1}", tgs.Site.Code, tgs.Site.Name));
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.IO;
using System.Globalization;

namespace EcaService
{
    /// <summary>
    /// The EcaSiteUpdater class is responsible for getting the most up-to-date list of sites
    /// and updating the Sites XML file.
    /// </summary>
    public class EcaSiteUpdater
    {
        //set-up of the user agent
        string defaultUserAgent = @"Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)";
        
        public string connString = "Server=sql4.aspone.cz;Database=db1677;User ID=db1677;Password=AnnAgnps;";
        
        /// <summary>
        /// Updates the list of sites by accessing KNMI Climate Explorer website
        /// </summary>
        public void UpdateListOfSites()
        {
            //(1) Download the complete file Stations TXT

            //(2) For each station: query the possible variables

            //OR--> Faster --> For each station: check if it is in the list of supported variables!
            //--> StationsVariables table

            SqlConnection conn = new SqlConnection(connString);
            conn.Open();

            using (SqlDataAdapter a = new SqlDataAdapter("SELECT * FROM Sites", conn))
            {
                DataTable tab = new DataTable();
                a.Fill(tab);
            }
        }

        public List<EcaSite> GetSiteListFromTextFile() 
        {
            string textFile = @"E:\dev\drought\EcaService\usa_sites.txt";
            List<EcaSite> siteList = new List<EcaSite>();

            using (StreamReader r = new StreamReader(textFile))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    string[] fields = line.Split(new char[] { ' ' },StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length == 6)
                    {
                        if (fields[0] == "STAID") continue;

                        EcaSite s = new EcaSite();
                        s.SiteCode2 = fields[0].Trim();
                        s.SiteCode = GhcnSiteCodeToSiteID(s.SiteCode2);
                        
                        string sn = fields[5].Trim();

                        if (!sn.Contains(",_ID")) continue;

                        s.SiteName = fields[5].Trim().Replace("_"," ");
                        s.Country = "USA";
                        s.Latitude = Convert.ToDouble(fields[2].Trim(), CultureInfo.InvariantCulture);
                        s.Longitude = Convert.ToDouble(fields[1].Trim(), CultureInfo.InvariantCulture);
                        s.Elevation = (int)(Convert.ToDouble(fields[3].Trim(), CultureInfo.InvariantCulture));

                        siteList.Add(s);
                    }
                }
            }
            return siteList;
        }

        public static long GhcnSiteCodeToSiteID(string siteCode) 
        {
            if (siteCode.StartsWith("USW"))
                return Convert.ToInt64(("998" + siteCode.Substring(3)));
            else if (siteCode.StartsWith("USC"))
                return Convert.ToInt64(("999" + siteCode.Substring(3)));
            else
                return 0;
        }

        public static string GhcnSiteIdToSiteCode(long siteId) 
        {
            string siteCodeStr = siteId.ToString();
            
            if (siteCodeStr.StartsWith("998"))
                return "USW" + siteCodeStr.Substring(3);
            else if (siteCodeStr.StartsWith("999"))
                return "USC" + siteCodeStr.Substring(3);
            else
                return siteCodeStr;
        }

        public List<EcaSite> GetInitialEcaSiteList()
        {
            string siteListUri = @"http://eca.knmi.nl/download/stations.txt";
            
            HttpWebRequest req= (HttpWebRequest)HttpWebRequest.Create(siteListUri);

            List<EcaSite> siteList = new List<EcaSite>();

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        string[] fields = line.Split(new char[] { ',' });
                        if (fields.Length == 6)
                        {
                            if (fields[0] == "STAID") continue;

                            EcaSite s = new EcaSite();
                            s.SiteCode = int.Parse(fields[0].Trim());
                            s.SiteName = fields[1].Trim();
                            s.Country = fields[2].Trim();
                            s.Latitude = ParseLatLon(fields[3].Trim());
                            s.Longitude = ParseLatLon(fields[4].Trim());
                            s.Elevation = int.Parse(fields[5].Trim());

                            siteList.Add(s);
                        }
                    }
                }
            }
            return siteList;
        }

        /// <summary>
        /// Gets the list of all ECA SiteID's that measure the selected variable
        /// </summary>
        /// <param name="ecaVariableCode"></param>
        /// <returns></returns>
        public List<long> GetSitesForVariable(string ecaVariableCode)
        {
            List<long> validSiteCodes = new List<long>();
            
            string uri = @"http://climexp.knmi.nl/getstations.cgi";
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.UserAgent = defaultUserAgent;
            req.Referer = @"http://climexp.knmi.nl/selectdailyseries.cgi?id=someone@somewhere";

            ASCIIEncoding encoding = new ASCIIEncoding();
            string postData = "email=someone@somewhere";
            postData += ("&climate=" + ecaVariableCode);
            postData += ("&num=10");
            postData += ("&lat1=0");
            postData += ("&lat2=90");
            postData += ("&lon1=-60");
            postData += ("&lon2=100");
            postData += ("&min=1");
            byte[] data = encoding.GetBytes(postData);

            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = data.Length;

            Stream newStream = req.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();

            string ecaStationCodeText = "ECA station code:";
            int ecaStationCodeTextLength = ecaStationCodeText.Length;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        if (line.StartsWith("ECA station code:"))
                        {
                            long stationCode = long.Parse(line.Substring(ecaStationCodeTextLength + 1, line.IndexOf("(") - ecaStationCodeTextLength - 1));
                            validSiteCodes.Add(stationCode);
                        }
                    }
                }
            }
            return validSiteCodes;
        }

        private double ParseLatLon(string dms)
        {
            string[] fields = dms.Split(new char[] { ':' });
            string deg = fields[0].Substring(1, fields[0].Length - 1);
            string hem = fields[0].Substring(0, 1);
            string min = fields[1];
            string sec = fields[2];
            int hemisphere = (deg == "-") ? -1 : 1;
            double dd = hemisphere * (int.Parse(deg) + double.Parse(min) / 60.0 + double.Parse(sec) / 3600.0);
            return dd;
        }

        public List<EcaVariable> GetEcaVariables()
        {
            List<EcaVariable> variablesList = new List<EcaVariable>();
            variablesList.Add(new EcaVariable { UrlCode = "ecaprcp", VariableCode = "RR", VariableName = "precipitation", DataFileName = "peca", VariableID = 1 });
            variablesList.Add(new EcaVariable { UrlCode = "ecatemp", VariableCode = "TG", VariableName = "average temperature", DataFileName = "teca", VariableID = 2 });
            variablesList.Add(new EcaVariable { UrlCode = "ecatmin", VariableCode = "TN", VariableName = "minimum temperature", DataFileName = "neca", VariableID = 3 });
            variablesList.Add(new EcaVariable { UrlCode = "ecatmax", VariableCode = "TX", VariableName = "maximum temperature", DataFileName = "xeca", VariableID = 4 });
            variablesList.Add(new EcaVariable { UrlCode = "ecapres", VariableCode = "PP", VariableName = "air pressure", DataFileName = "seca", VariableID = 5 });
            variablesList.Add(new EcaVariable { UrlCode = "ecasnow", VariableCode = "SD", VariableName = "snow depth", DataFileName = "deca", VariableID = 6 });
            variablesList.Add(new EcaVariable { UrlCode = "ecaclou", VariableCode = "CC", VariableName = "cloud cover", DataFileName = "ceca", VariableID = 7 });

            return variablesList;
        }

        public List<EcaVariable> GetGhcnVariables() 
        {
            List<EcaVariable> variablesList = new List<EcaVariable>();
            EcaVariable precip = new EcaVariable { UrlCode = "gdcnprcp", VariableCode = "PR", VariableName = "precipitation", DataFileName = "gdcnprcp", VariableID = 8 };
            variablesList.Add(precip);
            return variablesList;
        }

        //public List<EcaSiteInfo> GetGhcnSiteInformation() 
        //{
        //    List<EcaSiteInfo> siteInfoList = new List<EcaSiteInfo>();

        //    EcaVariable precip = GetGhcnVariables()[0];

        //    List<EcaSite> initialSiteList = GetSiteListFromTextFile();

        //    EcaVisitor vis = new EcaVisitor();
        //    foreach (EcaSite s in initialSiteList)
        //    {
        //        EcaTimeValue[] ectv2 = vis.GetGhcnDataValues(s.SiteCode2, DateTime.MinValue, DateTime.MaxValue);
        //        DateTime begin = ectv2[0].DateTimeUtc;
        //        DateTime end = ectv2[ectv2.Length - 1].DateTimeUtc;
        //        int valueCount = ectv2.Length;

        //        List<EcaSeriesInfo> seriesList = new List<EcaSeriesInfo>();
        //        seriesList.Add(new EcaSeriesInfo { Variable = precip, StartDate = begin, EndDate = end, ValueCount = valueCount });
        //        siteInfoList.Add(new EcaSiteInfo { Site= s, DataSeriesList = seriesList });
        //        Console.WriteLine(siteInfoList.Count + " " + s.SiteName);
        //    }
        //    return siteInfoList;
        //}

        public List<EcaSiteInfo> GetEcaSiteInformation()
        {
            List<EcaSiteInfo> siteInfoList = new List<EcaSiteInfo>();
            
            List<EcaVariable> variablesList = GetEcaVariables();

            List<EcaSite> initialSiteList = GetInitialEcaSiteList();

            foreach(EcaVariable v in variablesList)
            {
                List<long> siteIdList = GetSitesForVariable(v.UrlCode);
                
                foreach(EcaSite testSite in initialSiteList)
                {
                    if (siteIdList.Contains(testSite.SiteCode))
                    {
                        IEnumerable<EcaSiteInfo> existingSiteInfos = siteInfoList.Where(s => s.Site.SiteCode == testSite.SiteCode);
                        if (existingSiteInfos.Count() > 0)
                        {
                            existingSiteInfos.First().DataSeriesList.Add(new EcaSeriesInfo { Variable = v});
                        }
                        else
                        {
                            EcaSiteInfo newSiteInfo = new EcaSiteInfo();
                            newSiteInfo.Site = testSite;
                            newSiteInfo.DataSeriesList.Add(new EcaSeriesInfo { Variable = v});
                            siteInfoList.Add(newSiteInfo);
                        }
                    }

                }
            }

            return siteInfoList;
        }

        //public void SaveGhcnSiteInformationToDatabase() 
        //{
        //    //(1) Save or update each Variable
        //    List<EcaVariable> variablesList = GetGhcnVariables();
        //    foreach (EcaVariable v in variablesList)
        //    {
        //        SaveOrUpdateVariable(v);
        //    }

        //    List<EcaSiteInfo> siteInfoList = GetGhcnSiteInformation();
        //    EcaVisitor visitor = new EcaVisitor();

            
            
        //    //update info on startDate, EndDate, ValueCount
        //    foreach (EcaSiteInfo siteInfo in siteInfoList)
        //    {
        //        //(2) Save or update each series information
        //        try
        //        {
        //            SaveOrUpdateSite(siteInfo.Site);
        //            SaveOrUpdateSiteInfo(siteInfo);
        //        }
        //        catch { }
        //    }
        //}

        /// <summary>
        /// Saves the site information to the database
        /// </summary>
        public void SaveSiteInformationToDatabase()
        {
            //(1) Save or update each Variable
            List<EcaVariable> variablesList = GetEcaVariables();
            foreach (EcaVariable v in variablesList)
            {
                SaveOrUpdateVariable(v);
            }

            List<EcaSiteInfo> siteInfoList = GetEcaSiteInformation();
            EcaVisitor visitor = new EcaVisitor();

            foreach (EcaSiteInfo siteInfo in siteInfoList)
            {
                if (siteInfo.Site.SiteCode < 564) continue;
                
                Console.WriteLine(siteInfo.Site.SiteCode + " " + siteInfo.Site.SiteName);
                
                //update info on startDate, EndDate, ValueCount
                foreach (EcaSeriesInfo series in siteInfo.DataSeriesList)
                {
                    EcaTimeValue[] valueArray = new EcaTimeValue[1];
                    try
                    {
                        valueArray = visitor.GetEcaDataValues(series.Variable.UrlCode, siteInfo.Site.SiteCode);
                    }
                    catch
                    {
                        //in case of exception, skip this series
                        continue;
                    }
                    if (valueArray.Length > 1)
                    {
                        series.StartDate = valueArray[0].DateTimeUtc;
                        series.EndDate = valueArray[valueArray.Length - 1].DateTimeUtc;
                        series.ValueCount = (int)(Math.Round((series.EndDate.Subtract(series.StartDate)).TotalDays));
                    }
                }

                //(2) Save or update each series information
                try
                {
                    SaveOrUpdateSite(siteInfo.Site);
                    SaveOrUpdateSiteInfo(siteInfo);
                }
                catch { }
            }
        }

        /// <summary>
        /// Updates the StartDate, EndDate, ValueCount information by calling GetValues from ECA
        /// </summary>
        /// <param name="siteInfo">Site Info</param>
        public void SaveOrUpdateSiteInfo(EcaSiteInfo siteInfo)
        {
            //(1) Assume that the Site, Variable already exist
            foreach(EcaSeriesInfo series in siteInfo.DataSeriesList)
            {
                if (series.ValueCount == 0) continue;

                EcaVariable variable = series.Variable;
                
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    object siteIdResult = null;
                    object varIdResult = null;
                    using (SqlCommand cmd = new SqlCommand("SELECT SiteID, VariableID FROM DataSeries WHERE SiteID = @siteid AND VariableID = @varid", connection))
                    {
                        cmd.Parameters.Add(new SqlParameter("@siteid",siteInfo.Site.SiteCode));
                        cmd.Parameters.Add(new SqlParameter("@varid", variable.VariableID));
                        connection.Open();
                        SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow);
                        
                        if (reader.HasRows)
                        {
                            reader.Read();
                            siteIdResult = reader[0];
                            varIdResult = reader[1];
                        }
                        connection.Close();
                    }

                    if (siteIdResult != null && varIdResult != null)
                    {
                        //update the site
                        using (SqlCommand cmd = new SqlCommand("UPDATE DataSeries SET BeginDateTime = @begintime, EndDateTime = @endtime, ValueCount=@valuecount WHERE SiteID = @siteid AND VariableID = @varid", connection))
                        {
                            cmd.Parameters.Add(new SqlParameter("@begintime", SqlDbType.DateTime));
                            cmd.Parameters["@begintime"].Value = series.StartDate;
                            cmd.Parameters.Add(new SqlParameter("@endtime", SqlDbType.DateTime));
                            cmd.Parameters["@endtime"].Value = series.EndDate;
                            cmd.Parameters.Add(new SqlParameter("@valuecount", series.ValueCount));
                            cmd.Parameters.Add(new SqlParameter("@siteid", siteInfo.Site.SiteCode));
                            cmd.Parameters.Add(new SqlParameter("@varid", variable.VariableID));
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                    else
                    {
                        //save the site
                        using (SqlCommand cmd = new SqlCommand("INSERT INTO DataSeries(SiteID, VariableID, BeginDateTime, EndDateTime, ValueCount) VALUES (@siteid, @varid, @begintime, @endtime, @valuecount)", connection))
                        {
                            connection.Open();
                            cmd.Parameters.Add(new SqlParameter("@begintime", SqlDbType.DateTime));
                            cmd.Parameters["@begintime"].Value = series.StartDate;
                            cmd.Parameters.Add(new SqlParameter("@endtime", SqlDbType.DateTime));
                            cmd.Parameters["@endtime"].Value = series.EndDate;
                            cmd.Parameters.Add(new SqlParameter("@valuecount", series.ValueCount));
                            cmd.Parameters.Add(new SqlParameter("@siteid", siteInfo.Site.SiteCode));
                            cmd.Parameters.Add(new SqlParameter("@varid", variable.VariableID));
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
            }
        }

        public void SaveOrUpdateSite(EcaSite site)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                object siteIdResult = null;
                using (SqlCommand cmd = new SqlCommand("SELECT SiteID FROM Sites WHERE SiteID = @id", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@id", site.SiteCode));
                    connection.Open();
                    siteIdResult = cmd.ExecuteScalar();
                    connection.Close();
                }

                if (siteIdResult != null)
                {
                    //update the site
                    using (SqlCommand cmd = new SqlCommand("UPDATE Sites SET SiteCode=@code, SiteName = @name, Latitude=@lat, Longitude =@lon, Elevation=@elev, Country=@country WHERE SiteID = @id", connection))
                    {
                        cmd.Parameters.Add(new SqlParameter("@code", site.SiteCode2));
                        cmd.Parameters.Add(new SqlParameter("@name", site.SiteName));
                        cmd.Parameters.Add(new SqlParameter("@lat", site.Latitude));
                        cmd.Parameters.Add(new SqlParameter("@lon", site.Longitude));
                        cmd.Parameters.Add(new SqlParameter("@elev", site.Elevation));
                        cmd.Parameters.Add(new SqlParameter("@country", site.Country));
                        cmd.Parameters.Add(new SqlParameter("@id", site.SiteCode));
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                else
                {
                    //save the site
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO Sites(SiteCode, SiteID, SiteName, Latitude, Longitude, Elevation, Country) VALUES (@code, @id, @name, @lat, @lon, @elev, @country)", connection))
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@code", site.SiteCode2));
                        cmd.Parameters.Add(new SqlParameter("@id", site.SiteCode));
                        cmd.Parameters.Add(new SqlParameter("@name", site.SiteName));
                        cmd.Parameters.Add(new SqlParameter("@lat", site.Latitude));
                        cmd.Parameters.Add(new SqlParameter("@lon", site.Longitude));
                        cmd.Parameters.Add(new SqlParameter("@elev", site.Elevation));
                        cmd.Parameters.Add(new SqlParameter("@country", site.Country));
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }
        }

        public void SaveOrUpdateVariable(EcaVariable variable)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                object variableIdResult = null;
                using (SqlCommand cmd = new SqlCommand("SELECT VariableID FROM Variables WHERE VariableCode = @code", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                    connection.Open();
                    variableIdResult = cmd.ExecuteScalar();
                    connection.Close();
                }

                if (variableIdResult != null)
                {
                    //update the variable
                    using (SqlCommand cmd = new SqlCommand("UPDATE Variables SET VariableName = @name, VariableCode2=@code2 WHERE VariableID = @id", connection))
                    {
                        cmd.Parameters.Add(new SqlParameter("@name", variable.VariableName));
                        cmd.Parameters.Add(new SqlParameter("@code2", variable.UrlCode));
                        cmd.Parameters.Add(new SqlParameter("@id", Convert.ToInt32(variableIdResult)));
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                else
                {
                    //save the variable
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO Variables(VariableID, VariableCode, VariableName, VariableCode2) VALUES (@id, @code, @name, @code2)", connection))
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@id", variable.VariableID));
                        cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                        cmd.Parameters.Add(new SqlParameter("@name", variable.VariableName));
                        cmd.Parameters.Add(new SqlParameter("@code2", variable.UrlCode));
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }
        }

    }

    


    public class EcaSite
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Elevation { get; set; }
        public string SiteName { get; set; }
        public long SiteCode { get; set; }
        public string SiteCode2 { get; set; }
        public string Country { get; set; }
    }

    public class EcaVariable
    {
        public int VariableID { get; set; }
        public string VariableName { get; set; }
        public string VariableCode { get; set; }
        public string UrlCode { get; set; }
        public string DataFileName { get; set; }
    }

    public class EcaSiteInfo
    {
        public EcaSiteInfo()
        {
            DataSeriesList = new List<EcaSeriesInfo>();
        }
        
        public EcaSite Site { get; set; }
        public List<EcaSeriesInfo> DataSeriesList { get; set; }
    }

    public class EcaSeriesInfo
    {
        public EcaVariable Variable { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ValueCount { get; set; }
    }

    public class EcaTimeValue
    {
        public DateTime DateTimeUtc { get; set; }
        public double DataValue { get; set; }
    }
}

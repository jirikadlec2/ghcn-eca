using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Web;
using System.Configuration;
using WaterOneFlow.Schema.v1_1;
using WaterOneFlow;
using WaterOneFlowImpl.v1_1;
using WaterOneFlowImpl;
using System.Data;
using WaterOneFlowImpl.geom;
using System.Globalization;
using EcaService;
using System.IO;

namespace WaterOneFlow.odws
{
    /// <summary>
    /// The web service utilities
    /// </summary>
    public class WebServiceUtils
    {
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
        }
        
        public static string GetBaseUri()
        {
            string uri = HttpContext.Current.Request.Url.ToString();
            if (uri.EndsWith(".asmx"))
            {
                return uri;
            }
            else
            {
                return uri.Remove(uri.IndexOf(".asmx")) + ".asmx";
            }
        }
        
        public static QueryInfoType CreateQueryInfo(string webMethodName)
        {
            QueryInfoType queryInfo = new QueryInfoType();
            queryInfo.creationTime = DateTime.Now;
            queryInfo.creationTimeSpecified = true;
            queryInfo.criteria = new QueryInfoTypeCriteria();
            queryInfo.criteria.locationParam = String.Empty;
            queryInfo.criteria.MethodCalled = webMethodName;
            queryInfo.criteria.variableParam = String.Empty;
            queryInfo.queryURL = @"http://example.com";
            
            return queryInfo;
        }

        /// <summary>
        /// Gets the sites, in XML format [test for SNOW]
        /// </summary>
        public static SiteInfoResponseTypeSite[] GetSitesFromDb()
        {
            List<SiteInfoResponseTypeSite> siteList = new List<SiteInfoResponseTypeSite>();

            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    //string sqlSites = "SELECT Stations.st_id, st_name, altitude, location_id FROM Stations  WHERE Stations.st_id IN (SELECT st_id FROM StationsVariables WHERE var_id=8 OR var_id=1)";
                    string sql = "SELECT SiteID, SiteCode, SiteName, Latitude, Longitude, Elevation FROM Sites st " +
                        "WHERE SiteID IN (SELECT SiteID FROM DataSeries)";

                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        SiteInfoResponseTypeSite newSite = new SiteInfoResponseTypeSite();
                        SiteInfoType si = new SiteInfoType();
                        
                        if (dr["Elevation"] != DBNull.Value)
                        {
                            si.elevation_m = Convert.ToDouble(dr["Elevation"]);
                            si.elevation_mSpecified = true;
                        }
                        else
                        {
                            si.elevation_m = 0;
                            si.elevation_mSpecified = true;
                        }
                        si.geoLocation = new SiteInfoTypeGeoLocation();
                        
                        LatLonPointType latLon = new LatLonPointType();
                        latLon.latitude = Convert.ToDouble(dr["Latitude"]);
                        latLon.longitude = Convert.ToDouble(dr["Longitude"]);
                        latLon.srs = "EPSG:4326";
                        si.geoLocation.geogLocation = latLon;
                        si.geoLocation.localSiteXY = new SiteInfoTypeGeoLocationLocalSiteXY[1];
                        si.geoLocation.localSiteXY[0] = new SiteInfoTypeGeoLocationLocalSiteXY();
                        si.geoLocation.localSiteXY[0].X = latLon.longitude;
                        si.geoLocation.localSiteXY[0].Y = latLon.latitude;
                        si.geoLocation.localSiteXY[0].ZSpecified = false;
                        si.geoLocation.localSiteXY[0].projectionInformation = si.geoLocation.geogLocation.srs;
                        si.metadataTimeSpecified = false;
                        //si.oid = Convert.ToString(dr["st_id"]);
                        si.note = new NoteType[1];
                        si.note[0] = new NoteType();
                        si.note[0].title = "my note";
                        si.note[0].type = "custom";
                        si.note[0].Value = "GHCN";
                        si.verticalDatum = "Unknown";

                        si.siteCode = new SiteInfoTypeSiteCode[1];
                        si.siteCode[0] = new SiteInfoTypeSiteCode();
                        si.siteCode[0].network = serviceCode;
                        si.siteCode[0].siteIDSpecified = false;
                        si.siteCode[0].Value = Convert.ToString(dr["SiteCode"]);

                        si.siteName = Convert.ToString(dr["SiteName"]);

                        newSite.siteInfo = si;
                        siteList.Add(newSite);
                    }
                }
            }
            return siteList.ToArray();
        }

        /// <summary>
        /// Gets the sites, in XML format [test for SNOW]
        /// </summary>
        public static SiteInfoResponseTypeSite[] GetSitesByBox(box queryBox, bool includeSeries)
        {
            List<SiteInfoResponseTypeSite> siteList = new List<SiteInfoResponseTypeSite>();

            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sqlSites = "SELECT SiteID, SiteCode, SiteName, Latitude, Longitude, Elevation FROM Sites st " +
                        "WHERE SiteID IN (SELECT SiteID FROM DataSeries)";
                    
                    cmd.CommandText = sqlSites;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        SiteInfoResponseTypeSite newSite = new SiteInfoResponseTypeSite();
                        SiteInfoType si = new SiteInfoType();

                        if (dr["Elevation"] != DBNull.Value)
                        {
                            si.elevation_m = Convert.ToDouble(dr["Elevation"]);
                            si.elevation_mSpecified = true;
                        }
                        else
                        {
                            si.elevation_m = 0;
                            si.elevation_mSpecified = true;
                        }
                        si.geoLocation = new SiteInfoTypeGeoLocation();

                        LatLonPointType latLon = new LatLonPointType();
                        latLon.latitude = Convert.ToDouble(dr["Latitude"]);
                        latLon.longitude = Convert.ToDouble(dr["Longitude"]);
                        latLon.srs = "EPSG:4326";
                        si.geoLocation.geogLocation = latLon;
                        si.geoLocation.localSiteXY = new SiteInfoTypeGeoLocationLocalSiteXY[1];
                        si.geoLocation.localSiteXY[0] = new SiteInfoTypeGeoLocationLocalSiteXY();
                        si.geoLocation.localSiteXY[0].X = latLon.longitude;
                        si.geoLocation.localSiteXY[0].Y = latLon.latitude;
                        si.geoLocation.localSiteXY[0].ZSpecified = false;
                        si.geoLocation.localSiteXY[0].projectionInformation = si.geoLocation.geogLocation.srs;
                        si.metadataTimeSpecified = false;
                        //si.oid = Convert.ToString(dr["st_id"]);
                        si.note = new NoteType[1];
                        si.note[0] = new NoteType();
                        si.note[0].title = "my note";
                        si.note[0].type = "custom";
                        si.note[0].Value = "GHCN";
                        si.verticalDatum = "Unknown";

                        si.siteCode = new SiteInfoTypeSiteCode[1];
                        si.siteCode[0] = new SiteInfoTypeSiteCode();
                        si.siteCode[0].network = serviceCode;
                        si.siteCode[0].siteIDSpecified = false;
                        si.siteCode[0].Value = Convert.ToString(dr["SiteCode"]);

                        si.siteName = Convert.ToString(dr["SiteName"]);

                        newSite.siteInfo = si;
                        siteList.Add(newSite);
                    }
                }
            }
            return siteList.ToArray();
        }

        public static seriesCatalogTypeSeries GetSeriesCatalogFromDb(string siteCode, int variableId)
        {
            string serviceCode = ConfigurationManager.AppSettings["network"];
            if (siteCode.StartsWith(serviceCode))
            {
                siteCode = siteCode.Substring(serviceCode.Length);
            }
            
            string connStr = GetConnectionString();
            
            seriesCatalogTypeSeries s = new seriesCatalogTypeSeries();

            s.dataType = "Average";
            s.generalCategory = "Climate";

            //method
            s.method = new MethodType();
            if (variableId == 8)
            {
                s.method.methodCode = "1";
                s.method.methodID = 1;
                s.method.methodDescription = "Precipitation measured at 7:00 local time";
                s.method.methodLink = "http://climexp.knmi.nl";
            }
            else
            {
                s.method.methodCode = "2";
                s.method.methodID = 2;
                s.method.methodDescription = "Measured at professional climate station";
                s.method.methodLink = "http://climexp.knmi.nl";
            }

            //qc level
            s.qualityControlLevel = new QualityControlLevelType();
            s.qualityControlLevel.definition = "quality controlled data";
            s.qualityControlLevel.explanation = "quality controlled data";
            s.qualityControlLevel.qualityControlLevelCode = "2";
            s.qualityControlLevel.qualityControlLevelID = 2;
            s.qualityControlLevel.qualityControlLevelIDSpecified = true;


            if (variableId == 8)
            {
                s.sampleMedium = "Precipitation";
                s.dataType = "Incremental";
            }
            else
            {
                s.sampleMedium = "Air";
            }
            s.source = new SourceType();


            s.source.citation = "GHCN Global Station Network";
            s.source.organization = "GHCN";
            s.source.sourceCode = "1";
            if (variableId == 8)
                s.source.sourceDescription = "GHCN Global Station Network";
            else
                s.source.sourceDescription = "GHCN Global Station Network";
            s.source.sourceID = 1;
            s.source.sourceIDSpecified = true;
            

            //value count, begin time, end time
            string qrySeriesCatalog = "SELECT BeginDateTime, EndDateTime, ValueCount FROM DataSeries INNER JOIN Sites ON DataSeries.SiteID = Sites.SiteID WHERE SiteCode = '{0}' AND VariableID = {1}";
            string sql = string.Format(qrySeriesCatalog, siteCode, variableId);
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        dr.Read();

                        s.valueCount = new seriesCatalogTypeSeriesValueCount();
                        s.valueCount.Value = Convert.ToInt32(dr["ValueCount"]);
                        s.valueType = "Field Observation";

                        s.variableTimeInterval = new TimeIntervalType();
                        s.variableTimeInterval.beginDateTime = Convert.ToDateTime(dr["BeginDateTime"]);
                        s.variableTimeInterval.beginDateTimeUTC = s.variableTimeInterval.beginDateTime.AddHours(-1);
                        s.variableTimeInterval.beginDateTimeUTCSpecified = true;

                        s.variableTimeInterval.endDateTime = Convert.ToDateTime(dr["EndDateTime"]);
                        s.variableTimeInterval.endDateTimeUTC = s.variableTimeInterval.endDateTime.AddHours(-1);
                        s.variableTimeInterval.endDateTimeUTCSpecified = true;
                    }
                }

            }

            //variable
            s.variable = GetVariableInfoFromDb("GHCN:" + variableId);
            return s;
        }

        public static VariableInfoType[] GetVariablesFromDb()
        {
            List<VariableInfoType> variablesList = new List<VariableInfoType>();

            string cnn = GetConnectionString();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sqlVariables = "select VariableID, VariableCode, VariableName from Variables WHERE VariableID IN (3, 4, 8)";
                    cmd.CommandText = sqlVariables;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        VariableInfoType varInfo = new VariableInfoType();

                        int var_id = Convert.ToInt32(dr["VariableID"]);

                        if (var_id == 8)
                        {
                            varInfo.dataType = "Incremental";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Precipitation";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "mm";
                            varInfo.unit.unitCode = "54";
                            varInfo.unit.unitDescription = "millimeter";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "millimeter";
                            varInfo.unit.unitType = "Length";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Precipitation";
                        }
                        else if (var_id == 3)
                        {
                            varInfo.dataType = "Minimum";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Air";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "degC";
                            varInfo.unit.unitCode = "96";
                            varInfo.unit.unitDescription = "degree celsius";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "degree celsius";
                            varInfo.unit.unitType = "Temperature";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Temperature";
                        }
                        else if (var_id == 4)
                        {
                            varInfo.dataType = "Maximum";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Air";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "degC";
                            varInfo.unit.unitCode = "96";
                            varInfo.unit.unitDescription = "degree celsius";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "degree celsius";
                            varInfo.unit.unitType = "Temperature";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Temperature";
                        }
                        else
                        {
                            varInfo.dataType = "Continuous";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Air";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "cm";
                            varInfo.unit.unitCode = "47";
                            varInfo.unit.unitDescription = "centimeter";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "centimeter";
                            varInfo.unit.unitType = "Length";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Snow depth";
                        }
                        
                        variablesList.Add(varInfo);
                    }
                }
            }
            return variablesList.ToArray();
        }

        internal static VariableInfoType GetVariableInfoFromDb(string VariableParameter)
        {
            VariableInfoType varInfo = new VariableInfoType();
            string cnn = GetConnectionString();
            string variableId = VariableParameter.Substring(VariableParameter.LastIndexOf(":") + 1);

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sqlVariables = string.Format(
                        "select VariableID, VariableCode, VariableName from variables where VariableID={0}", variableId);
                    cmd.CommandText = sqlVariables;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.HasRows)
                    {
                        dr.Read();

                        int var_id = Convert.ToInt32(dr["VariableID"]);
                        if (var_id == 8)
                        {
                            varInfo.dataType = "Incremental";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Precipitation";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "mm";
                            varInfo.unit.unitCode = "54";
                            varInfo.unit.unitDescription = "millimeter";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "millimeter";
                            varInfo.unit.unitType = "Length";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Precipitation";
                        }
                        else if (var_id == 3)
                        {
                            varInfo.dataType = "Minimum";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Air";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "degC";
                            varInfo.unit.unitCode = "96";
                            varInfo.unit.unitDescription = "degree celsius";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "degree celsius";
                            varInfo.unit.unitType = "Temperature";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Temperature";
                        }
                        else if (var_id == 4)
                        {
                            varInfo.dataType = "Maximum";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Air";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "degC";
                            varInfo.unit.unitCode = "96";
                            varInfo.unit.unitDescription = "degree celsius";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "degree celsius";
                            varInfo.unit.unitType = "Temperature";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Temperature";
                        }
                        else
                        {
                            varInfo.dataType = "Continuous";
                            varInfo.generalCategory = "Climate";
                            varInfo.metadataTimeSpecified = false;
                            varInfo.noDataValue = -9999.0;
                            varInfo.noDataValueSpecified = true;
                            varInfo.sampleMedium = "Air";
                            varInfo.speciation = "Not Applicable";

                            //time support and time unit
                            varInfo.timeScale = new VariableInfoTypeTimeScale();
                            varInfo.timeScale.isRegular = true;
                            varInfo.timeScale.timeSpacingSpecified = false;
                            varInfo.timeScale.timeSupport = 0.0f;
                            varInfo.timeScale.timeSupportSpecified = true;
                            varInfo.timeScale.unit = new UnitsType();
                            varInfo.timeScale.unit.unitAbbreviation = "d";
                            varInfo.timeScale.unit.unitCode = "104";
                            varInfo.timeScale.unit.unitID = 0;
                            varInfo.timeScale.unit.unitName = "day";
                            varInfo.timeScale.unit.unitType = "Time";

                            //variable unit
                            varInfo.unit = new UnitsType();
                            varInfo.unit.unitAbbreviation = "cm";
                            varInfo.unit.unitCode = "47";
                            varInfo.unit.unitDescription = "centimeter";
                            varInfo.unit.unitID = 1;
                            varInfo.unit.unitIDSpecified = true;
                            varInfo.unit.unitName = "centimeter";
                            varInfo.unit.unitType = "Length";

                            //variable code
                            varInfo.valueType = "Field Observation";

                            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                            varInfo.variableCode[0].@default = true;
                            varInfo.variableCode[0].defaultSpecified = true;
                            varInfo.variableCode[0].Value = Convert.ToString(dr["VariableID"]);
                            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                            varInfo.variableCode[0].variableID = Convert.ToInt32(dr["VariableID"]);

                            varInfo.variableName = "Snow depth";
                        }
                    }
                }
            }
            return varInfo;
        }

        public static SiteInfoType GetSiteFromDb2(string siteCode)
        {
            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];
            SiteInfoType si = new SiteInfoType();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string qrySiteByCode = "SELECT SiteID, SiteCode, SiteName, Latitude, Longitude, Elevation FROM Sites WHERE SiteCode='{0}'";
                    string sqlSite = string.Format(qrySiteByCode, siteCode);

                    cmd.CommandText = sqlSite;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    dr.Read();
                    if (dr.HasRows)
                    {       
                        si.geoLocation = new SiteInfoTypeGeoLocation();
                        LatLonPointType latLon = new LatLonPointType();
                        latLon.latitude = Convert.ToDouble(dr["Latitude"]);
                        latLon.longitude = Convert.ToDouble(dr["Longitude"]);
                        latLon.srs = "EPSG:4326";
                        si.geoLocation.geogLocation = latLon;
                        si.siteCode = new SiteInfoTypeSiteCode[1];
                        si.siteCode[0] = new SiteInfoTypeSiteCode();
                        si.siteCode[0].network = serviceCode;
                        si.siteCode[0].siteIDSpecified = false;
                        si.siteCode[0].Value = Convert.ToString(dr["SiteCode"]);
                        si.siteName = Convert.ToString(dr["SiteName"]);
                        si.elevation_m = Convert.ToDouble(dr["Elevation"]);
                    }
                }
            }
            return si;
        }

        public static SiteInfoResponseTypeSite GetSiteFromDb(string siteCode, bool includeSeriesCatalog)
        {
            SiteInfoResponseTypeSite newSite = new SiteInfoResponseTypeSite();

            newSite.siteInfo = GetSiteFromDb2(siteCode);

            //to add the catalog
            if (includeSeriesCatalog)
            {
                List<int> variableIdList = GetVariablesForSite(siteCode);
                int numVariables = variableIdList.Count;
                
                //how many series ???
                List<seriesCatalogTypeSeries> listOfSeriesCatalogs = new List<seriesCatalogTypeSeries>();
                foreach(int variableID in variableIdList)
                {
                    seriesCatalogTypeSeries catalog = GetSeriesCatalogFromDb(siteCode, variableID);
                    if (catalog != null)
                    {
                        if (catalog.variableTimeInterval != null)
                        {
                            listOfSeriesCatalogs.Add(catalog);
                        }
                    }
                }
                
                newSite.seriesCatalog = new seriesCatalogType[1];
                newSite.seriesCatalog[0] = new seriesCatalogType();
                newSite.seriesCatalog[0].series = new seriesCatalogTypeSeries[listOfSeriesCatalogs.Count];
                int numSeries = listOfSeriesCatalogs.Count;

                for (int i = 0; i < numVariables; i++)
                {
                    newSite.seriesCatalog[0].series[i] = listOfSeriesCatalogs[i];
                }
            }

            return newSite;
        }

        private static List<int> GetVariablesForSite(string siteCode)
        {
            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];
            if (siteCode.StartsWith(serviceCode))
            {
                siteCode = siteCode.Substring(serviceCode.Length);
            }
            List<int> variableIdList = new List<int>();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = "SELECT VariableID FROM DataSeries " +
                        "INNER JOIN Sites ON DataSeries.SiteID = Sites.SiteID WHERE Sites.SiteCode=@siteCode";

                    cmd.CommandText = sql;
                    cmd.Parameters.Add(new SqlParameter("@siteCode", siteCode));
                    cmd.Connection = conn;
                    conn.Open();
                    
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        if (dr.HasRows)
                        {
                            int varId = Convert.ToInt32(dr["VariableID"]);
                            if (!variableIdList.Contains(varId))
                                variableIdList.Add(varId);
                        }
                    }
                }
            }
            return variableIdList;
        }

        /// <summary>
        /// Get the values, from the Db
        /// </summary>
        /// <param name="siteId">site id (local database id)</param>
        /// <param name="variableId">variable id (local database id)</param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        internal static TsValuesSingleVariableType GetValuesFromDb(string siteCode, string variableId, DateTime startDateTime, DateTime endDateTime)
        {
            //to get values, from the db
            TsValuesSingleVariableType s = new TsValuesSingleVariableType();
            s.censorCode = new CensorCodeType[1];
            s.censorCode[0] = new CensorCodeType();
            s.censorCode[0].censorCode = "nc";
            s.censorCode[0].censorCodeDescription = "not censored";
            s.censorCode[0].censorCodeID = 1;
            s.censorCode[0].censorCodeIDSpecified = true;

            //method
            s.method = new MethodType[1];
            s.method[0] = new MethodType();
            int varId = Convert.ToInt32(variableId);

            s.method[0].methodCode = "2";
            s.method[0].methodID = 2;
            s.method[0].methodDescription = "Measured at professional climate stations";
            s.method[0].methodLink = "http://climexp.knmi.nl";

            //qc level
            s.qualityControlLevel = new QualityControlLevelType[1];
            s.qualityControlLevel[0] = new QualityControlLevelType();
            s.qualityControlLevel[0].definition = "raw data";
            s.qualityControlLevel[0].explanation = "raw data";
            s.qualityControlLevel[0].qualityControlLevelCode = "1";
            s.qualityControlLevel[0].qualityControlLevelID = 1;
            s.qualityControlLevel[0].qualityControlLevelIDSpecified = true;

            //source
            s.source = new SourceType[1];
            s.source[0] = new SourceType();
            s.source[0].citation = @"Peterson, Thomas C. and Russell S. Vose (1997). ""An overview of the Global Historical Climatology Network temperature data base"". Bulletin of the American Meteorological Society 78 (12): 2837–2849.";
            s.source[0].organization = "GHCN";
            s.source[0].sourceCode = "1";
            s.source[0].sourceDescription = " GHCN - Global Historical Climate Network";
            s.source[0].sourceID = 1;
            s.source[0].sourceIDSpecified = true;
            //s.source[0].sourceLink[0] = @"http://climexp.knmi.nl";

            //time units
            s.units = new UnitsType();
            s.units.unitAbbreviation = "d";
            s.units.unitCode = "d";
            s.units.unitID = 0;
            s.units.unitName = "day";
            s.units.unitType = "Time";

            //values: get from knmi
            EcaVisitor vis = new EcaVisitor();

            string ghcnVariableCode = "gdcnprcp";
            string shortCode = "PR";
            int vId = Convert.ToInt32(variableId);
            switch (vId)
            {
                case 3:
                    ghcnVariableCode = "gdcntmin";
                    shortCode = "TN";
                    break;
                case 4:
                    ghcnVariableCode = "gdcntmax";
                    shortCode = "TX";
                    break;
                case 8:
                    ghcnVariableCode = "gdcnprcp";
                    shortCode = "PR";
                    break;
                case 9:
                    ghcnVariableCode = "gdcnsnwd";
                    shortCode = "SN";
                    break;
            }

            //check for the data file
            string baseDir = "J:\\";
            try
            {
                baseDir = HttpContext.Current.Server.MapPath("/ghcn_files");
            }
            catch(Exception ex)
            {
                baseDir = @"D:\Websites\448cf9624b\www\ghcn_files\";
                if (!Directory.Exists(baseDir))
                {
                    baseDir = "J:\\dev\\ghcn_files\\";
                }
            }
            string dataFileName = Path.Combine(baseDir,  siteCode + "_" + shortCode + ".dat");
      
            if (File.Exists(dataFileName))
            {
                s.value = GhcnBinaryFileReader.ReadFromBinaryFile(dataFileName, startDateTime, endDateTime);
            }
            else
            {
                EcaTimeValue[] vals = vis.GetGhcnDataValues(siteCode, ghcnVariableCode, startDateTime, endDateTime);

                string methodCode = "1";
                string methodID = "1";
                List<ValueSingleVariable> valuesList = new List<ValueSingleVariable>();
                foreach (EcaTimeValue val in vals)
                {
                    ValueSingleVariable v = new ValueSingleVariable();
                    v.censorCode = "nc";
                    v.dateTime = val.DateTimeUtc;
                    v.dateTimeUTC = v.dateTime;
                    v.dateTimeUTCSpecified = true;
                    v.methodCode = methodCode;
                    v.methodID = methodID;
                    v.offsetValueSpecified = false;
                    v.qualityControlLevelCode = "1";
                    v.sourceCode = "1";
                    v.sourceID = "1";
                    v.timeOffset = "00:00";
                    v.Value = Convert.ToDecimal(val.DataValue);
                    valuesList.Add(v);
                }

                //convert list to array
                DateTime beginDate = valuesList[0].dateTime;
                DateTime endDate = valuesList[valuesList.Count - 1].dateTime;
                int numDays = endDate.Subtract(beginDate).Days;
                ValueSingleVariable[] valuesArray = new ValueSingleVariable[numDays];
                int valueIndex = 0;

                DateTime curDate = beginDate;
                foreach (ValueSingleVariable val in valuesList)
                {
                    if (valueIndex >= valuesArray.Length) break;

                    while (curDate < val.dateTime)
                    {
                        valuesArray[valueIndex] = CreateNoDataValue(curDate);
                        curDate = curDate.AddDays(1);
                        valueIndex++;
                    }

                    if (valueIndex >= valuesArray.Length) break;
                    valuesArray[valueIndex] = val;
                    curDate = val.dateTime.AddDays(1);
                    valueIndex++;
                }
                s.value = valuesArray;
            }
            
            return s;
        }

        private static Decimal convertValue(object val, int varId) {
            var dVal = Convert.ToDouble(val);
            if (varId == 1)
            {
                return (dVal >= 0) ? Convert.ToDecimal(dVal * 0.1) : Convert.ToDecimal(-9999.0);
            }
            else
            {
                return dVal >= 0 ? Convert.ToDecimal(dVal) : Convert.ToDecimal(0.1);
            }
        }

        private static ValueSingleVariable CreateNoDataValue(DateTime time)
        {
            ValueSingleVariable v = new ValueSingleVariable();
            v.censorCode = "nc";
            v.dateTime = Convert.ToDateTime(time);
            v.dateTimeUTC = v.dateTime.AddHours(-1);
            v.dateTimeUTCSpecified = true;
            v.methodCode = "1";
            v.methodID = "1";
            v.offsetValueSpecified = false;
            v.qualityControlLevelCode = "1";
            v.sourceCode = "1";
            v.sourceID = "1";
            v.timeOffset = "01:00";
            v.Value = 0.0M;
            return v;
        }
    }
}

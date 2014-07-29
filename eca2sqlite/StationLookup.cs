using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HydroDesktop.Interfaces.ObjectModel;
using System.IO;

namespace eca2sqlite
{
    /// <summary>
    /// Lookup for stations
    /// </summary>
    public class StationLookup
    {
        private Dictionary<long, Site> lookup = new Dictionary<long, Site>();
        
        public void CreateStationLookup(string stationsFileName)
        {
            if (!File.Exists(stationsFileName)) 
                throw new ArgumentException("Station file doesn't exist");

            string line = String.Empty;
            int startLine = 20;
            using (StreamReader r = new StreamReader(stationsFileName))
            {
                int counter = 0;
                while ((line = r.ReadLine()) != null)
                {
                    counter++;
                    if (counter >= startLine)
                    {
                        Site s = ParseSite(line);
                        lookup.Add(s.Id, s);
                    }
                }
            }
        }

        public Site FindSiteByID(int siteID)
        {
            return lookup[siteID];
        }

        private Site ParseSite(string line)
        {
            if (line.Length < 76)
                throw new ArgumentException("insufficient characters in line. line must have 76 characters.");
            
            int staid = int.Parse(line.Substring(0,5));
            string staname = line.Substring(6, 39).Trim();
            string country = line.Substring(47, 2);
            double lat = ParseLat(line.Substring(50, 9));
            double lon = ParseLon(line.Substring(60, 10));
            int elev = ParseElev(line.Substring(71, 5));

            Site s = new Site();
            s.Latitude = lat;
            s.Longitude = lon;
            s.SpatialReference = new SpatialReference(4326);
            s.SpatialReference.SRSName = "WGS1984";
            s.PosAccuracy_m = 30.0;
            s.Name = staname;
            s.Code = "ECA:" + staid.ToString("0000");
            s.Elevation_m = elev;
            s.Id = staid;
            s.County = country;
            return s;
        }

        private double ParseLat(string latStr)
        {
            int hem = (latStr.Substring(0, 1) == "-") ? -1 : 1;
            int deg = int.Parse(latStr.Substring(1, 2));
            int min = int.Parse(latStr.Substring(4, 2));
            int sec = int.Parse(latStr.Substring(7, 2));

            return hem * (deg + (float)min / 60.0f + (float)sec / 3600.0f);
        }

        private double ParseLon(string lonStr)
        {
            int hem = (lonStr.Substring(0, 1) == "-") ? -1 : 1;
            int deg = int.Parse(lonStr.Substring(1, 3));
            int min = int.Parse(lonStr.Substring(5, 2));
            int sec = int.Parse(lonStr.Substring(8, 2));

            return hem * (deg + (float)min / 60.0f + (float)sec / 3600.0f);
        }

        private int ParseElev(string elev)
        {
            return int.Parse(elev.Trim());
        }
    }
}

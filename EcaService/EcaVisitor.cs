using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Globalization;

namespace EcaService
{
    public class EcaVisitor
    {
        /// <summary>
        /// Retrieves the ECA-D data file by calling the knmi climate
        /// explorer file
        /// </summary>
        /// <param name="quantity"></param>
        /// <param name="wmoID"></param>
        /// <returns></returns>
        public EcaTimeValue[] GetEcaDataValues(string quantity, long wmoID)
        {
            List<EcaTimeValue> dataValuesList = new List<EcaTimeValue>();
            
            //set-up of the user agent
            string defaultUserAgent = @"Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)";
            
            //(1) We call the initial page
            string metadataUri = String.Format(@"http://climexp.knmi.nl/eca{0}.cgi?id=someone@somewhere&WMO={1}", "prcp",wmoID);
            string responseOne = String.Empty;
            string responseTwo = String.Empty;
            HttpWebRequest requestOne = (HttpWebRequest)HttpWebRequest.Create(metadataUri);
            
            requestOne.UserAgent = defaultUserAgent;
            requestOne.Referer = @"http://climexp.knmi.nl/getstations.cgi";
            using (HttpWebResponse resp = (HttpWebResponse)requestOne.GetResponse())
            {
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                {
                    responseOne = r.ReadToEnd();
                }
            }
            if (String.IsNullOrEmpty(responseOne))
                throw new WebException();

            //(2) We call the data file
            string dataFileUri = String.Format(@"http://climexp.knmi.nl/data/{0}eca{1}.dat", "p", wmoID);

            HttpWebRequest requestTwo = (HttpWebRequest)HttpWebRequest.Create(dataFileUri);
            requestTwo.UserAgent = defaultUserAgent;
            requestTwo.Referer = metadataUri;

            string line;

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)requestTwo.GetResponse())
                {
                    using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                    {
                        while ((line = r.ReadLine()) != null)
                        {
                            if (line.StartsWith("#")) continue;
                            string[] fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            int yr = int.Parse(fields[0]);
                            int mo = int.Parse(fields[1]);
                            int da = int.Parse(fields[2]);
                            double val = double.Parse(fields[3], CultureInfo.InvariantCulture);
                            dataValuesList.Add(new EcaTimeValue { DateTimeUtc = new DateTime(yr, mo, da), DataValue = val });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(500);
                using (HttpWebResponse resp = (HttpWebResponse)requestTwo.GetResponse())
                {
                    using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                    {
                        string ret = r.ReadToEnd();
                    }

                }
            }

            return dataValuesList.ToArray();
        }


        public EcaSite[] GetSitesInBox(double xmin, double xmax, double ymin, double ymax) 
        {
            List<EcaSite> sitesList = new List<EcaSite>();

            //set-up of the user agent
            string defaultUserAgent = @"Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)";

            //(1) We call the initial page
            string datatype = "gdcnprcp";
            string program = "gdcnprcp";
            string xminstr = xmin.ToString("0.000", CultureInfo.InvariantCulture);
            string xmaxstr = xmax.ToString("0.000", CultureInfo.InvariantCulture);
            string yminstr = ymin.ToString("0.000", CultureInfo.InvariantCulture);
            string ymaxstr = ymax.ToString("0.000", CultureInfo.InvariantCulture);

            //create the request data
            string requestData = string.Format(
                @"email=someone%40somewhere&climate=gdcnprcp&name=&num=10&lat=&lon=&lat1={0}&lat2={1}&lon1={2}&lon2={3}" +
                @"&list=%23+lon1+lon2+lat1+lat2+%28optional%29%0D%0Astation+number+%28one+per+line%29&min=10&dist=&elevmin=&elevmax=",
                yminstr, ymaxstr, xminstr, xmaxstr);

            string getStationsUri = @"http://climexp.knmi.nl/getstations.cgi";

            HttpWebRequest requestOne = (HttpWebRequest)HttpWebRequest.Create(getStationsUri);
            requestOne.Method = "POST";
            // Create POST data and convert it to a byte array.
            string postData = requestData;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
	 
	        // Set the ContentType property of the WebRequest.
            requestOne.ContentType = "application/x-www-form-urlencoded";
	 
	        // Set the ContentLength property of the WebRequest.
	        requestOne.ContentLength = byteArray.Length;
 
	        // Get the request stream.
	        var dataStream = requestOne.GetRequestStream();
	 
	        // Write the data to the request stream.
	        dataStream.Write(byteArray, 0, byteArray.Length);
	 
	        // Close the Stream object.
	        dataStream.Close();

            string responseOne = String.Empty;
            string responseTwo = String.Empty;

            requestOne.UserAgent = defaultUserAgent;
            requestOne.Referer = @"http://climexp.knmi.nl/getstations.cgi";
            using (HttpWebResponse resp = (HttpWebResponse)requestOne.GetResponse())
            {
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                {
                    responseOne = r.ReadToEnd();
                }
            }
            if (String.IsNullOrEmpty(responseOne))
                throw new WebException();


            //(2) We call the data kml file
            string dataFileUri = String.Format(
@"http://climexp.knmi.nl/box2kml.cgi?id=someone@somewhere&climate={0}&prog={1}&listname=data/list_gdcn{2}_{3}:{4}_{5}:{6}_10___:__.txt",
datatype, program, "prcp", xminstr, xmaxstr, yminstr, ymaxstr);

            HttpWebRequest requestTwo = (HttpWebRequest)HttpWebRequest.Create(dataFileUri);
            requestTwo.UserAgent = defaultUserAgent;
            requestTwo.Referer = getStationsUri;

            string line;

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)requestTwo.GetResponse())
                {
                    using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                    {
                        while ((line = r.ReadLine()) != null)
                        {
                            if (line.StartsWith("#")) continue;
                            //string[] fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            //int yr = int.Parse(fields[0]);
                            //int mo = int.Parse(fields[1]);
                            //int da = int.Parse(fields[2]);
                            //double val = double.Parse(fields[3], CultureInfo.InvariantCulture);
                            //dataValuesList.Add(new EcaTimeValue { DateTimeUtc = new DateTime(yr, mo, da), DataValue = val });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(500);
                using (HttpWebResponse resp = (HttpWebResponse)requestTwo.GetResponse())
                {
                    using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                    {
                        string ret = r.ReadToEnd();
                    }

                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the ECA-D data file by calling the knmi climate
        /// explorer file
        /// </summary>
        /// <param name="quantity"></param>
        /// <param name="wmoID"></param>
        /// <returns></returns>
        public EcaTimeValue[] GetGhcnDataValues(string ghcnCode, string ghcnVariable) {
            List<EcaTimeValue> dataValuesList = new List<EcaTimeValue>();

            //set-up of the user agent
            string defaultUserAgent = @"Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)";

            //(1) We call the initial page
            string ghcnShortCode = ghcnVariable.Substring(4);
            if (ghcnShortCode == "prcp")
                ghcnShortCode = "prcpall";

            string metadataUri = String.Format(@"http://climexp.knmi.nl/gdcn{0}.cgi?id=someone@somewhere&WMO={1}", ghcnShortCode, ghcnCode);
            string responseOne = String.Empty;
            string responseTwo = String.Empty;
            HttpWebRequest requestOne = (HttpWebRequest)HttpWebRequest.Create(metadataUri);

            requestOne.UserAgent = defaultUserAgent;
            requestOne.Referer = @"http://climexp.knmi.nl/getstations.cgi";
            using (HttpWebResponse resp = (HttpWebResponse)requestOne.GetResponse())
            {
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                {
                    responseOne = r.ReadToEnd();
                }
            }
            if (String.IsNullOrEmpty(responseOne))
                throw new WebException();

            //(2) We call the data file
            string dataFileUri = String.Format(@"http://climexp.knmi.nl/data/pgdcngts{0}.dat", ghcnCode);

            if (ghcnShortCode == "tmin")
                dataFileUri = String.Format(@"http://climexp.knmi.nl/data/ngdcn{0}.dat", ghcnCode);
            else if (ghcnShortCode == "tmax")
                dataFileUri = String.Format(@"http://climexp.knmi.nl/data/xgdcn{0}.dat", ghcnCode);
            else if (ghcnShortCode == "snwd")
                dataFileUri = String.Format(@"http://climexp.knmi.nl/data/dgdcn{0}.dat", ghcnCode);

            HttpWebRequest requestTwo = (HttpWebRequest)HttpWebRequest.Create(dataFileUri);
            requestTwo.UserAgent = defaultUserAgent;
            requestTwo.Referer = metadataUri;

            string line;

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)requestTwo.GetResponse())
                {
                    using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                    {
                        while ((line = r.ReadLine()) != null)
                        {
                            if (line.StartsWith("#")) continue;
                            string[] fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            int yr = int.Parse(fields[0]);
                            int mo = int.Parse(fields[1]);
                            int da = int.Parse(fields[2]);
                            double val = double.Parse(fields[3], CultureInfo.InvariantCulture);
                            dataValuesList.Add(new EcaTimeValue { DateTimeUtc = new DateTime(yr, mo, da), DataValue = val });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(500);
                using (HttpWebResponse resp = (HttpWebResponse)requestTwo.GetResponse())
                {
                    using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                    {
                        string ret = r.ReadToEnd();
                    }

                }
            }

            return dataValuesList.ToArray();
        }
    }
}

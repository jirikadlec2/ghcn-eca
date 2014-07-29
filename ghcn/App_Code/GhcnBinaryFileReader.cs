using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WaterOneFlow.Schema.v1_1;
using System.IO;

/// <summary>
/// Summary description for GhcnBinaryFileReader
/// </summary>
public class GhcnBinaryFileReader
{

	public static ValueSingleVariable[] ReadFromBinaryFile(string fileName, DateTime startTime, DateTime endTime)
    {
        TimeSpan ts = endTime - startTime;
        int totalDays = (int)ts.TotalDays;
        if (startTime.Date == endTime.Date)
        {
            //special case: one day
        }
        else 
        {
            //other cases
            using (FileStream stream = new FileStream(fileName,FileMode.Open))
            {
                //read the startDate
                byte[] startDateBytes = new byte[sizeof(long)];
                stream.Read(startDateBytes, 0, startDateBytes.Length);
                long[] startDateBinary = new long[1];
                Buffer.BlockCopy(startDateBytes, 0, startDateBinary, 0, startDateBytes.Length);
                DateTime startDateFromFile = DateTime.FromBinary(startDateBinary[0]);

                if (startTime < startDateFromFile)
                {
                    startTime = startDateFromFile;
                }

                //find position of query start time
                int startTimePositionDays = (int)((startTime - startDateFromFile).TotalDays);
                if (startTimePositionDays < 0)
                    return null;
                int numDaysInFile = (int)((stream.Length - sizeof(long)) / sizeof(Int16));
                DateTime endDateFromFile = startDateFromFile.AddDays(numDaysInFile);

                if (endTime < startDateFromFile)
                    return null;
                if (startTime > endDateFromFile)
                    return null;                  

                long startTimePositionInBytes = sizeof(long) + startTimePositionDays * sizeof(Int16);
                int numDaysStartEnd = (int)((endTime - startTime).TotalDays);
                long numBytesStartEnd = numDaysStartEnd * sizeof(Int16);
                if (startTimePositionInBytes + numBytesStartEnd > stream.Length)
                {
                    numBytesStartEnd = stream.Length - startTimePositionInBytes;
                    numDaysStartEnd = (int)(numBytesStartEnd / sizeof(Int16));
                }
                long endTimePositionInBytes = startTimePositionInBytes + numBytesStartEnd;

                byte[] resultBytes = new byte[numBytesStartEnd];


                stream.Seek(startTimePositionInBytes, SeekOrigin.Begin);
                stream.Read(resultBytes, 0, resultBytes.Length);

                Int16[] result = new Int16[numDaysStartEnd];
                Buffer.BlockCopy(resultBytes, 0, result, 0, resultBytes.Length);

                ValueSingleVariable[] s = new ValueSingleVariable[result.Length];
                DateTime curTime = startTime;
                string methodCode = "1";
                string methodID = "1";
                int noDataVal = -9999;
                for (int i=0; i< result.Length; i++)
                {
                    s[i] = new ValueSingleVariable();
                    s[i].dateTime = curTime;
                    s[i].Value = result[i] == noDataVal ? Convert.ToDecimal(noDataVal) : Convert.ToDecimal((double)result[i] / 10.0);
                    s[i].censorCode = "nc";
                    s[i].dateTimeUTC = s[i].dateTime;
                    s[i].dateTimeUTCSpecified = true;
                    s[i].methodCode = methodCode;
                    s[i].methodID = methodID;
                    s[i].offsetValueSpecified = false;
                    s[i].qualityControlLevelCode = "1";
                    s[i].sourceCode = "1";
                    s[i].sourceID = "1";
                    s[i].timeOffset = "00:00";
                    curTime = curTime.AddDays(1);
                }
                return s;
            }
        }
        return null;
    }
}
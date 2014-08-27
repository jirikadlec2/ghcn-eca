using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EcaService
{
    public class BinaryFileGenerator
    {
        /// <summary>
        /// Converts the series to a regular-spaced array
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="beginDate"></param>
        /// <returns></returns>
        public Int16[] CheckDataValues(EcaTimeValue[] vals, out DateTime beginDate)
        {
            beginDate = vals[0].DateTimeUtc;
            DateTime endDate = vals[vals.Length-1].DateTimeUtc;
            TimeSpan totalTimeSpan = endDate - beginDate;
            int totalDays = (int)totalTimeSpan.TotalDays;
            Int16[] result = new Int16[totalDays];
            Int16 noDataVal = -9999;
            DateTime curDate = beginDate;
            DateTime prevDate = beginDate.AddDays(-1);

            int dayIndex = 0;
            foreach (EcaTimeValue v in vals)
            {
                DateTime calcDate = prevDate.AddDays(1);
                while (calcDate < v.DateTimeUtc.Date)
                {
                    result[dayIndex] = noDataVal;
                    prevDate = curDate;
                    curDate = curDate.AddDays(1);
                    calcDate = curDate;
                    dayIndex++;
                }

                if (dayIndex >= result.Length)
                    break;

                result[dayIndex] = (Int16)(Math.Round(v.DataValue * 10));
                prevDate = curDate;
                curDate = curDate.AddDays(1);
                dayIndex++;  
            }
            return result;
        }
        
        public void SaveToBinaryFile(EcaTimeValue[] vals, string siteCode, EcaVariable ghcnVariable, string folder)
        {
            //the begin date
            DateTime beginDate = vals[0].DateTimeUtc;
            int valueCount = vals.Length;
            string fileName = Path.Combine(folder, string.Format("{0}_{1}.dat", ghcnVariable.VariableCode, siteCode));
            using(FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                //write the data values as an array of integers
                DateTime firstDate;
                Int16[] valuesArray = CheckDataValues(vals, out firstDate);
                long binaryStartDate = beginDate.ToBinary();
                byte[] startDateBytes = new byte[sizeof(long)];
                System.Buffer.BlockCopy(new long[] { binaryStartDate }, 0, startDateBytes, 0, startDateBytes.Length);
                
                byte[] bytesOriginal = new byte[valuesArray.Length * sizeof(Int16)];
                System.Buffer.BlockCopy(valuesArray, 0, bytesOriginal, 0, bytesOriginal.Length);

                fs.Write(startDateBytes, 0, startDateBytes.Length);
                fs.Write(bytesOriginal, 0, bytesOriginal.Length);
                fs.Flush();
            }

        }
    }
}

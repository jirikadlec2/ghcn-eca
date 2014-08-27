using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EcaService
{
    class Program
    {
        static void Main(string[] args)
        {
            //EcaTimeValue[] values = new EcaVisitor().GetEcaDataValues("precip", 1);
            //new EcaSiteUpdater().GetEcaSiteInformation();
            //new EcaSiteUpdater().SaveSiteInformationToDatabase();

            new EcaSiteUpdater().SaveGhcnSiteInformationToDatabase(GhcnVariableTypes.snow(), 
@"C:\dev\github\ghcn-eca\EcaService\data_files\ghcn_global_snow.txt",
@"C:\dev\ghcn_data");

            //List<EcaSite> siteList = new EcaSiteUpdater().GetSiteListFromTextFile();

            //EcaSite[] sites = new EcaVisitor().GetSitesInBox(-120, -110, 40, 50);

            //EcaTimeValue[] values = new EcaVisitor().GetGhcnDataValues("USC00100227");
        }
    }
}

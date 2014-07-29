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

            new EcaSiteUpdater().SaveGhcnSiteInformationToDatabase(GhcnVariableTypes.tmax(), "ghcn_usa_tmax.txt");

            //List<EcaSite> siteList = new EcaSiteUpdater().GetSiteListFromTextFile();

            //EcaSite[] sites = new EcaVisitor().GetSitesInBox(-120, -110, 40, 50);

            //EcaTimeValue[] values = new EcaVisitor().GetGhcnDataValues("USC00100227");
        }
    }
}

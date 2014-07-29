using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HydroDesktop.Interfaces.ObjectModel;
using HydroDesktop.Database;
using HydroDesktop.Interfaces;

namespace eca2sqlite
{
    class Program
    {
        static void Main(string[] args)
        {
            EcaDbCreator cr = new EcaDbCreator("E:\\dev\\eca\\eca_vyber.sqlite");

            cr.ExportStations();
        }
    }
}

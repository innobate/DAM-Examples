﻿using DAM;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace ConsoleApp_Core_2
{
    class Program
    {
        static void Main(string[] args)
        {
            var connection = System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (var dbManager = new DataAccessManager(SqlClientFactory.Instance, connection))
            {
                var countries = dbManager.Select<Country>("select * from Country");

                foreach (var country in countries)
                {
                    Console.WriteLine("Country: {0}  - Alpha-2: {1}   - Alpha-3: {2}", country.name, country.alpha2, country.alpha3);
                }

                Console.ReadLine();

                Console.WriteLine("Country Table Exists: {0}", dbManager.TableExists("Country", false));

                Console.WriteLine("Country Table Exists: {0}", dbManager.TableExists("[Country]", true));
            }

            Console.ReadLine();


        }
    }



    public class Country
    {
        public double id { get; set; }
        public string name { get; set; }
        public string alpha2 { get; set; }
        public string alpha3 { get; set; }
    }
}

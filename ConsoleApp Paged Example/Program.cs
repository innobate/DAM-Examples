﻿using DAM;
using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ConsoleApp_Paged_Example
{
    class Program
    {
        public static IConfiguration configuration;
        static void Main(string[] args)
        {
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json",true, true)
                .Build();

            var connection = configuration.GetConnectionString("DefaultConnection");

            using (var dbManager = new DataAccessManager(SqlClientFactory.Instance, connection))
            {
                int startPage = 1;
                var pagedCountries = dbManager.Select<Country>("select * from Country", startPage, 10);

                while (pagedCountries.Count > 0)
                {
                    Console.WriteLine("Results from page:-{0}", startPage);
                    foreach (var country in pagedCountries)
                    {
                        Console.WriteLine("Country: {0}  - Alpha-2: {1}   - Alpha-3: {2}", country.name, country.alpha2, country.alpha3);
                    }
                    startPage++;
                    pagedCountries = dbManager.Select<Country>("select * from Country", startPage, 10);
                }

                Console.WriteLine("----End of Paged Results----");
            }

            Console.ReadLine();
        }
    }



    public class Country
    {
        public int id { get; set; }
        public string name { get; set; }
        public string alpha2 { get; set; }
        public string alpha3 { get; set; }
    }

}

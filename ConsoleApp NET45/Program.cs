using DAM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_NET45
{
    class Program
    {
        static void Main(string[] args)
        {
            var connection = System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            var dbManager = new DataAccessManager("System.Data.SqlClient", connection);
            var countries = dbManager.Select<Country>("select * from Country");

            foreach (var country in countries)
            {
                Console.WriteLine("Country: {0}  - Alpha-2: {1}   - Alpha-3: {2}", country.name, country.alpha2, country.alpha3);
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

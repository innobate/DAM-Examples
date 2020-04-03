using DAM;
using System;
using System.Data;
using System.Data.Odbc;
using System.Linq;

namespace ConsoleApp_CSV_Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // Connection string for a Text file
            string ConnectionString = @"Driver={Microsoft Text Driver (*.txt; *.csv)};DBQ=" + AppDomain.CurrentDomain.BaseDirectory + @"world-countries\data\en\";

            using (var dbManager = new DataAccessManager(OdbcFactory.Instance, ConnectionString))
            {
                var x = dbManager.Select<Country>("Select * from world.csv");
                var y = dbManager.Select("Select * from world.csv");
                ShowTable(y);

                var z = dbManager.Select<Country>("Select * from world.csv where alpha2 like 'n%'");

                Country nz = z.AsEnumerable().Where(x => x.name == "New Zealand").FirstOrDefault();
                Console.WriteLine("Name: {0,-24} ISO2: {1}  ISO3: {2}", nz.name, nz.alpha2, nz.alpha3);


                //parameter examples
                var a = dbManager.Select<Country>("Select * from world.csv where alpha2 like ?", new DatabaseParameter("@p", "%a"));
                var b = dbManager.Select<Country>("Select * from world.csv where alpha2 like ? or alpha3 like ?", new DatabaseParameter("@p", "%a"),new DatabaseParameter("@p1", "n%"));

                //paged example
                int startPage = 1;
                var pagedCountries = dbManager.Select<Country>("Select * from world.csv", startPage, 10);

                while (pagedCountries.Count > 0)
                {
                    Console.WriteLine("Results from page:-{0}", startPage);
                    foreach (var country in pagedCountries)
                    {
                        Console.WriteLine("Label: {0,-56}  - ISO2: {1}   - ISO3: {2}", country.name, country.alpha2, country.alpha3);
                    }
                    startPage++;
                    pagedCountries = dbManager.Select<Country>("Select * from world.csv", startPage, 10);
                }
                Console.WriteLine("----End of Paged Results----");

            }
        }

        private static void ShowTable(DataTable table)
        {
            foreach (DataColumn col in table.Columns)
            {
                Console.Write("{0,-24}", col.ColumnName);
            }
            Console.WriteLine();

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    if (col.DataType.Equals(typeof(DateTime)))
                        Console.Write("{0,-24:d}", row[col]);
                    else if (col.DataType.Equals(typeof(Decimal)))
                        Console.Write("{0,-24:C}", row[col]);
                    else
                        Console.Write("{0,-24}", row[col]);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
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

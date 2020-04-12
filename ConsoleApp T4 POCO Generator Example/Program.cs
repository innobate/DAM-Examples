using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp_T4_POCO_Generator_Example.Models;
using DAM;

namespace ConsoleApp_T4_POCO_Generator_Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var connection = "Server=.//;Initial Catalog=AdventureWorks;integrated security=True;MultipleActiveResultSets=true;Connection Timeout=3000;";
            var dbManager = new DataAccessManager("System.Data.SqlClient", connection);

            var customers = dbManager.Select<Customer>("select * from Sales.Customer");

            foreach (var customer in customers)
            {
                Console.WriteLine("ID: {0}  - Account: {1}   - Type {2}", customer.CustomerID, customer.AccountNumber, customer.CustomerType);
            }

            Console.ReadLine();
        }
    }
}

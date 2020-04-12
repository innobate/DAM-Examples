using System;

namespace ConsoleApp_T4_POCO_Generator_Example.Models
{
    /// <summary>
    /// Represents Customer Entity.
    /// NOTE: This class is generated from a T4 template - you should not modify it manually.
    /// </summary>
    public partial class Customer 
    {
        public System.String AccountNumber { get; set; }

        public System.String CustomerType { get; set; }

        public System.Guid rowguid { get; set; }

        public System.DateTime ModifiedDate { get; set; }

        public System.Int32 CustomerID { get; set; }

        public System.Int32? TerritoryID { get; set; }

    }
}      

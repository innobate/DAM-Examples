using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_Example_CSV
{
    public class Country
    {
        public double id { get; set; }
        public string name { get; set; }
        public string alpha2 { get; set; }
        public string alpha3 { get; set; }

        public string Image
        {
            get {
                return AppDomain.CurrentDomain.BaseDirectory + @"world-countries\flags\16x16\" + this.alpha2+".png"; 
            }
        }
    }
}

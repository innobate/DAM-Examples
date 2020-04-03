using DAM;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_Example_CSV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetGridDataSource();
        }

        private void SetGridDataSource()
        {
            // Connection string for a Text file
            string ConnectionString = @"Driver={Microsoft Excel Driver (*.xls)};DriverId=790;DBQ=" + AppDomain.CurrentDomain.BaseDirectory + @"world-countries\data\en\world.xls";

            using (var dbManager = new DataAccessManager(OdbcFactory.Instance, ConnectionString))
            {
                var y = dbManager.Select<Country>("Select * from [world$]");
                dgTest.SetValue(ListView.ItemsSourceProperty, y);
            }
        }
    }
}

using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
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

namespace BuddhaBowls.UserControls
{
    /// <summary>
    /// Interaction logic for CogsSubReportControl.xaml
    /// </summary>
    public partial class CogsSubReportControl : UserControl
    {
        public CogsSubReportControl()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(((DataGrid)sender).SelectedItem != null)
                ((CogsSubReportVM)DataContext).ItemDoubleClicked((IInvEvent)((DataGrid)sender).SelectedItem);
        }
    }
}

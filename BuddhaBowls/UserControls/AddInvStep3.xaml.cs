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
    /// Interaction logic for AddInvStep3.xaml
    /// </summary>
    public partial class AddInvStep3 : UserControl
    {
        public AddInvStep3(NewInventoryItemWizard context)
        {
            InitializeComponent();
            DataContext = context;
        }
    }
}

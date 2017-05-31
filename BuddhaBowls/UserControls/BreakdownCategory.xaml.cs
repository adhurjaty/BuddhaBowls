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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class BreakdownCategory : UserControl
    {
        public BreakdownCategory()
        {
            InitializeComponent();
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            PriceColumn.Binding = new Binding("LastPurchasedPrice") { StringFormat = "c" };
        }

        private void BreakdownGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (BreakdownGrid.SelectedItem != null)
            {
                ((DataGrid)sender).RowEditEnding -= BreakdownGrid_RowEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).Items.Refresh();
                ((DataGrid)sender).RowEditEnding += BreakdownGrid_RowEditEnding;
                ((BreakdownCategoryItem)DataContext).Update();
            }
            PriceColumn.Binding = new Binding("PurchaseExtension") { StringFormat = "c" };
        }
    }
}

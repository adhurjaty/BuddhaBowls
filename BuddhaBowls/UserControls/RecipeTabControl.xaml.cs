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
    /// Interaction logic for RecipeTabControl.xaml
    /// </summary>
    public partial class RecipeTabControl : UserControl
    {
        public RecipeTabControl()
        {
            InitializeComponent();
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((RecipeTabVM)DataContext).FilterInventoryItems(textBox.Text);
        }
    }
}

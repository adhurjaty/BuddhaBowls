﻿using System;
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
    /// Interaction logic for AddVendorStep2.xaml
    /// </summary>
    public partial class AddVendorStep2 : UserControl
    {
        public AddVendorStep2(NewVendorWizardVM context)
        {
            InitializeComponent();
            DataContext = context;
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((NewVendorWizardVM)DataContext).FilterItems(textBox.Text);
        }

    }
}

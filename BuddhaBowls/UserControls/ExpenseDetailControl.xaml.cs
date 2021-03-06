﻿using BuddhaBowls.Models;
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
    /// Interaction logic for ExpenseDetail.xaml
    /// </summary>
    public partial class ExpenseDetailControl : UserControl
    {
        public ExpenseDetailControl()
        {
            InitializeComponent();
        }

        public ExpenseDetailControl(ExpenseDetailVM context) : this()
        {
            DataContext = context;
        }

        private void dataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem != null)
            {
                ((DataGrid)sender).RowEditEnding -= dataGrid_RowEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).RowEditEnding += dataGrid_RowEditEnding;
                ExpenseItem item = (ExpenseItem)e.Row.Item;
                PAndLSummarySection section = (PAndLSummarySection)((DataGrid)sender).DataContext;
                ((ExpenseDetailVM)DataContext).EditedItem(section, item);
            }
        }
    }
}

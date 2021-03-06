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
    /// Interaction logic for ChangeInventoryOrder.xaml
    /// </summary>
    public partial class ChangeInventoryOrder : UserControl
    {
        public ChangeInventoryOrder()
        {
            InitializeComponent();
        }

        public ChangeInventoryOrder(ChangeOrderVM context) : this()
        {
            DataContext = context;
        }

        public void listBox_StartDrag(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = (ListBoxItem)sender;
            DragDrop.DoDragDrop(item, item.DataContext, DragDropEffects.Move);
            item.IsSelected = true;
        }

        public void listBox_EndDrag(object sender, DragEventArgs e)
        {
            string droppedData = (string)e.Data.GetData(typeof(string));
            string target = (string)((ListBoxItem)sender).DataContext;

            ((ChangeOrderVM)DataContext).ReorderNewList(droppedData, target);
        }
    }
}

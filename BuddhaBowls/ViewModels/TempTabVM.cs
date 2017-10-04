using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BuddhaBowls
{
    public class TempTabVM : TabVM
    {
        protected UserControl _tabControl;
        protected UpdateBinding FinishDelegate;

        private string _header;
        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                NotifyPropertyChanged("Header");
            }
        }

        // Stack of temporary tabs adds and removes from the front
        public static List<UserControl> TabStack;

        public TempTabVM() : base()
        {

        }

        public void Add(string header)
        {
            if (_tabControl == null)
                throw new Exception("Must initialize _tabControl to add tab");
            _tabControl.Tag = header;
            if (TabStack == null)
            {
                TabStack = new List<UserControl>();
            }

            if (TabStack.Count == 0)
            {
                TabStack.Add(_tabControl);
                ParentContext.AppendTempTab(_tabControl);
            }
            else
            {
                // if there's already a desired new tab in the tabstack, then remove it
                TabStack.Remove(TabStack.FirstOrDefault(x => (string)x.Tag == header));
                TabStack.Insert(0, _tabControl);
                ParentContext.ReplaceTempTab(_tabControl);
            }
        }

        protected void Close()
        {
            TabStack.RemoveAt(0);

            if(TabStack.Count > 0)
                ParentContext.ReplaceTempTab(TabStack[0]);
            else
                ParentContext.RemoveTempTab();
        }
    }
}

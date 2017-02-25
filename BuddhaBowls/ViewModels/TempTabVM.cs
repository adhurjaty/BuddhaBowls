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
        public string Header;

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

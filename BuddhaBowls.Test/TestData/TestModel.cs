using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Test.TestData
{
    public class TestModel : Model
    {
        public string Col1 { get; set; }
        public string Col2 { get; set; }
        public string Col3 { get; set; }
        public string AnotherCol { get; set; }

        public TestModel()
        {
            _tableName = "TestCopy";
            _dbInt = new DatabaseInterface(@"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddahBowls.Test\Data");
        }

        public TestModel(Dictionary<string, string> fieldParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, fieldParams);

            if (record != null)
            {
                InitializeObject(record);
            }
        }
    }
}

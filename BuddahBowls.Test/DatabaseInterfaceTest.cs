using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Services;

namespace BuddahBowls.Test
{
    [TestClass]
    public class DatabaseInterfaceTest
    {
        private DatabaseInterface _dbInt;

        public DatabaseInterfaceTest()
        {
            _dbInt = new DatabaseInterface();
        }

        [TestMethod]
        public void GetRecordTest()
        {

        }

        [TestMethod]
        public void GetColumnsTest()
        {
            try
            {
                _dbInt.Open("Test");

                string[] columns = _dbInt.GetColumnNames();

                Assert.AreEqual(new string[] { "Col1", "Col2", "Col3", "Another Col" }, columns);
            }
            finally
            {
                _dbInt.Close();
            }
        }
    }
}

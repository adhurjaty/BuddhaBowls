using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Services;
using System.Collections.Generic;
using System.IO;

namespace BuddhaBowls.Test
{
    [TestClass]
    public class DatabaseInterfaceTest
    {
        private DatabaseInterface _dbInt;

        public DatabaseInterfaceTest()
        {
            _dbInt = new DatabaseInterface(@"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddahBowls.Test\Data");
        }

        [TestMethod]
        public void GetRecordTest()
        {
            string[] record = _dbInt.GetRecord("Test", new Dictionary<string, string>() { { "Col2", "7" } });
            string[] refRecord = new string[] { "6", "7", "lksdf", "anil" };

            // skip id field
            for (int i = 1; i < record.Length; i++)
            {
                Assert.AreEqual(refRecord[i-1], record[i]);
            }
        }

        [TestMethod]
        public void GetNoRecordTest()
        {
            string[] record = _dbInt.GetRecord("Test", new Dictionary<string, string>() { { "Col2", "999" } });

            Assert.IsNull(record);
        }

        [TestMethod]
        public void GetMultipleRecordsTest()
        {
            string[][] records = _dbInt.GetRecords("Test", new Dictionary<string, string>() { { "Col2", "7" } });
            string[][] refRecords = new string[][]
            {
                new string[]{ "6", "7", "lksdf", "anil" },
                new string[] { "sdaf", "7", "98",	"hari" }
            };

            for (int i = 0; i < records.Length; i++)
            {
                // skip id field
                for (int j = 1; j < records[0].Length; j++)
                {
                    Assert.AreEqual(refRecords[i][j-1], records[i][j]);
                }
            }
        }

        [TestMethod]
        public void GetAllRecordsTest()
        {
            string[][] records = _dbInt.GetRecords("Test");

            Assert.AreEqual(4, records.Length);
        }

        [TestMethod]
        public void DeleteMultipleRecordsTest()
        {
            string dbFilePath = _dbInt.FilePath("Test");
            string copyFilePath = Util.CopyTable(dbFilePath);

            Dictionary<string, string> mapping = new Dictionary<string, string>() { { "Col2", "7" } };

            bool success = _dbInt.DeleteRecords("TestCopy", mapping);

            Assert.IsTrue(success);
            Assert.IsNull(_dbInt.GetRecords("TestCopy", mapping));

            File.Delete(copyFilePath);
        }

        [TestMethod]
        public void DeleteSingleRecordTest()
        {
            string dbFilePath = _dbInt.FilePath("Test");
            string copyFilePath = Util.CopyTable(dbFilePath);

            Dictionary<string, string> mapping = new Dictionary<string, string>() { { "Col2", "7" } };

            bool success = _dbInt.DeleteRecord("TestCopy", mapping);

            Assert.IsTrue(success);
            Assert.AreEqual(1, _dbInt.GetRecords("TestCopy", mapping).Length);

            File.Delete(copyFilePath);
        }

        [TestMethod]
        public void WriteRecordTest()
        {
            string dbFilePath = _dbInt.FilePath("Test");
            string copyFilePath = Util.CopyTable(dbFilePath);

            try
            {
                Dictionary<string, string> mapping = new Dictionary<string, string>()
                {
                    { "Col1", "Test1" },
                    { "Col2", "Another test value" },
                    { "Col3", "Here's one" },
                    { "AnotherCol", "3912" }
                };

                _dbInt.WriteRecord("TestCopy", mapping);

                string[] record = _dbInt.GetRecord("TestCopy", new Dictionary<string, string>() { { "AnotherCol", "3912" } });
                Assert.AreEqual("Another test value", record[2]);
                Assert.AreEqual("4", record[0]);
            }
            finally
            {
                File.Delete(copyFilePath);
            }
        }

        [TestMethod]
        public void UpdateRecordTest()
        {
            string dbFilePath = _dbInt.FilePath("Test");
            string copyFilePath = Util.CopyTable(dbFilePath);

            int id = 1;
            Dictionary<string, string> newVals = new Dictionary<string, string>()
            {
                { "Col2", "new col2" },
                { "AnotherCol", "new another doe" }
            };

            try
            {
                bool result = _dbInt.UpdateRecord("TestCopy", newVals, id);
                string[] record = _dbInt.GetRecord("TestCopy", new Dictionary<string, string>() { { "Id", id.ToString() } });

                Assert.IsTrue(result);
                Assert.AreEqual(record[4], "new another doe");
            }
            finally
            {
                File.Delete(copyFilePath);
            }
            
        }

        [TestMethod]
        public void GetColumnNamesTest()
        {
            string[] headers = _dbInt.GetColumnNames("Test");

            CollectionAssert.AreEqual(new string[] { "Id", "Col1", "Col2", "Col3", "AnotherCol" }, headers);
        }
    }
}

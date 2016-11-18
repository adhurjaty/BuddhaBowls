using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Services;
using System.Collections.Generic;
using System.IO;

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
            string[] record = _dbInt.GetRecord("Test", new Dictionary<string, string>() { { "Col2", "7" } });
            string[] refRecord = new string[] { "6", "7", "lksdf", "anil" };

            // skip id field
            for (int i = 1; i < record.Length; i++)
            {
                Assert.AreEqual(refRecord[i-1], record[i]);
            }
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
        public void DeleteMultipleRecordsTest()
        {
            string dbFilePath = _dbInt.FilePath("Test");
            string copyFilePath = CopyTable(dbFilePath);

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
            string copyFilePath = CopyTable(dbFilePath);

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
            string copyFilePath = CopyTable(dbFilePath);

            try
            {
                Dictionary<string, string> mapping = new Dictionary<string, string>()
                {
                    { "Col1", "Test1" },
                    { "Col2", "Another test value" },
                    { "Col3", "Here's one" },
                    { "Another Col", "3912" }
                };

                _dbInt.WriteRecord("TestCopy", mapping);

                string[] record = _dbInt.GetRecord("TestCopy", mapping);
                Assert.AreEqual("Another test value", record[2]);
                Assert.AreEqual("4", record[0]);
            }
            finally
            {
                File.Delete(copyFilePath);
            }
        }

        private string CopyTable(string dbFilePath)
        {
            string copyFilePath = dbFilePath.Split('.')[0] + "Copy.csv";

            if(File.Exists(copyFilePath))
            {
                File.Delete(copyFilePath);
            }

            File.Copy(dbFilePath, copyFilePath);

            return copyFilePath;
        }
    }
}

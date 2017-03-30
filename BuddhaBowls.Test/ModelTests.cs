using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Test.TestData;
using System.Collections.Generic;
using System.IO;

namespace BuddhaBowls.Test
{
    [TestClass]
    public class ModelTests
    {
        public ModelTests()
        {

        }

        [TestMethod]
        public void GetModelTest()
        {
            string tableCopy = Util.CopyTable(@"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddahBowls.Test\Data\Test.csv");

            try
            {
                TestModel tModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });

                Assert.AreEqual(1, tModel.Id);
                Assert.AreEqual("lksdf", tModel.Col3);
            }
            finally
            {
                File.Delete(tableCopy);
            }
        }

        [TestMethod]
        public void UpdateModelTest()
        {
            string tableCopy = Util.CopyTable(@"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddahBowls.Test\Data\Test.csv");

            try
            {
                TestModel tModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
                tModel.Col3 = "new Col3 value";
                tModel.Update();

                TestModel newModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
                Assert.AreEqual(tModel.Col3, newModel.Col3);
            }
            finally
            {
                File.Delete(tableCopy);
            }
        }

        [TestMethod]
        public void DeleteModelTest()
        {
            string tableCopy = Util.CopyTable(@"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddahBowls.Test\Data\Test.csv");

            try
            {
                TestModel tModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
                tModel.Destroy();

                TestModel newModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
                Assert.AreEqual(-1, newModel.Id);
            }
            finally
            {
                File.Delete(tableCopy);
            }
        }

        [TestMethod]
        public void InsertModelTest()
        {
            string tableCopy = Util.CopyTable(@"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddahBowls.Test\Data\Test.csv");

            try
            {
                TestModel tModel = new TestModel() { Col1 = "something", Col2 = "wicked", Col3 = "this", AnotherCol = "way" };
                tModel.Insert();

                TestModel refModel = new TestModel(new Dictionary<string, string>() { { "Col2", "wicked" } });
                Assert.AreEqual(4, refModel.Id);
                Assert.AreEqual("this", refModel.Col3);
            }
            finally
            {
                File.Delete(tableCopy);
            }
        }
    }
}

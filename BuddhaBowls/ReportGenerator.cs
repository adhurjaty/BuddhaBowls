using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Excel = Microsoft.Office.Interop.Excel;

namespace BuddhaBowls
{
    public class ReportGenerator
    {
        HashSet<string> _itemCategories;
        Dictionary<string, string> _categoryColors;

        ModelContainer _models;

        Excel.Application _excelApp;
        Excel.Workbook _workbook;
        Excel.Sheets _sheets;

        public ReportGenerator(ModelContainer models)
        {
            _models = models;

            SetInventoryCategories();
            SetCategoryColors();

            _excelApp = new Excel.Application();
            _excelApp.DisplayAlerts = false;
            _workbook = _excelApp.Workbooks.Add();
            _sheets = _workbook.Sheets;
        }

        ~ReportGenerator()
        {
            if(_excelApp != null)
                Close();
        }

        public void Generate()
        {
            
        }

        public void FillInventoryId(string recipeName)
        {
            List<RecipeItem> recipe = _models.BatchItems.First(x => x.Name.ToUpper() == recipeName.ToUpper()).recipe;

            foreach(RecipeItem ri in recipe)
            {
                InventoryItem item = _models.InventoryItems.FirstOrDefault(x => x.Name == ri.Name);
                if (item == null)
                    ri.InventoryItemId = null;
                else
                    ri.InventoryItemId = item.Id;
                ri.Update(recipeName);
            }
        }

        public List<string[]> MakeBatchRecipeTable(string recipeName)
        {
            BatchItem batchItem = _models.BatchItems.First(x => x.Name.ToUpper() == recipeName.ToUpper());
            List<RecipeItem> recipe = batchItem.recipe;
            List<string[]> outList = new List<string[]>();
            Dictionary<string, List<string[]>> categoryDict = new Dictionary<string, List<string[]>>();

            string[] headers = new string[] { "NAME", "MEASURE", "RECIPE UNIT", "# RU", "RU COST", "COST" };

            foreach(RecipeItem item in recipe)
            {
                InventoryItem inv = _models.InventoryItems[(int)item.InventoryItemId];
                if(!categoryDict.Keys.Contains(inv.Category))
                    categoryDict[inv.Category] = new List<string[]>();

                float cost = inv.GetCost();
                float lineCost = cost * item.Quantity;
                categoryDict[inv.Category].Add(new string[] { inv.Name, item.Measure, inv.RecipeUnit,
                                                item.Quantity.ToString(), cost.ToString(), lineCost.ToString() });
            }

            Dictionary<string, float> categoryCosts = _models.GetCategoryCosts(batchItem);
            float batchCost = _models.GetBatchItemCost(batchItem);

            outList.Add(new string[] { recipeName });

            string[] sortedKeys = categoryDict.Keys.ToArray();
            Array.Sort(sortedKeys);
            foreach (string key in sortedKeys)
            {
                string[] headerCopy = new string[headers.Length];
                Array.Copy(headers, headerCopy, headers.Length);
                headerCopy[0] = key;
                outList.Add(headerCopy);

                foreach(string[] row in categoryDict[key])
                {
                    outList.Add(row);
                }

                outList.Add(new string[] { key.ToUpper() + " TOTAL", categoryCosts[key].ToString("c") });
                outList.Add(new string[] { "%", (categoryCosts[key] / batchCost).ToString("p1") });
            }

            outList.Add(new string[] { "BATCH TOTAL", batchCost.ToString("c") });

            return outList;
        }

        public string BatchRecipeReport(List<string[]> contents, string filename)
        {
            int numCols = contents.Max(x => x.Length);
            Excel.Worksheet sheet = _sheets.Add();
            sheet.Name = "Batch Recipe Costing";

            filename = Path.GetFileNameWithoutExtension(filename) + ".xlsx";

            for(int i = 0; i < contents.Count; i++)
            {
                int start = 0;
                bool merged = false;
                if (contents[i].Length < numCols)
                {
                    start = numCols - contents[i].Length;
                    sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, start + 1]].Merge();
                    merged = true;
                }

                for(int j = start; j < numCols; j++)
                {
                    if (merged)
                    {
                        sheet.Cells[i + 1, 1] = contents[i][0];
                        Excel.Range formatRange = sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, start + 1]];
                        if(contents[i].Length == 1)
                            formatRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        else
                            formatRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                    }

                    sheet.Cells[i + 1, j + 1] = contents[i][j - start];
                    merged = false;
                }

                // color the cells if it is a category line
                string category = contents[i][0].Split(' ')[0].ToUpper();
                string key = _itemCategories.FirstOrDefault(x => x.StartsWith(category));
                if (!string.IsNullOrEmpty(key))
                {
                    ((Excel.Range)sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, numCols]]).Interior.Color =
                                    MainHelper.ColorFromString(_categoryColors[key]);
                }

            }

            string filePath = Path.Combine(Properties.Settings.Default.DBLocation, "Reports", filename);
            _workbook.SaveAs(filePath);

            return filePath;
        }

        public bool CreateBatchRecipeReport(string recipeName)
        {
            List<string[]> rows = MakeBatchRecipeTable(recipeName);
            string path = BatchRecipeReport(rows, recipeName + " Report");

            return File.Exists(path);
        }

        public List<string[]> MakeMasterInventoryTable()
        {
            List<string[]> outList = new List<string[]>();

            string[] topHeader = new string[] { "", "AS PURCHASED UNITS", "INVENTORY COUNT UNIT" };
            string[] subHeader = new string[] { "", "Unit", "Unit Price", "Conversion", "Count Unit", "Count Price", "Count No.", "Extension" };

            List<InventoryItem> categorizedInvItems = _models.InventoryItems.OrderBy(x => x.Category).ToList();
            string curCategory = "";
            float categoryCost = 0;

            foreach (InventoryItem item in categorizedInvItems)
            {
                if(item.Category != curCategory)
                {
                    if(curCategory != "")
                    {
                        outList.Add(new string[] { curCategory + " total:", categoryCost.ToString("c") });
                        outList.Add(new string[] { "" });
                        categoryCost = 0;
                    }
                    string[] tempHeader = new string[topHeader.Length];
                    Array.Copy(topHeader, tempHeader, topHeader.Length);
                    tempHeader[0] = item.Category;

                    outList.Add(tempHeader);

                    string[] copyHeader = new string[subHeader.Length];
                    Array.Copy(subHeader, copyHeader, subHeader.Length);
                    copyHeader[0] = item.Category;
                    outList.Add(copyHeader);

                    curCategory = item.Category;
                }

                outList.Add(new string[] { item.Name, item.PurchasedUnit, item.LastPurchasedPrice.ToString("c"), item.Conversion.ToString(),
                                           item.CountUnit, (item.LastPurchasedPrice / item.Conversion).ToString("c"), item.Count.ToString(),
                                           (item.LastPurchasedPrice / item.Conversion * item.Count).ToString("c") });
                categoryCost += item.LastPurchasedPrice / item.Conversion * item.Count;
            }

            return outList;
        }

        public string MasterInventoryReport(List<string[]> contents, string filename)
        {
            int numCols = contents.Max(x => x.Length);
            Excel.Worksheet sheet = _sheets.Add();
            sheet.Name = "Master Invetory List";

            filename = Path.GetFileNameWithoutExtension(filename) + ".xlsx";

            for (int i = 0; i < contents.Count; i++)
            {
                int start = 0;
                bool merged = false;

                if (contents[i].Length == 3)
                {
                    Excel.Range range1 = sheet.Range[sheet.Cells[i + 1, 2], sheet.Cells[i + 1, 3]];
                    Excel.Range range2 = sheet.Range[sheet.Cells[i + 1, 5], sheet.Cells[i + 1, 7]];
                    range1.Merge();
                    range2.Merge();

                    sheet.Cells[i + 1, 2] = contents[i][1];
                    sheet.Cells[i + 1, 5] = contents[i][2];

                    range1.Interior.Color = MainHelper.ColorFromString(_categoryColors[contents[i][0].ToUpper()]);
                    range2.Interior.Color = MainHelper.ColorFromString(_categoryColors[contents[i][0].ToUpper()]);
                }
                else
                {
                    if (string.IsNullOrEmpty(contents[i][0]))
                        continue;
                    if (contents[i].Length < numCols)
                    {
                        start = numCols - contents[i].Length;
                        sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, start + 1]].Merge();
                        merged = true;
                    }

                    for (int j = start; j < numCols; j++)
                    {
                        if (merged)
                        {
                            sheet.Cells[i + 1, 1] = contents[i][0];
                            Excel.Range formatRange = sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, start + 1]];
                            if (contents[i].Length == 1)
                                formatRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                            else
                                formatRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                        }

                        sheet.Cells[i + 1, j + 1] = contents[i][j - start];
                        merged = false;
                    }

                    // color the cells if it is a category line
                    string category = contents[i][0].Split(' ')[0].ToUpper();
                    string key = _itemCategories.FirstOrDefault(x => x.StartsWith(category));
                    if (!string.IsNullOrEmpty(key))
                    {
                        ((Excel.Range)sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, numCols]]).Interior.Color =
                                        MainHelper.ColorFromString(_categoryColors[key]);
                    }
                }

            }

            string filePath = Path.Combine(Properties.Settings.Default.DBLocation, "Reports", filename);
            _workbook.SaveAs(filePath);

            return filePath;
        }

        public bool CreateMasterInventoryReport()
        {
            List<string[]> rows = MakeMasterInventoryTable();
            string path = MasterInventoryReport(rows, "Master Inventory Report");

            return File.Exists(path);
        }

        public void Close()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Marshal.ReleaseComObject(_sheets);
            if (_workbook != null)
                _workbook.Close();

            Marshal.ReleaseComObject(_workbook);
            if (_excelApp != null)
                _excelApp.Quit();
            Marshal.ReleaseComObject(_excelApp);

            _excelApp = null;
        }

        private void SetInventoryCategories()
        {
            _itemCategories = new HashSet<string>();

            foreach(InventoryItem item in _models.InventoryItems)
            {
                _itemCategories.Add(item.Category.ToUpper());
            }
        }

        private void SetCategoryColors()
        {
            const string COLOR = "_COLOR";

            _categoryColors = new Dictionary<string, string>();

            FieldInfo[] fields = typeof(GlobalVar).GetFields().Where(x => x.Name.IndexOf(COLOR) != -1).ToArray();
            string[] fieldNames = fields.Select(x => x.Name).ToArray();
            foreach(string category in _itemCategories)
            {
                int idx = Array.IndexOf(fieldNames, category.Replace(' ', '_') + COLOR);
                if (idx > -1)
                {
                    _categoryColors[category] = (string)fields[idx].GetValue(null);
                }
            }
        }
    }
}

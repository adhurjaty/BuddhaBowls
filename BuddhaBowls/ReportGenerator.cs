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

        public ReportGenerator()
        {
            SetInventoryCategories();
            SetCategoryColors();

            _excelApp = new Excel.Application();
            _excelApp.DisplayAlerts = false;
            _workbook = _excelApp.Workbooks.Add();
            _sheets = _workbook.Sheets;

            _models = new ModelContainer();
        }

        ~ReportGenerator()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Marshal.ReleaseComObject(_sheets);
            _workbook.Close();
            Marshal.ReleaseComObject(_workbook);
            _excelApp.Quit();
            Marshal.ReleaseComObject(_excelApp);
        }

        public void Generate()
        {
            
        }

        public void FillInventoryId(string recipeName)
        {
            List<RecipeItem> recipe = MainHelper.GetRecipe(recipeName);

            foreach(RecipeItem ri in recipe)
            {
                ri.InventoryItemId = _models.InventoryItems.First(x => x.Name == ri.Name).Id;
                ri.Update(recipeName);
            }
        }

        public List<string[]> MakeBatchRecipeTable(string recipeName)
        {
            List<RecipeItem> recipe = MainHelper.GetRecipe(recipeName);
            List<string[]> outList = new List<string[]>();
            Dictionary<string, List<string[]>> categoryDict = new Dictionary<string, List<string[]>>();
            Dictionary<string, float> categoryCosts = new Dictionary<string, float>();

            string[] headers = new string[] { "NAME", "MEASURE", "RECIPE UNIT", "# RU", "RU COST", "COST" };

            foreach(RecipeItem item in recipe)
            {
                InventoryItem inv = _models.InventoryItems[(int)item.InventoryItemId];
                if(!categoryDict.Keys.Contains(inv.Category))
                {
                    categoryDict[inv.Category] = new List<string[]>();
                    categoryCosts[inv.Category] = 0;
                }

                float cost = inv.GetCost();
                float lineCost = cost * item.Quantity;
                categoryDict[inv.Category].Add(new string[] { inv.Name, item.Measure, inv.RecipeUnit,
                                                item.Quantity.ToString(), cost.ToString(), lineCost.ToString() });
                categoryCosts[inv.Category] += lineCost;
            }

            float batchCost = categoryCosts.Keys.Sum(x => categoryCosts[x]);

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

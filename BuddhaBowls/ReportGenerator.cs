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
using System.Windows;
using System.Windows.Media;
using Excel = Microsoft.Office.Interop.Excel;

namespace BuddhaBowls
{
    public class ReportGenerator
    {
        //HashSet<string> _itemCategories;
        //Dictionary<string, string> _categoryColors;

        ModelContainer _models;

        Excel.Application _excelApp;
        Excel.Workbook _workbook;
        Excel.Sheets _sheets;

        public ReportGenerator(ModelContainer models)
        {
            _models = models;

            //SetInventoryCategories();
            //SetCategoryColors();

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
            //List<BatchItem> recipe = _models.Recipes.First(x => x.Name.ToUpper() == recipeName.ToUpper()).itemList;

            //foreach(BatchItem ri in recipe)
            //{
            //    InventoryItem item = _models.InventoryItems.FirstOrDefault(x => x.Name == ri.Name);
            //    if (item == null)
            //        ri.InventoryItemId = null;
            //    else
            //        ri.InventoryItemId = item.Id;
            //    ri.Update(recipeName);
            //}
        }

        public List<string[]> MakeBatchRecipeTable(string recipeName)
        {
            //Recipe batchItem = _models.Recipes.First(x => x.Name.ToUpper() == recipeName.ToUpper());
            //List<BatchItem> recipe = batchItem.itemList;
            //List<string[]> outList = new List<string[]>();
            //Dictionary<string, List<string[]>> categoryDict = new Dictionary<string, List<string[]>>();

            //string[] headers = new string[] { "NAME", "MEASURE", "RECIPE UNIT", "# RU", "RU COST", "COST" };

            //foreach(BatchItem item in recipe)
            //{
            //    InventoryItem inv = _models.InventoryItems[(int)item.InventoryItemId];
            //    if(!categoryDict.Keys.Contains(inv.Category))
            //        categoryDict[inv.Category] = new List<string[]>();

            //    float cost = inv.GetCost();
            //    float lineCost = cost * item.Quantity;
            //    categoryDict[inv.Category].Add(new string[] { inv.Name, item.Measure, inv.RecipeUnit,
            //                                    item.Quantity.ToString(), cost.ToString(), lineCost.ToString() });
            //}

            //Dictionary<string, float> categoryCosts = _models.GetCategoryCosts(batchItem);
            //float batchCost = _models.GetBatchItemCost(batchItem);

            //outList.Add(new string[] { recipeName });

            //string[] sortedKeys = categoryDict.Keys.ToArray();
            //Array.Sort(sortedKeys);
            //foreach (string key in sortedKeys)
            //{
            //    string[] headerCopy = new string[headers.Length];
            //    Array.Copy(headers, headerCopy, headers.Length);
            //    headerCopy[0] = key;
            //    outList.Add(headerCopy);

            //    foreach(string[] row in categoryDict[key])
            //    {
            //        outList.Add(row);
            //    }

            //    outList.Add(new string[] { key.ToUpper() + " TOTAL", categoryCosts[key].ToString("c") });
            //    outList.Add(new string[] { "%", (categoryCosts[key] / batchCost).ToString("p1") });
            //}

            //outList.Add(new string[] { "BATCH TOTAL", batchCost.ToString("c") });

            //return outList;
            return null;
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
                string key = _models.ItemCategories.FirstOrDefault(x => x.StartsWith(category));
                if (!string.IsNullOrEmpty(key))
                {
                    ((Excel.Range)sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, numCols]]).Interior.Color =
                                    _models.GetColorFromCategory(key);
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

                    range1.Interior.Color = _models.GetColorFromCategory(contents[i][0].ToUpper());
                    range2.Interior.Color = _models.GetColorFromCategory(contents[i][0].ToUpper());
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
                    string key = _models.ItemCategories.FirstOrDefault(x => x.StartsWith(category));
                    if (!string.IsNullOrEmpty(key))
                    {
                        ((Excel.Range)sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, numCols]]).Interior.Color =
                                        _models.GetColorFromCategory(key);
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

        public string GenerateOrder(PurchaseOrder po, Vendor vendor, string filepath = "")
        {
            List<InventoryItem> items = po.GetOpenPOItems();
            if (items == null)
                items = po.GetReceivedPOItems();

            HashSet<string> categories = new HashSet<string>(items.Select(x => x.Category));
            Dictionary<string, float> categoryCosts = new Dictionary<string, float>();

            Excel.Worksheet sheet = _sheets.Add();

            // format column widths
            sheet.Columns[2].ColumnWidth = 12;
            sheet.Columns[3].ColumnWidth = 15;
            sheet.Columns[4].ColumnWidth = 15;
            sheet.Columns[6].ColumnWidth = 12;

            sheet.Name = vendor.Name + " Purchase Order";

            int row = 1;
            Excel.Range range = sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 6]];
            range.Merge();
            sheet.Cells[row, 1] = vendor.Name + " Purchase Order";
            sheet.Cells[row, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            row++;
            row++;

            sheet.Cells[row, 1] = "Contact";
            sheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            range = sheet.Range[sheet.Cells[row, 2], sheet.Cells[row, 3]];
            range.Merge();
            sheet.Cells[row, 2] = vendor.Contact;
            sheet.Cells[row, 4] = "Purchase Order #";
            range = sheet.Range[sheet.Cells[row, 5], sheet.Cells[row, 6]];
            range.Merge();
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            sheet.Cells[row, 5] = po.Id.ToString();
            row++;
            sheet.Cells[row, 1] = "Phone:";
            range = sheet.Range[sheet.Cells[row, 2], sheet.Cells[row, 3]];
            range.Merge();
            sheet.Cells[row, 2] = vendor.PhoneNumber;
            sheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            sheet.Cells[row, 4] = "Date:";
            range = sheet.Range[sheet.Cells[row, 5], sheet.Cells[row, 6]];
            range.Merge();
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            sheet.Cells[row, 5] = po.OrderDate.ToString("MM/dd/yy");
            row++;
            row++;

            int startRow = 0;
            foreach(string category in categories)
            {
                long color = _models.GetColorFromCategory(category);
                range = (Excel.Range)sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 6]];
                range.Interior.Color = color;
                range.WrapText = true;
                range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                range.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
                range.Borders.Weight = Excel.XlBorderWeight.xlThick;
                range.Font.Bold = true;
                sheet.Cells[row, 1] = "Pack Size";
                sheet.Cells[row, 2] = "Purchased Unit";
                sheet.Cells[row, 3] = category;
                sheet.Cells[row, 4] = "Order Amt";
                sheet.Cells[row, 5] = "Current Price";
                sheet.Cells[row, 6] = "Extension";
                row++;

                startRow = row;
                categoryCosts[category] = 0;
                foreach(InventoryItem item in items.Where(x => x.Category.ToUpper() == category.ToUpper()).OrderBy(x => x.Name))
                {
                    sheet.Cells[row, 1] = item.Conversion.ToString();
                    sheet.Cells[row, 2] = item.PurchasedUnit;
                    sheet.Cells[row, 3] = item.Name;
                    sheet.Cells[row, 4] = item.LastOrderAmount != 0 ? item.LastOrderAmount.ToString() : "";
                    sheet.Cells[row, 5] = item.LastPurchasedPrice.ToString("c");
                    sheet.Cells[row, 6] = item.PriceExtension.ToString("c");
                    categoryCosts[category] += item.PriceExtension;
                    row++;
                }

                ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]).Merge();
                ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]).Font.Bold = true;
                sheet.Cells[row, 4] = category + " Total:";
                sheet.Cells[row, 6] = categoryCosts[category].ToString("c");
                ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 6]]).Interior.Color = color;
                ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row, 6]]).Borders.Weight = Excel.XlBorderWeight.xlMedium;
                ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row, 6]]).BorderAround2(Weight: Excel.XlBorderWeight.xlThick);

                row++;
            }
            row++;

            startRow = row;
            foreach(KeyValuePair<string, float> kvp in categoryCosts)
            {
                range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
                range.Merge();
                range.Font.Bold = true;
                sheet.Cells[row, 4] = kvp.Key + " Total";
                sheet.Cells[row, 6] = kvp.Value.ToString("c");
                row++;
            }
            ((Excel.Range)sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 3]]).Merge();
            range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
            range.Merge();
            range.Font.Bold = true;
            sheet.Cells[row, 4] = "Total";
            sheet.Cells[row, 6] = categoryCosts.Keys.Sum(x => categoryCosts[x]).ToString("c");

            range = (Excel.Range)sheet.Range[sheet.Cells[startRow, 4], sheet.Cells[row, 6]];
            range.Borders.Weight = Excel.XlBorderWeight.xlMedium;
            range.BorderAround2(Weight: Excel.XlBorderWeight.xlThick);

            if(string.IsNullOrEmpty(filepath))
                filepath = Path.Combine(Properties.Settings.Default.DBLocation, "Purchase Orders", "PO_" + po.Id.ToString() + ".xlsx");

            try
            {
                _workbook.SaveAs(filepath);
            }
            catch(Exception e)
            {
                MessageBox.Show("Can't save purchase order. Check that purchase orders are closed on your machine and there are no open Excel processes",
                                "Purchase Order Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return filepath;
        }

        public string GenerateReceivingList(PurchaseOrder po, Vendor vendor)
        {
            List<InventoryItem> items = po.GetOpenPOItems();
            List<string> itemOrder = vendor.GetRecListOrder();
            HashSet<string> categories = new HashSet<string>(items.Select(x => x.Category));

            Excel.Worksheet sheet = _sheets.Add();

            // format column widths
            sheet.Columns[2].ColumnWidth = 12;
            sheet.Columns[3].ColumnWidth = 15;
            sheet.Columns[4].ColumnWidth = 15;
            sheet.Columns[6].ColumnWidth = 12;

            sheet.Name = vendor.Name + " Receiving List";

            int row = 1;
            Excel.Range range = sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 6]];
            range.Merge();
            sheet.Cells[row, 1] = vendor.Name + " Receiving List";
            sheet.Cells[row, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            row++;
            row++;

            sheet.Cells[row, 1] = "Contact";
            sheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            range = sheet.Range[sheet.Cells[row, 2], sheet.Cells[row, 3]];
            range.Merge();
            sheet.Cells[row, 2] = vendor.Contact;
            sheet.Cells[row, 4] = "Purchase Order #";
            range = sheet.Range[sheet.Cells[row, 5], sheet.Cells[row, 6]];
            range.Merge();
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            sheet.Cells[row, 5] = po.Id.ToString();
            row++;
            sheet.Cells[row, 1] = "Phone:";
            range = sheet.Range[sheet.Cells[row, 2], sheet.Cells[row, 3]];
            range.Merge();
            sheet.Cells[row, 2] = vendor.PhoneNumber;
            sheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            sheet.Cells[row, 4] = "Date:";
            range = sheet.Range[sheet.Cells[row, 5], sheet.Cells[row, 6]];
            range.Merge();
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            sheet.Cells[row, 5] = po.OrderDate.ToString("MM/dd/yy");
            row++;
            row++;

            int startRow = 0;

            range = (Excel.Range)sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 7]];
            range.WrapText = true;
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            range.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            range.Borders.Weight = Excel.XlBorderWeight.xlThick;
            range.Font.Bold = true;
            sheet.Cells[row, 1] = "Pack Size";
            sheet.Cells[row, 2] = "Purchased Unit";
            sheet.Cells[row, 3] = "Item Name";
            sheet.Cells[row, 4] = "Order Amt";
            sheet.Cells[row, 5] = "Current Price";
            sheet.Cells[row, 6] = "Extension";
            sheet.Cells[row, 7] = "Received?";
            row++;

            startRow = row;
            foreach (InventoryItem item in items.OrderBy(x => itemOrder.IndexOf(x.Name)))
            {
                sheet.Cells[row, 1] = item.Conversion.ToString();
                sheet.Cells[row, 2] = item.PurchasedUnit;
                sheet.Cells[row, 3] = item.Name;
                sheet.Cells[row, 4] = item.LastOrderAmount;
                sheet.Cells[row, 5] = item.LastPurchasedPrice.ToString("c");
                sheet.Cells[row, 6] = item.PriceExtension.ToString("c");
                row++;
            }

            ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row-1, 7]]).BorderAround2(Weight: Excel.XlBorderWeight.xlThick);
            ((Excel.Range)sheet.Range[sheet.Cells[startRow+1, 1], sheet.Cells[row-1, 7]]).Borders.Weight = Excel.XlBorderWeight.xlMedium;

            string filepath = Path.Combine(Properties.Settings.Default.DBLocation, "Receiving Lists", "ReceivingList_" + po.Id.ToString() + ".xlsx");
            try
            {
                _workbook.SaveAs(filepath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Can't save receiving list. Check that receiving list are closed on your machine and there are no open Excel processes",
                                "Receiving List Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return filepath;
        }

        public string GenerateVendorOrderSheet(Vendor vendor)
        {
            PurchaseOrder po = new PurchaseOrder(vendor, vendor.GetFromPriceList(), DateTime.Now);
            return GenerateOrder(po, vendor,
                Path.Combine(Properties.Settings.Default.DBLocation, "Receiving Lists", "ReceivingList_" + po.Id.ToString() + ".xlsx"));

        }

        //public string GenerateReceivingList(PurchaseOrder po, Vendor vendor)
        //{
        //    List<InventoryItem> items = po.GetOpenPOItems();
        //    HashSet<string> categories = new HashSet<string>(items.Select(x => x.Category));

        //    Excel.Worksheet sheet = _sheets.Add();

        //    // format column widths
        //    sheet.Columns[2].ColumnWidth = 12;
        //    sheet.Columns[3].ColumnWidth = 15;
        //    sheet.Columns[4].ColumnWidth = 15;
        //    sheet.Columns[6].ColumnWidth = 12;

        //    sheet.Name = vendor.Name + " Receiving List";

        //    int row = 1;
        //    Excel.Range range = sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 6]];
        //    range.Merge();
        //    sheet.Cells[row, 1] = vendor.Name + " Receiving List";
        //    sheet.Cells[row, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
        //    row++;
        //    row++;

        //    sheet.Cells[row, 1] = "Contact";
        //    sheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
        //    range = sheet.Range[sheet.Cells[row, 2], sheet.Cells[row, 3]];
        //    range.Merge();
        //    sheet.Cells[row, 2] = vendor.Contact;
        //    sheet.Cells[row, 4] = "Purchase Order #";
        //    range = sheet.Range[sheet.Cells[row, 5], sheet.Cells[row, 6]];
        //    range.Merge();
        //    range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
        //    sheet.Cells[row, 5] = po.Id.ToString();
        //    row++;
        //    sheet.Cells[row, 1] = "Phone:";
        //    range = sheet.Range[sheet.Cells[row, 2], sheet.Cells[row, 3]];
        //    range.Merge();
        //    sheet.Cells[row, 2] = vendor.PhoneNumber;
        //    sheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
        //    sheet.Cells[row, 4] = "Date:";
        //    range = sheet.Range[sheet.Cells[row, 5], sheet.Cells[row, 6]];
        //    range.Merge();
        //    range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
        //    sheet.Cells[row, 5] = po.OrderDate.ToString("MM/dd/yy");
        //    row++;
        //    row++;

        //    int startRow = 0;
        //    foreach (string category in categories)
        //    {
        //        long color = _models.GetColorFromCategory(category);
        //        range = (Excel.Range)sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 7]];
        //        range.Interior.Color = color;
        //        range.WrapText = true;
        //        range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
        //        range.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
        //        range.Borders.Weight = Excel.XlBorderWeight.xlThick;
        //        range.Font.Bold = true;
        //        sheet.Cells[row, 1] = "Pack Size";
        //        sheet.Cells[row, 2] = "Purchased Unit";
        //        sheet.Cells[row, 3] = category;
        //        sheet.Cells[row, 4] = "Order Amt";
        //        sheet.Cells[row, 5] = "Current Price";
        //        sheet.Cells[row, 6] = "Extension";
        //        sheet.Cells[row, 7] = "Received?";
        //        row++;

        //        startRow = row;
        //        foreach (InventoryItem item in items.Where(x => x.Category.ToUpper() == category.ToUpper()).OrderBy(x => x.Name))
        //        {
        //            sheet.Cells[row, 1] = item.Conversion.ToString();
        //            sheet.Cells[row, 2] = item.PurchasedUnit;
        //            sheet.Cells[row, 3] = item.Name;
        //            sheet.Cells[row, 4] = item.LastOrderAmount;
        //            sheet.Cells[row, 5] = item.LastPurchasedPrice.ToString("c");
        //            sheet.Cells[row, 6] = item.PriceExtension.ToString("c");
        //            row++;
        //        }

        //        ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row, 7]]).Borders.Weight = Excel.XlBorderWeight.xlMedium;
        //        ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row, 7]]).BorderAround2(Weight: Excel.XlBorderWeight.xlThick);

        //        row++;
        //    }
        //    row++;

        //    string filepath = Path.Combine(Properties.Settings.Default.DBLocation, "Receiving Lists", "ReceivingList_" + po.Id.ToString() + ".xlsx");
        //    _workbook.SaveAs(filepath);

        //    return filepath;
        //}

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
    }
}

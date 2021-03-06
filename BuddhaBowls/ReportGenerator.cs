﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
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

        DBCache _models;

        Excel.Application _excelApp;
        Excel.Workbook _workbook;
        Excel.Sheets _sheets;

        public ReportGenerator(DBCache models)
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
                string key = _models.GetInventoryCategories().FirstOrDefault(x => x.StartsWith(category));
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

        public string GenerateMasterInventoryTable(string filename)
        {
            Excel.Worksheet sheet = _sheets.Add();
            sheet.Name = "Master Invetory List";
            int numRows = 8;

            string filePath = Path.Combine(Properties.Settings.Default.DBLocation, "Reports", filename);
            if(IsWorkbookOpen(filePath))
            {
                MessageBox.Show("Close Master Inventory List", "Excel Error");
                return "";
            }

            string[] subHeader = new string[] { "Unit", "Unit Price", "Conversion", "Count Unit", "Count Price", "Count No.", "Extension" };

            // initial column formatting
            sheet.Columns[1].ColumnWidth = 20.67;
            sheet.Columns[2].ColumnWidth = 12.67;
            sheet.Columns[3].ColumnWidth = 8.83;
            sheet.Columns[4].ColumnWidth = 10.5;
            sheet.Columns[5].ColumnWidth = 10.5;
            sheet.Columns[6].ColumnWidth = 10;
            sheet.Columns[7].ColumnWidth = 10.5;
            sheet.Columns[8].ColumnWidth = 10.5;

            // top header
            Excel.Range topHeaderRange = sheet.Range[sheet.Cells[1, 1], sheet.Cells[2, numRows]];
            topHeaderRange.Merge();
            topHeaderRange.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            topHeaderRange.Font.Size = 20;
            sheet.Cells[1, 1] = "Buddha Bowls Master Inventory";
            sheet.Cells[1, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            sheet.Cells[1, 1].Font.Bold = true;

            // categorized sections
            int currentRow = 3;
            int[] borderBounds = new int[2];
            foreach (IGrouping<string, VendorInventoryItem> invGroup in MainHelper.CategoryGrouping(_models.VIContainer.Items))
            {
                string category = invGroup.Key;
                long categoryColor = _models.GetColorFromCategory(category);

                Excel.Range purchaseRange = sheet.Range[sheet.Cells[currentRow, 2], sheet.Cells[currentRow, 3]];
                purchaseRange.Merge();
                Excel.Range countRange = sheet.Range[sheet.Cells[currentRow, 5], sheet.Cells[currentRow, 7]];
                countRange.Merge();

                // category top headers
                sheet.Cells[currentRow, 2] = "AS PURCHASED UNITS";
                sheet.Cells[currentRow, 5] = "INVENTORY COUNT UNIT";
                purchaseRange.Interior.Color = categoryColor;
                purchaseRange.Font.Bold = true;
                purchaseRange.BorderAround2(Weight: Excel.XlBorderWeight.xlThick);
                purchaseRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                countRange.Interior.Color = categoryColor;
                countRange.Font.Bold = true;
                countRange.BorderAround2(Weight: Excel.XlBorderWeight.xlThick);
                countRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                currentRow++;
                borderBounds[0] = currentRow;

                sheet.Cells[currentRow, 1] = category;
                sheet.Cells[currentRow, 1].Font.Bold = true;
                sheet.Cells[currentRow, 1].Interior.Color = categoryColor;

                // category sub-headers
                for (int i = 0; i < subHeader.Length; i++)
                {
                    sheet.Cells[currentRow, 2 + i] = subHeader[i];
                    sheet.Cells[currentRow, 2 + i].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    sheet.Cells[currentRow, 2 + i].Font.Bold = true;
                }

                currentRow++;

                // items in category
                float categoryTotal = 0;
                float extension;
                foreach (VendorInventoryItem item in invGroup)
                {
                    sheet.Cells[currentRow, 1] = item.Name;
                    sheet.Cells[currentRow, 2] = item.PurchasedUnit;
                    sheet.Cells[currentRow, 3] = item.LastPurchasedPrice.ToString("c");
                    sheet.Cells[currentRow, 4] = string.Format("{0:0.##}", item.Conversion);
                    sheet.Cells[currentRow, 4].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    sheet.Cells[currentRow, 5] = item.CountUnit;
                    if (item.Conversion > 0)
                    {
                        sheet.Cells[currentRow, 6] = (item.LastPurchasedPrice / item.Conversion).ToString("c");
                        extension = (item.LastPurchasedPrice / item.Conversion) * item.Count;
                    }
                    else
                    {
                        sheet.Cells[currentRow, 6] = "ERR";
                        extension = -1;
                    }
                    sheet.Cells[currentRow, 7] = string.Format("{0:0.##}", item.Count);
                    sheet.Cells[currentRow, 8] = extension != -1 ? extension.ToString("c") : "ERR";

                    categoryTotal += extension;
                    currentRow++;
                }

                // category total
                ((Excel.Range)sheet.Range[sheet.Cells[currentRow, 1], sheet.Cells[currentRow, 8]]).Interior.Color = categoryColor;
                Excel.Range label = sheet.Range[sheet.Cells[currentRow, 6], sheet.Cells[currentRow, 7]];
                label.Merge();
                sheet.Cells[currentRow, 6] = category + " Total:";
                sheet.Cells[currentRow, 6].Font.Bold = true;
                sheet.Cells[currentRow, 6].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                sheet.Cells[currentRow, 8] = categoryTotal.ToString("c");

                borderBounds[1] = currentRow;

                Excel.Range wholeRange = sheet.Range[sheet.Cells[borderBounds[0], 1], sheet.Cells[borderBounds[1], 8]];
                wholeRange.Borders.Weight = Excel.XlBorderWeight.xlMedium;
                wholeRange.BorderAround2(Weight: Excel.XlBorderWeight.xlThick);

                currentRow += 2;
            }

            _workbook.SaveAs(filePath);
            return filePath;
        }

        public List<string[]> MakeMasterInventoryTable()
        {
            List<string[]> outList = new List<string[]>();

            string[] topHeader = new string[] { "", "AS PURCHASED UNITS", "INVENTORY COUNT UNIT" };
            string[] subHeader = new string[] { "", "Unit", "Unit Price", "Conversion", "Count Unit", "Count Price", "Count No.", "Extension" };

            List<VendorInventoryItem> categorizedInvItems = _models.VIContainer.Items.OrderBy(x => x.Category).ToList();
            string curCategory = "";
            float categoryCost = 0;

            foreach (VendorInventoryItem item in categorizedInvItems)
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

            for(int i = 1; i <= numCols; i++)
            {
                sheet.Columns[i].ColumnWidth = 15;
            }

            filename = Path.GetFileNameWithoutExtension(filename) + ".xlsx";

            int[] sectionRows = new int[3];
            int sectionIdx = 1;

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
                    range1.Font.Bold = true;
                    range2.Font.Bold = true;

                    sheet.Cells[i + 1, 2] = contents[i][1];
                    sheet.Cells[i + 1, 5] = contents[i][2];

                    range1.Interior.Color = _models.GetColorFromCategory(contents[i][0]);
                    range2.Interior.Color = _models.GetColorFromCategory(contents[i][0]);

                    sectionRows[0] = i;
                    sectionIdx = 1;
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
                    string key = _models.GetInventoryCategories().FirstOrDefault(x => x.StartsWith(category));
                    if (!string.IsNullOrEmpty(key))
                    {
                        Excel.Range colorRange = sheet.Range[sheet.Cells[i + 1, 1], sheet.Cells[i + 1, numCols]];
                        colorRange.Interior.Color = _models.GetColorFromCategory(key);
                        colorRange.Font.Bold = true;

                        sectionRows[sectionIdx] = i;
                        sectionIdx++;
                    }

                    if(sectionRows.Length == sectionIdx)
                    {
                        Excel.Range borderRange = sheet.Range[sheet.Cells[sectionRows[0] + 1, 1], sheet.Cells[sectionRows[1] + 1, numCols]];
                        borderRange.Borders.Weight = Excel.XlBorderWeight.xlThick;
                        borderRange = sheet.Range[sheet.Cells[sectionRows[1] + 1, 1], sheet.Cells[sectionRows[2] + 1, numCols]];
                        borderRange.Borders.Weight = Excel.XlBorderWeight.xlMedium;
                        borderRange.BorderAround2(Weight: Excel.XlBorderWeight.xlThick);

                        sectionIdx = 0;
                    }
                }

            }

            string filePath = Path.Combine(Properties.Settings.Default.DBLocation, "Reports", filename);
            _workbook.SaveAs(filePath);

            return filePath;
        }

        public string CreateMasterInventoryReport()
        {
            List<string[]> rows = MakeMasterInventoryTable();

            return MasterInventoryReport(rows, "Master Inventory Report");
        }

        public string GenerateOrder(PurchaseOrder po, Vendor vendor, string filepath = "")
        {
            List<InventoryItem> items = po.ItemList;
            Dictionary<string, float> categoryCosts = new Dictionary<string, float>();
            Excel.Worksheet sheet = _sheets.Add();

            // format column widths
            sheet.Columns[2].ColumnWidth = 12;
            sheet.Columns[3].ColumnWidth = 15;
            sheet.Columns[4].ColumnWidth = 15;
            sheet.Columns[6].ColumnWidth = 12;

            sheet.Name = GetOrderSheetName(vendor.Name);

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

            row = FillOrderSheet(ref sheet, items, row, ref categoryCosts);
            row++;

            int startRow = row;
            range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
            range.Merge();
            range.Font.Bold = true;
            sheet.Cells[row, 4] = "Total Cases";
            sheet.Cells[row, 6] = items.Sum(x => x.LastOrderAmount).ToString();
            row++;
            foreach (KeyValuePair<string, float> kvp in categoryCosts)
            {
                range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
                range.Merge();
                range.Font.Bold = true;
                sheet.Cells[row, 4] = kvp.Key + " Total";
                sheet.Cells[row, 6] = kvp.Value.ToString("c");
                ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 6]]).Interior.Color = _models.GetColorFromCategory(kvp.Key);
                row++;
            }

            if(vendor.ShippingCost > 0)
            {
                range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
                range.Merge();
                range.Font.Bold = true;
                sheet.Cells[row, 4] = "Shipping Cost:";
                sheet.Cells[row, 6] = vendor.ShippingCost.ToString("c");
                row++;
            }

            ((Excel.Range)sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 3]]).Merge();
            range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
            range.Merge();
            range.Font.Bold = true;
            sheet.Cells[row, 4] = "Total";
            sheet.Cells[row, 6] = (categoryCosts.Keys.Sum(x => categoryCosts[x]) + vendor.ShippingCost).ToString("c");

            range = (Excel.Range)sheet.Range[sheet.Cells[startRow, 4], sheet.Cells[row, 6]];
            range.Borders.Weight = Excel.XlBorderWeight.xlMedium;
            range.BorderAround2(Weight: Excel.XlBorderWeight.xlThick);

            if (string.IsNullOrEmpty(filepath))
            {
                string outDir = Path.Combine(Properties.Settings.Default.DBLocation, "Purchase Orders");
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                filepath = po.GetPOPath();
            }

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

        public int FillOrderSheet(ref Excel.Worksheet sheet, List<InventoryItem> items, int row, ref Dictionary<string, float> categoryCosts)
        {
            int startRow = 0;
            foreach (IGrouping<string, InventoryItem> invGroup in MainHelper.CategoryGrouping(items))
            {
                string category = invGroup.Key;
                long categoryColor = _models.GetColorFromCategory(category);
                Excel.Range range = (Excel.Range)sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 6]];
                range.Interior.Color = categoryColor;
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
                foreach (InventoryItem item in invGroup)
                {
                    sheet.Cells[row, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    sheet.Cells[row, 1] = item.Conversion.ToString();
                    sheet.Cells[row, 2] = item.PurchasedUnit;
                    sheet.Cells[row, 3] = item.Name;
                    sheet.Cells[row, 4] = item.LastOrderAmount != 0 ? item.LastOrderAmount.ToString() : "";
                    sheet.Cells[row, 5] = item.LastPurchasedPrice.ToString("c");
                    sheet.Cells[row, 6] = item.PurchaseExtension.ToString("c");
                    categoryCosts[category] += item.PurchaseExtension;
                    row++;
                }

                ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]).Merge();
                ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]).Font.Bold = true;
                sheet.Cells[row, 4] = category + " Total:";
                sheet.Cells[row, 6] = categoryCosts[category].ToString("c");
                ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 6]]).Interior.Color = categoryColor;
                ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row, 6]]).Borders.Weight = Excel.XlBorderWeight.xlMedium;
                ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row, 6]]).BorderAround2(Weight: Excel.XlBorderWeight.xlThick);

                row++;
            }

            return row;
        }

        public string GenerateReceivingList(PurchaseOrder po, Vendor vendor)
        {
            List<InventoryItem> items = po.ItemList;
            List<string> itemOrder = vendor.GetRecListOrder();

            Excel.Worksheet sheet = _sheets.Add();

            // format column widths
            sheet.Columns[2].ColumnWidth = 12;
            sheet.Columns[3].ColumnWidth = 15;
            sheet.Columns[4].ColumnWidth = 15;
            sheet.Columns[6].ColumnWidth = 12;

            sheet.Name = GetRecSheetName(vendor.Name);

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
            float totalExtension = 0;
            foreach (InventoryItem item in items.OrderBy(x => itemOrder.IndexOf(x.Name)))
            {
                sheet.Cells[row, 1] = item.Conversion.ToString();
                sheet.Cells[row, 2] = item.PurchasedUnit;
                sheet.Cells[row, 3] = item.Name;
                sheet.Cells[row, 4] = item.LastOrderAmount;
                sheet.Cells[row, 5] = item.LastPurchasedPrice.ToString("c");
                sheet.Cells[row, 6] = item.PurchaseExtension.ToString("c");
                totalExtension += item.PurchaseExtension;
                row++;
            }
            totalExtension += vendor.ShippingCost;

            ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row - 1, 7]]).BorderAround2(Weight: Excel.XlBorderWeight.xlThick);
            ((Excel.Range)sheet.Range[sheet.Cells[startRow, 1], sheet.Cells[row - 1, 7]]).Borders.Weight = Excel.XlBorderWeight.xlMedium;

            range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
            range.Merge();
            range.Font.Bold = true;
            sheet.Cells[row, 4] = "Shipping Cost";
            sheet.Cells[row, 6] = vendor.ShippingCost.ToString("c");
            row++;

            range = ((Excel.Range)sheet.Range[sheet.Cells[row, 4], sheet.Cells[row, 5]]);
            range.Merge();
            range.Font.Bold = true;
            sheet.Cells[row, 4] = "Total";
            sheet.Cells[row, 6] = totalExtension.ToString("c");

            ((Excel.Range)sheet.Range[sheet.Cells[row-2, 4], sheet.Cells[row, 6]]).BorderAround2(Weight: Excel.XlBorderWeight.xlThick);
            ((Excel.Range)sheet.Range[sheet.Cells[row-2, 4], sheet.Cells[row, 6]]).Borders.Weight = Excel.XlBorderWeight.xlMedium;

            string outDir = Path.Combine(Properties.Settings.Default.DBLocation, "Receiving Lists");
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            string filepath = Path.Combine(outDir, "ReceivingList_" + po.Id.ToString() + ".xlsx");
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
            List<InventoryItem> items = vendor.ItemList;

            Dictionary<string, float> categoryCosts = new Dictionary<string, float>();

            Excel.Worksheet sheet = _sheets.Add();

            // format column widths
            sheet.Columns[2].ColumnWidth = 12;
            sheet.Columns[3].ColumnWidth = 15;
            sheet.Columns[4].ColumnWidth = 15;
            sheet.Columns[6].ColumnWidth = 12;

            sheet.Name = vendor.Name + " Items";

            int row = 1;
            Excel.Range range = sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 6]];
            range.Merge();
            sheet.Cells[row, 1] = vendor.Name + " Items";
            sheet.Cells[row, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            row++;
            row++;

            sheet.Cells[row, 1] = "Contact";
            sheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            range = sheet.Range[sheet.Cells[row, 2], sheet.Cells[row, 3]];
            range.Merge();
            sheet.Cells[row, 2] = vendor.Contact;
            range = sheet.Range[sheet.Cells[row, 5], sheet.Cells[row, 6]];
            range.Merge();
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
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
            row++;
            row++;
            for(int i = 0; i < items.Count; i++)
            {
                items[i].LastOrderAmount = 0;
                items[i].Count = 0;
            }

            FillOrderSheet(ref sheet, items, row, ref categoryCosts);

            string outDir = Path.GetDirectoryName(vendor.GetOrderSheetPath());
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            string filepath = Path.Combine(outDir, vendor.Name + "_Items.xlsx");

            try
            {
                _workbook.SaveAs(filepath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Can't save purchase order. Check that purchase orders are closed on your machine and there are no open Excel processes",
                                "Vendor Items Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return filepath;
        }

        private bool IsWorkbookOpen(string filePath)
        {
            try
            {
                _workbook.SaveAs(filePath);
                return false;
            }
            catch(Exception e)
            {
                return true;
            }
        }

        private string GetSheetName(string name, string type)
        {
            if (name.Length + type.Length > 30)
                return name.Substring(0, 30 - type.Length) + type;
            return name + type;
        }

        private string GetOrderSheetName(string name)
        {
            return GetSheetName(name, " Purchase Order");
        }

        private string GetRecSheetName(string name)
        {
            return GetSheetName(name, " Receiving List");
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

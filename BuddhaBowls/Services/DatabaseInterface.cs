using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace BuddhaBowls.Services
{
    public class DatabaseInterface
    {
        private string _dataPath;
        private Excel.Application _app;
        private Excel.Workbook _workbook;
        private Excel.Worksheet _table;
        private bool _isOpen;

        public DatabaseInterface(string dataPath = null)
        {
            _dataPath = dataPath ?? Properties.Settings.Default.DBLocation;
            _app = new Excel.Application();
            _isOpen = false;
        }

        ~DatabaseInterface()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            _app.Quit();
            Marshal.ReleaseComObject(_app);
        }

        public string[] GetRecord(string tableName, Dictionary<string, string> mapping)
        {
            Excel.Range data;
            string[] columns;
            int[] colIdxs;
            string[] outRow = null;

            try
            {
                Open(tableName);
                data = _table.UsedRange;
                columns = GetColumnNames();
                string[] mappingKeys = mapping.Keys.ToArray();
                colIdxs = mappingKeys.Select(x => Array.IndexOf(columns, x)).ToArray();
                
                for(int i = 1; i <= data.Rows.Count; i++)
                {
                    Excel.Range row = data.Rows[i];
                    bool matchingRow = true;

                    for(int j = 0; j < colIdxs.Length; j++)
                    {
                        if(data.Cells[i, colIdxs[j] + 1] != mapping[mappingKeys[j]])
                        {
                            matchingRow = false;
                            break;
                        }
                    }

                    if(matchingRow)
                    {
                        outRow = row.Value;
                    }
                }
            }
            finally
            {
                Close();
            }

            return outRow;
        }

        public void Open(string tableName)
        {
            string tablePath = Path.Combine(_dataPath, tableName + ".csv");
            if (File.Exists(tablePath))
            {
                _workbook = _app.Workbooks.Open(Path.Combine(_dataPath, tableName));
                _table = _workbook.Sheets[1];
                _isOpen = true;
            }
            else
            {
                throw new ArgumentException("Invalid excel file: " + tablePath);
            }
        }

        public void Close()
        {
            if (_isOpen)
            {
                Marshal.ReleaseComObject(_table);
                _workbook.Close();
                Marshal.ReleaseComObject(_workbook);
                _isOpen = false;
            }
        }

        public string[] GetColumnNames()
        {
            if(!_isOpen)
            {
                throw new Exception("Worksheet must be open to use GetColumnNames");
            }

            return _table.Range["A1", "A1"].EntireRow.Value;
        }
    }
}

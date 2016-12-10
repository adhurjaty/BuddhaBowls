using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuddhaBowls.Services
{
    public class DatabaseInterface
    {
        private string _dataPath;

        public DatabaseInterface(string dataPath = null)
        {
            _dataPath = dataPath ?? Properties.Settings.Default.DBLocation;
        }

        public string[] GetRecord(string tableName, Dictionary<string, string> mapping)
        {
            string[][] records = GetRecords(tableName, mapping, 1);
            return records != null ? records[0] : null;
        }

        public string[][] GetRecords(string tableName, Dictionary<string, string> mapping = null, int limit = 0)
        {
            if(!File.Exists(FilePath(tableName)))
                return null;

            List<string[]> records = new List<string[]>();

            using (TextFieldParser parser = new TextFieldParser(FilePath(tableName)))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                string[] columnNames = GetColumnNames(tableName, parser);
                string[] mappingKeys = mapping != null ? mapping.Keys.ToArray() : new string[0];

                int[] columnIdxs = mappingKeys.Select(x => Array.IndexOf(columnNames, x)).ToArray();

                while (!parser.EndOfData)
                {
                    bool match = true;
                    string[] fields = parser.ReadFields();

                    for (int i = 0; i < columnIdxs.Length; i++)
                    {
                        if (fields[columnIdxs[i]] != mapping[mappingKeys[i]])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        records.Add(fields);
                        if (--limit == 0)
                            return records.ToArray();
                    }
                }
            }

            if (records.Count > 0)
                return records.ToArray();

            return null;
        }

        public int WriteRecord(string tableName, Dictionary<string, string> mapping)
        {
            // Id column always first, don't get it here - auto-populated field
            List<string> columns = GetColumnNames(tableName).Skip(1).ToList();
            List<string> newRecord = columns.Select(x => mapping.Keys.Contains(x) ? mapping[x] : "").ToList();

            int lastId = int.Parse(File.ReadLines(FilePath(tableName)).Last().Split(',')[0]);
            int newId = lastId + 1;

            newRecord.Insert(0, (lastId + 1).ToString());

            File.AppendAllText(FilePath(tableName), string.Join(",", newRecord) + "\n");

            return newId;
        }

        public bool DeleteRecords(string tableName, Dictionary<string, string> mapping, int limit = int.MaxValue)
        {
            List<string[]> fileContents = new List<string[]>();
            bool found = false;

            using (TextFieldParser parser = new TextFieldParser(FilePath(tableName)))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                string[] columnNames = GetColumnNames(tableName, parser);
                string[] mappingKeys = mapping.Keys.ToArray();

                int[] columnIdxs = mappingKeys.Select(x => Array.IndexOf(columnNames, x)).ToArray();

                fileContents.Add(columnNames);

                while (!parser.EndOfData)
                {
                    bool match = true;
                    string[] fields = parser.ReadFields();

                    for (int i = 0; i < columnIdxs.Length; i++)
                    {
                        if (fields[columnIdxs[i]] != mapping[mappingKeys[i]])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (!match || limit <= 0)
                        fileContents.Add(fields);
                    if (match)
                    {
                        limit--;
                        found = true;
                    }
                }
            }

            if(found)
            {
                File.WriteAllText(FilePath(tableName), string.Join("\n", fileContents.Select(x => string.Join(",", x))) + "\n");
            }

            return found;
        }

        public bool DeleteRecord(string tableName, Dictionary<string, string> mapping)
        {
            return DeleteRecords(tableName, mapping, 1);
        }

        public bool UpdateRecord(string tableName, Dictionary<string, string> setFields, int id)
        {
            List<string[]> fileContents = new List<string[]>();
            bool found = false;

            using (TextFieldParser parser = new TextFieldParser(FilePath(tableName)))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                string[] columnNames = GetColumnNames(tableName, parser);

                fileContents.Add(columnNames);

                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();

                    if (fields[0] == id.ToString())
                    {
                        string[] setFieldKeys = setFields.Keys.ToArray();
                        int[] columnIdxs = setFieldKeys.Select(x => Array.IndexOf(columnNames, x)).ToArray();

                        for(int i = 0; i < columnIdxs.Length; i++)
                        {
                            fields[columnIdxs[i]] = setFields[setFieldKeys[i]];
                        }

                        found = true;
                    }

                    fileContents.Add(fields);
                }
            }

            if (found)
            {
                File.WriteAllText(FilePath(tableName), string.Join("\n", fileContents.Select(x => string.Join(",", x))));
            }

            return found;
        }

        public string FilePath(string tableName)
        {
            return Path.Combine(_dataPath, tableName + ".csv");
        }

        public string[] GetColumnNames(string tableName, TextFieldParser parser = null)
        {
            if (parser == null)
            {
                using (TextFieldParser p = new TextFieldParser(FilePath(tableName)))
                {
                    p.TextFieldType = FieldType.Delimited;
                    p.SetDelimiters(",");
                    return p.ReadFields();
                }
            }

            return parser.ReadFields();
        }

        public void CreateTable(string[] colHeaders, string[][] rows, string tableName)
        {
            List<string> contents = new List<string>();
            contents.Add(string.Join(",", colHeaders));
            contents = contents.Concat(rows.Select(x => string.Join(",", x))).ToList();
            File.WriteAllLines(FilePath(tableName), contents);
        }
    }
}

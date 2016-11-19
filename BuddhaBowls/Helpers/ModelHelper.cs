using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BuddhaBowls.Models;
using BuddhaBowls.Services;

namespace BuddhaBowls.Helpers
{
    /// <summary>
    /// Collection of static functions to do model-specific tasks
    /// </summary>
    public static class ModelHelper
    {
        /// <summary>
        /// Create a list of some Model type T given field/value parameters
        /// </summary>
        /// <param name="mapping">Dictionary of WHERE _key_ = _value_ AND ... </param>
        /// <param name="orderBy">ORDER BY parameter in SQL</param>
        /// <returns></returns>
        public static List<T> InstantiateList<T>(Dictionary<string, string> mapping, string table) where T : Model, new()
        {
            T listObj = new T();
            DatabaseInterface dbInt = new DatabaseInterface();
            string[][] records = dbInt.GetRecords(table, mapping);

            if (records != null)
            {
                return InstantiateListHelper<T>(records);
            }

            return null;
        }

        /// <summary>
        /// Create a list of Model type T that has all records in table
        /// </summary>
        /// <returns></returns>
        public static List<T> InstantiateList<T>(string table, bool fileExists = true) where T : Model, new()
        {
            T listObj = new T();
            DatabaseInterface dbInt = new DatabaseInterface();
            string[][] records = dbInt.GetRecords(table);

            if (records != null)
            {
                return InstantiateListHelper<T>(records, fileExists);
            }

            return null;
        }

        /// <summary>
        /// Creates a list of Model objects from a 2D string
        /// </summary>
        /// <param name="records">Database collection of rows</param>
        /// <returns></returns>
        private static List<T> InstantiateListHelper<T>(string[][] records, bool fileExists = true) where T : Model, new()
        {
            T listObj;
            List<T> returnList = new List<T>();

            foreach (string[] row in records)
            {
                listObj = new T();
                if (fileExists)
                    listObj.InitializeObject(row);
                else
                {
                    string[] columns = OrderColumns(listObj.GetProperties());
                    listObj.InitializeObject(row, columns);
                }
                returnList.Add(listObj);
            }
            return returnList;
        }

        private static string[] OrderColumns(string[] columns)
        {
            List<string> ordered = columns.ToList();
            ordered.Insert(0, ordered.Last());
            ordered.RemoveAt(ordered.Count - 1);

            return ordered.ToArray();
        }
        //public static bool CompareSingles(Single n1, Single n2)
        //{

        //    return Round(n1, 3) == Round(n2, 3);
        //}

        //private static Single Round(Single n, Int32 places)
        //{
        //    return (Single)(Int32)(n * 10 * places + 0.5f) / (10f * places);
        //}

    }
}


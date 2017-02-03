using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class Model
    {
        protected string _tableName;
        protected DatabaseInterface _dbInt;

        public int Id { get; set; } = -1;

        public Model()
        {
            _dbInt = new DatabaseInterface();
        }

        /// <summary>
        /// Sets model's properties given a single database row. Used in all models' constructors
        /// </summary>
        /// <param name="row">csv row from table file</param>
        public void InitializeObject(string[] row, string[] columns = null)
        {
            string[] properties = GetPropertiesDB();
            columns = columns ?? _dbInt.GetColumnNames(_tableName);

            foreach (string property in properties)
            {
                if (property != null && columns.Contains(property))
                {
                    int idx = Array.IndexOf(columns, property);
                    if (idx > -1 && row[idx] != null)
                    {
                        SetProperty(property, row[idx]);
                    }
                }
            }
        }

        /// <summary>
        /// Writes model to DB
        /// </summary>
        public virtual void Update()
        {
            Dictionary<string, string> setDict = FieldsToDict();
            //Dictionary<string, string> setDict = new Dictionary<string, string>();
            //object value;

            //foreach (string prop in GetPropertiesDB())
            //{
            //    value = GetPropertyValue(prop);
            //    if (value != null && prop != "Id")
            //        setDict[prop] = value.ToString();
            //}

            _dbInt.UpdateRecord(_tableName, setDict, Id);
        }

        /// <summary>
        /// Deletes model from DB
        /// </summary>
        public virtual void Destroy()
        {
            _dbInt.DeleteRecords(_tableName, new Dictionary<string, string>() { { "Id", Id.ToString() } });
        }

        /// <summary>
        /// Inserts new model in DB
        /// </summary>
        public virtual int Insert()
        {
            Dictionary<string, string> mapping = FieldsToDict();
            Id = _dbInt.WriteRecord(_tableName, mapping);
            return Id;
        }

        public Dictionary<string, string> FieldsToDict()
        {
            string[] propNames = GetPropertiesDB(new string[] { "Id" }); // don't include ID - DB creates a unique one

            Dictionary<string, string> mapping = new Dictionary<string, string>();

            for (Int32 i = 0; i < propNames.Length; i++)
            {
                if (GetPropertyValue(propNames[i]) != null)
                    mapping[propNames[i]] = GetPropertyValue(propNames[i]).ToString();
            }

            return mapping;
        }

        /// <summary>
        /// Creates copy of object
        /// </summary>
        /// <typeparam name="T">Class calling this method</typeparam>
        protected T Copy<T>() where T : Model, new()
        {
            T copy = new T();
            foreach (PropertyInfo prop in copy.GetType().GetProperties())
            {
                copy.SetProperty(prop.Name, prop.GetValue(this));
            }

            return copy;
        }

        /// <summary>
        /// Check whether properties of object and other are equal
        /// </summary>
        /// <typeparam name="T">Class calling method</typeparam>
        /// <param name="other">Model to compare to this</param>
        //public bool Equals<T>(T other) where T : Model, new()
        //{
        //    foreach (PropertyInfo prop in GetType().GetProperties())
        //    {
        //        if (prop.GetValue(this) == null)
        //        {
        //            if (prop.GetValue(other) == null)
        //                continue;
        //            else
        //                return false;
        //        }
        //        if (GetPropertyValue(prop.Name).GetType() == typeof(Single))
        //        {
        //            return ModelHelper.CompareSingles((Single)GetPropertyValue(prop.Name), (Single)other.GetPropertyValue(prop.Name));
        //        }
        //        if (!GetPropertyValue(prop.Name).Equals(other.GetPropertyValue(prop.Name)))
        //            return false;
        //    }

        //    return true;
        //}

        /// <summary>
        /// Creates dictionary whose key is the name of the property and value is a string showing values of this and other properties comma separated.
        /// Used for testing
        /// </summary>
        public Dictionary<string, string> Compare<T>(T other) where T : Model, new()
        {
            Dictionary<string, string> outDict = new Dictionary<string, string>();

            foreach (PropertyInfo prop in GetType().GetProperties())
            {
                if (prop.GetValue(this) == null)
                {
                    if (prop.GetValue(other) == null)
                        continue;
                    else
                        outDict[prop.Name] = "null, " + prop.GetValue(other).ToString();
                }
                else if (prop.GetValue(other) == null)
                    outDict[prop.Name] = prop.GetValue(this).ToString() + ", " + "null";
                else if (!prop.GetValue(this).Equals(prop.GetValue(other)) &&
                         !((prop.PropertyType == typeof(Single) || prop.PropertyType == typeof(Single?)) &&
                           Math.Abs((Single)prop.GetValue(this) - (Single)prop.GetValue(other)) < 0.01))
                    outDict[prop.Name] = prop.GetValue(this).ToString() + ", " + prop.GetValue(other).ToString();
            }

            return outDict;
        }

        /// <summary>
        /// Set properties of Model to null
        /// </summary>
        /// <param name="omit">Do not nullify indicated properties</param>
        public void Clear(string[] omit = null)
        {
            List<string> omitList = new List<string>() { "Id" };
            if (omit != null)
            {
                omitList = omitList.Concat(omit.ToList()).ToList();
            }

            foreach (string prop in GetProperties())
            {
                if (!omitList.Contains(prop))
                {
                    if (GetPropertyValue(prop) == null || Nullable.GetUnderlyingType(GetPropertyValue(prop).GetType()) != null ||
                        GetPropertyType(prop) == typeof(DateTime))
                        SetProperty(prop, null);
                    else if (GetPropertyType(prop) == typeof(string))
                        SetProperty(prop, "");
                    else if (GetPropertyType(prop) == typeof(bool))
                        SetProperty(prop, false);
                    else
                        SetProperty(prop, Convert.ChangeType(0, GetPropertyType(prop)));
                }
            }
        }

        #region custom reflection methods
        /// <summary>
        /// Sets property to value. Does nothing if there is an invalid property name. Case sensitive
        /// </summary>
        /// <param name="property">name of property as string</param>
        /// <param name="value">value to set property</param>
        public virtual void SetProperty(string property, object value)
        {
            property = GetPropertyName(property);

            if (property == null)
                return;

            if (value == null || value.ToString() == "")
                value = null;

            if (value == null)
            {
                if (GetType().GetProperty(property).PropertyType.ToString().Contains("Nullable"))
                {
                    GetType().GetProperty(property).SetValue(this, null);
                }
                return;
            }

            Type thisType = GetType().GetProperty(property).PropertyType;

            if ((thisType == typeof(Int16) || thisType == typeof(Int16?)) && (value.GetType() == typeof(Int32) || value.GetType() == typeof(Int32?)))
                value = Convert.ChangeType(value, typeof(Int16));

            //if (thisType.ToString().Contains("Nullable"))
            //    value = Convert.ChangeType(value, Nullable.GetUnderlyingType(thisType));

            if((thisType == typeof(int) || thisType == typeof(int?)) && value.GetType() == typeof(string))
                value = int.Parse((string)value);

            if ((thisType == typeof(bool) || thisType == typeof(bool?)) && value.GetType() == typeof(string))
                value = ((string)value).ToUpper() == "TRUE";

            if ((thisType == typeof(float) || thisType == typeof(float?)) && value.GetType() == typeof(string))
                value = float.Parse((string)value);

            if ((thisType == typeof(DateTime) || thisType == typeof(DateTime?)) && value.GetType() == typeof(string))
                value = DateTime.Parse((string)value);

            GetType().GetProperty(property).SetValue(this, value);
        }

        /// <summary>
        /// Gets the value of the property. Throws an exception with invalid property name. Case sensitive
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual object GetPropertyValue(string property)
        {
            string propertyName = GetPropertyName(property);
            if (propertyName == null)
                throw new ArgumentException("Property " + property + " does not exist in " + GetType().Name);
            return GetType().GetProperty(propertyName).GetValue(this);
        }

        public virtual string GetPropertyName(string property)
        {
            string[] props = GetProperties();
            Int32 idx = props.Select(x => x.ToLower()).ToList().IndexOf(property.ToLower());
            if (idx == -1)
                return null;
            return props[idx];
        }

        /// <summary>
        /// Get array of property names
        /// </summary>
        /// <param name="omit">Property names to omit from the array</param>
        /// <returns></returns>
        public string[] GetProperties(string[] omit = null)
        {
            if (omit != null)
                return Array.FindAll(GetType().GetProperties().Select(x => x.Name).ToArray(), x => !omit.Contains(x));
            return GetType().GetProperties().Select(x => x.Name).ToArray();
        }

        /// <summary>
        /// Get array of property names that correspond with DB column names
        /// </summary>
        /// <param name="omit">Property names to omit from the array</param>
        /// <returns></returns>
        public virtual string[] GetPropertiesDB(string[] omit = null)
        {
            return GetProperties(omit);
        }
        #endregion


        /// <summary>
        /// Get type of the object property. Gets underlying nullable type if applicable
        /// </summary>
        /// <param name="propName"></param>
        /// <returns></returns>
        public virtual Type GetPropertyType(string propName)
        {
            propName = GetPropertyName(propName);
            if (propName == null)
                throw new ArgumentException("Property name does not exist");
            Type propType = GetType().GetProperty(propName).PropertyType;
            if (propType.ToString().Contains("Nullable"))
                propType = Nullable.GetUnderlyingType(propType);

            return propType;
        }

        public bool IsNullable(string propName)
        {
            propName = GetPropertyName(propName);
            if (propName == null)
                throw new ArgumentException("Property name does not exist");
            Type propType = GetType().GetProperty(propName).PropertyType;

            return propType.ToString().Contains("Nullable");
        }

        public DatabaseInterface GetDBInt()
        {
            return _dbInt;
        }

        //public void SetDBInt(DatabaseInterface dbInt)
        //{
        //    _dbInt = dbInt;
        //}

        public string GetTableName()
        {
            return _tableName;
        }
    }
}

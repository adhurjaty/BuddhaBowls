using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.CSharp;
using System.Runtime.Remoting;
using System.IO;
using BuddhaBowls.Services;
using BuddhaBowls.Models;

namespace BuddhaBowls.Test
{
    /// <summary>
    /// Create and read XML files that mock objects from the database.
    /// </summary>
    public static class XmlSerialize
    {
        /// <summary>
        /// Base tag name for XML document.
        /// </summary>
        private const string CONTENT_NODE = "modelContents";

        /// <summary>
        /// Create a mock object (e.g. Model, Model Container, etc.).
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="path">Where to save the XML file</param>
        /// <param name="obj">Object to serialize</param>
        public static void Serialize<T>(string path, T obj)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);
            XmlElement contents = doc.CreateElement(CONTENT_NODE);
            doc.AppendChild(contents);

            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach(PropertyInfo prop in props)
            {
                Type modelType = prop.PropertyType;
                XmlElement element = doc.CreateElement(prop.Name);
                contents.AppendChild(element);

                if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type listType = modelType.GetGenericArguments()[0];
                    dynamic itemList = prop.GetValue(obj);
                    if (itemList != null)
                    {
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            XmlElement el = doc.CreateElement(prop.Name + i.ToString());
                            element.AppendChild(el);
                            AppendObjectXml(ref doc, ref el, itemList[i], listType);
                        }
                    }
                }
                else
                {
                    AppendObjectXml(ref doc, ref element, prop.GetValue(obj), modelType);
                }
            }

            doc.Save(path);
        }

        public static void SerializeModel<T>(string path, T obj)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);
            XmlElement contents = doc.CreateElement(typeof(T).Name);
            doc.AppendChild(contents);

            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                XmlElement element = doc.CreateElement(prop.Name);
                contents.AppendChild(element);

                string text = prop.GetValue(obj) != null ? prop.GetValue(obj).ToString() : "null";
                XmlText txtElement = doc.CreateTextNode(text);
                element.AppendChild(txtElement);
            }

            doc.Save(path);
        }

        public static void SerializeList<T>(string path, List<T> objList)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);
            XmlElement contents = doc.CreateElement(typeof(T).Name);
            doc.AppendChild(contents);

            for (int i = 0; i < objList.Count; i++)
            {
                XmlElement el = doc.CreateElement(typeof(T).Name + i.ToString());
                contents.AppendChild(el);
                AppendObjectXml(ref doc, ref el, objList[i], typeof(T));
            }

            doc.Save(path);
        }

        /// <summary>
        /// Takes an XML path and creates a type of IModelContainer.
        /// </summary>
        /// <typeparam name="T">Data type for Model container (ModelContainer or MockModelContainer)</typeparam>
        /// <param name="path">Path of the XML file to deserialize</param>
        /// <returns>IModelContainer object</returns>
        public static ModelContainer DeserializeModelContainer(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            ModelContainer obj = new ModelContainer();
            PropertyInfo[] props = obj.GetType().GetProperties();

            foreach(PropertyInfo prop in props)
            {
                XmlNode node = doc.GetElementsByTagName(prop.Name)[0];
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elType = prop.PropertyType.GetGenericArguments()[0];
                    var thisList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elType));
                    prop.SetValue(obj, thisList);
                    foreach (XmlNode listElementNode in node.ChildNodes)
                    {
                        var listElementObj = Activator.CreateInstance(elType);
                        ConstructObject(ref listElementObj, listElementNode.ChildNodes);
                        thisList.GetType().GetMethod("Add").Invoke(thisList, new[] { listElementObj });
                    }
                }
                else
                {
                    var modelProperty = Activator.CreateInstance(prop.PropertyType);
                    ConstructObject(ref modelProperty, node.ChildNodes);
                    prop.SetValue(obj, modelProperty);
                }
            }

            return obj;
        }

        /// <summary>
        /// Takes an XML path and creates a type of Model (e.g. dieTest, waferTest, etc.)
        /// </summary>
        /// <typeparam name="T">Data type for Model (dieTest, waferTest, bolo, etc.)</typeparam>
        /// <param name="path">Path of the XML file to deserialize</param>
        /// <returns>Model object</returns>
        public static T DeserializeModel<T>(string path) where T : Model, new()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            T obj = new T();
            PropertyInfo[] props = obj.GetType().GetProperties();

            XmlNode node = doc.GetElementsByTagName(typeof(T).Name)[0];
            ConstructObject(ref obj, node.ChildNodes);

            return obj;
        }

        /// <summary>
        /// Takes an XML path and creates a list of a type of Model.
        /// </summary>
        /// <typeparam name="T">Data type for Model (DieTestSpecs, PixelOperabilitySpec, etc.)</typeparam>
        /// <param name="path">Path of the XML file to deserialize</param>
        /// <returns>List of types of Models</returns>
        public static List<T> DeserializeList<T>(string path) where T : Model, new()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            List<T> thisList = new List<T>();
            XmlNode node = doc.GetElementsByTagName(typeof(T).Name)[0];
            foreach (XmlNode listElementNode in node.ChildNodes)
            {
                T listElementObj = new T();
                ConstructObject(ref listElementObj, listElementNode.ChildNodes);
                thisList.Add(listElementObj);
            }

            return thisList;
        }

        /// <summary>
        /// Takes in the object and converts the properties to XML strings and appends them to the XML file.
        /// </summary>
        /// <param name="doc">Document to append to</param>
        /// <param name="element">Parent tag of the elements being appended</param>
        /// <param name="obj">Object in memory</param>
        /// <param name="type">Type of the object in memory</param>
        private static void AppendObjectXml(ref XmlDocument doc, ref XmlElement element, dynamic obj, Type type)
        {
            PropertyInfo[] props = type.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                XmlElement el = doc.CreateElement(prop.Name);
                element.AppendChild(el);
                string text = prop.GetValue(obj) != null ? prop.GetValue(obj).ToString() : "null";
                XmlText txtElement = doc.CreateTextNode(text);
                el.AppendChild(txtElement);
            }
        }

        /// <summary>
        /// Fill Model object with values in nodeList.
        /// </summary>
        /// <remarks>Assumes obj is of type Model and injects a dbInt of user's choice.</remarks>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="obj">Object with properties to populate, assumed to be Model child</param>
        /// <param name="nodeList">Xml elements describing the list</param>
        /// <param name="dbInt">Injected database interface</param>
        private static void ConstructObject<T>(ref T obj, XmlNodeList nodeList)
        {
            foreach (XmlNode itemPropNode in nodeList)
            {
                Type itemType = (Type)obj.GetType().GetMethod("GetPropertyType").Invoke(obj, new[] { itemPropNode.Name });
                var val = itemPropNode.InnerText != "null" ? Convert.ChangeType(itemPropNode.InnerText, itemType) : null;
                obj.GetType().GetMethod("SetProperty").Invoke(obj, new[] { itemPropNode.Name, val });
            }
        }
    }
}

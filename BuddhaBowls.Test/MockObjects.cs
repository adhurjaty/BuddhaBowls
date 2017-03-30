using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Test
{
    /// <summary>
    /// Generate Models from XML and vice versa. Used for testing.
    /// </summary>
    public static class MockObjects
    {
        /// <summary>
        /// Path where all the XML files are stored.
        /// </summary>
        public static string OBJECT_PATH = @"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddhaBowls.Test\Objects";

        /// <summary>
        /// Retrieve a list of pixel operability specs from an XML file.
        /// </summary>
        /// <param name="bolo">Bolo type (e.g. 1301, 1403, 1407, etc.)</param>
        /// <returns>List of pixel operability specs</returns>
        //public static List<PixelOperabilitySpec> GetPixelSpecs(string bolo)
        //{
        //    return XmlSerialize.DeserializeList<PixelOperabilitySpec>(Path.Combine(OBJECT_PATH, bolo + "PixelOperabilitySpec.xml"));
        //}

        /// <summary>
        /// Retrieve a Model container from an XML file.
        /// </summary>
        /// <param name="waferId">Database ID in WaferTest table</param>
        /// <returns>Model container</returns>
        public static ModelContainer GetModelContainer(Int32 waferId)
        {
            return XmlSerialize.DeserializeModelContainer(Path.Combine(OBJECT_PATH, waferId.ToString() + "Models.xml"));
        }

        /// <summary>
        /// todo / Create a mock DieTest object from an XML file.
        /// </summary>
        /// <param name="dieId">Database ID in DieTest table(?)</param>
        /// <returns>Mock DieTest object</returns>
        public static InventoryItem GetInventoryItem(string name)
        {
            return XmlSerialize.DeserializeModel<InventoryItem>(Path.Combine(OBJECT_PATH, name + ".xml"));
        }

        public static Recipe GetRecipe(string name)
        {
            return XmlSerialize.DeserializeModel<Recipe>(Path.Combine(OBJECT_PATH, name + ".xml"));
        }

        public static T GetFromPath<T>(string path) where T : Model, new()
        {
            return XmlSerialize.DeserializeModel<T>(path);
        }

        public static void SaveModelContainer(ModelContainer models)
        {
            XmlSerialize.Serialize(Path.Combine(OBJECT_PATH, "Models.xml"), models);
        }

        public static void SaveInventoryItem(InventoryItem invItem)
        {
            XmlSerialize.SerializeModel(Path.Combine(OBJECT_PATH, invItem.Name + ".xml"), invItem);
        }

        public static void SaveInventoryItem(InventoryItem invItem, string subfolder)
        {
            XmlSerialize.SerializeModel(Path.Combine(OBJECT_PATH, subfolder, invItem.Name + ".xml"), invItem);
        }

        public static void SaveRecipe(Recipe recipe)
        {
            XmlSerialize.SerializeModel(Path.Combine(OBJECT_PATH, recipe.Name + ".xml"), recipe);
        }

        //public static void SaveSpecsList<T>(List<T> specs, string bolo)
        //{
        //    XmlSerialize.SerializeList<T>(Path.Combine(OBJECT_PATH, bolo + typeof(T).Name + ".xml"), specs);
        //}
    }
}

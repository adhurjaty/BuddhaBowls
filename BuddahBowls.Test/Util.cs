using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddahBowls.Test
{
    public static class Util
    {
        public static string CopyTable(string dbFilePath)
        {
            string filename = Path.GetFileNameWithoutExtension(dbFilePath);
            string directory = Path.GetDirectoryName(dbFilePath);
            string copyFilePath = Path.Combine(directory, filename + "Copy.csv");

            if (File.Exists(copyFilePath))
            {
                File.Delete(copyFilePath);
            }

            File.Copy(dbFilePath, copyFilePath);

            return copyFilePath;
        }
    }
}

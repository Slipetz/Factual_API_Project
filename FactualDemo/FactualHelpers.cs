using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FactualDemo
{
    static class FactualHelpers
    {
        public const string FactualKey = "tdyOQ7pUNeM1PkncC4pvZJfC2zx7e88t9hRVyuDC";
        public const string FactualSecret = "pE3mk8u7DceJ4wTyKyH4celwOATuGLw8MEeKGrCz";

        public static void WriteListToFile(List<string> fileList, string path)
        {
            StreamWriter listWriter = new StreamWriter(path);
            foreach (string fileName in fileList)
            {
                listWriter.WriteLine(fileName);
            }
            listWriter.Close();
        }

        public static void WriteJsonToFile(FactualJson inMemoryDB, string path)
        {
            StreamWriter jsonSaver = new StreamWriter(path);
            inMemoryDB.response.included_rows = inMemoryDB.response.data.Count();
            string jsonString = JsonConvert.SerializeObject(inMemoryDB);
            jsonSaver.Write(jsonString);
            jsonSaver.Close();
        }

        public static FactualPoint[] FindEquals(string property, string value, FactualJson inMemoryDb)
        {
            List<FactualPoint> returnArray = new List<FactualPoint>();
            foreach (FactualPoint fp in inMemoryDb.response.data)
            {
                if (fp.GetType().GetProperty(property).GetValue(fp) != null && fp.GetType().GetProperty(property).GetValue(fp).Equals(value))
                {
                    returnArray.Add(fp);
                }
            }
            return returnArray.ToArray();
        }

        public static FactualPoint[] FindContains(string property, string value, FactualJson inMemoryDb)
        {
            List<FactualPoint> returnArray = new List<FactualPoint>();
            foreach (FactualPoint fp in inMemoryDb.response.data)
            {
                if (fp.GetType().GetProperty(property).GetValue(fp) != null && fp.GetType().GetProperty(property).GetValue(fp).ToString().Contains(value))
                {
                    returnArray.Add(fp);
                }
            }
            return returnArray.ToArray();
        }

        public static void ClearCachedFiles()
        {
            if (File.Exists("FactualCacheList.txt"))
            {
                StreamReader fileReader = new StreamReader("FactualCacheList.txt");
                while (!fileReader.EndOfStream)
                {
                    string fileName = fileReader.ReadLine();
                    if (fileName != null) File.Delete(fileName);
                }
                fileReader.Close();
            }
        }

        public static FactualJson GetInMemoryFile()
        {
            StreamReader fileLoader = new StreamReader("FactualData.txt");
            string jsonFile = fileLoader.ReadToEnd();
            fileLoader.Close();
            return JsonConvert.DeserializeObject<FactualJson>(jsonFile);
        }
    }
}

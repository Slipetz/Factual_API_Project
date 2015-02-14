using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace FactualAPIProject1
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

        public static void WriteDBToFile(FactualJson inMemoryDB, string path)
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
            if (File.Exists("FactualData.txt"))
            {
                StreamReader fileLoader = new StreamReader("FactualData.txt");
                string jsonFile = fileLoader.ReadToEnd();
                fileLoader.Close();
                return JsonConvert.DeserializeObject<FactualJson>(jsonFile);
            }
            else
                return new FactualJson();
        }

        public static void WriteJSONToFile(string fileName, string factualInfo)
        {
            var fileSaver = new StreamWriter(fileName);
            fileSaver.WriteLine(factualInfo);
            fileSaver.Close();
        }

        public static FactualPoint[] ParseJsonData(FactualJson inMemoryDb, FactualJson factualData)
        {
            var newPoints = new List<FactualPoint>(inMemoryDb.response.data);
            //If we already have an inMemoryDB, then we replace this stuff with the new data
            foreach (var data in factualData.response.data)
            {
                //Check to see if we can compare the factual ID and see if any of them match
                if ((FindEquals("factual_id", data.factual_id, inMemoryDb).Any()))
                {
                    for (int i = 0; i < inMemoryDb.response.data.Count(); i++)
                    {
                        if (inMemoryDb.response.data[i].factual_id.Equals(data.factual_id))
                            inMemoryDb.response.data[i] = data;
                    }
                }
                else
                {
                    newPoints.Add(data);
                }
            }
            //Re-attach the above list back into the data dump/inMemoryDB
            return newPoints.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactualDriver;
using FactualDriver.Filters;
using Newtonsoft.Json;

namespace FactualAPIProject1
{
    class Program
    {
        public const bool isOnline = true;

        public static readonly Coord[] latLongArray =
        {
            //Fanshawe College
            new Coord {lat = 43.0120, lon = -81.2003}, 

            //Western University
            new Coord {lat = 43.009953, lon = -81.273613},

            //Westmount Mall
            new Coord {lat = 42.9479549, lon = -81.2924851},

            //Masonville Mall (overlap with Western)
            new Coord {lat = 42.985982, lon = -81.246399},
            
            //My house - Overlap with Westmount
            new Coord {lat = 42.942209299999995, lon = -81.27783010000002}
        };

        public static readonly Dictionary<string, string> testDataEquals = new Dictionary<string, string>
        {
            {"postcode", "N6J 5N5"},
            {"name", "Tim Hortons"},
            {"country", "us"},
            {"locality", "London"},
            {"website", "http://www.fanshawec.ca"}
        };

        public static readonly Dictionary<string, string> testDataContains = new Dictionary<string, string>()
        {
            {"postcode", "N6J"},
            {"name", "Fanshawe"},
            {"locality", "Lon"},
            {"website", "https:"},
            {"address", "Richmond"}
        };

        static void Main(string[] args)
        {
            var inMemoryDb = FactualHelpers.GetInMemoryFile();

            if (isOnline)
            {
                Console.WriteLine("Online! Beginning Factual API Process");

                var factual = new Factual(FactualHelpers.FactualKey, FactualHelpers.FactualSecret)
                {
                    ConnectionTimeout = 100000,
                    ReadTimeout = 300000
                };

                //List that will hold the fileNames we will parse shortly to compile our data
                var fileList = new List<string>();
                int testCount = 0; 

                Console.WriteLine("Querying Factual to get updated data...");
                //Parse factual data with the test data
                foreach (var latLong in latLongArray)
                {
                    Console.WriteLine("Contacting Factual for Test " + (++testCount) + " with co-ordinates: Lat: " + latLong.lat + " Long: " + latLong.lon);
                    var factualInfo = factual.Fetch("places",
                        new Query().WithIn(new Circle(latLong.lat, latLong.lon, 5000)).Limit(35));

                    Console.WriteLine("Writing Factual Data to disk...");
                    var fileName = "FactualData-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt";
                    fileList.Add(fileName);
                    FactualHelpers.WriteJSONToFile(fileName, factualInfo);

                    //This gives us the Object version of our JSON
                    var factualData = JsonConvert.DeserializeObject<FactualJson>(factualInfo);
                    Console.WriteLine("Parsing Factual data into the In-Memory DB");
                    if (inMemoryDb.response == null || inMemoryDb.response.data.Count() < 0)
                    {
                        inMemoryDb = factualData; //If inMemoryDB doesn't exist, give it the new data
                    }
                    else
                    {
                        inMemoryDb.response.data = FactualHelpers.ParseJsonData(inMemoryDb, factualData);
                    }
                } //End of Factual ForEach
   
                //Remove the old cached files so we do not have an overlap in our Factual Data
                FactualHelpers.ClearCachedFiles();

                //When we reach the end of the ForEach - We have our list of cached files, and a complete set of our inMemory code
                //Now, we need to create a master list which contains the name of each file should we need a backup
                Console.WriteLine("Creating Cache List");
                FactualHelpers.WriteListToFile(fileList, "FactualCacheList.txt");

                Console.WriteLine("Writing In-Memory DB Copy");
                FactualHelpers.WriteDBToFile(inMemoryDb, "FactualData.txt");

                Console.WriteLine("Online information recorded. Testing Commencing!");
            }
            else
            {
                if (inMemoryDb.response == null)
                {
                    Console.WriteLine("Uh oh! We are off-line with no back-up for our database! Attempting to parse old factual data");
                    if (File.Exists("FactualCacheList.txt"))
                    {
                        var fileLoader = new StreamReader("FactualCacheList.txt");
                        while (!fileLoader.EndOfStream)
                        {
                            var filePath = fileLoader.ReadLine();
                            if (!File.Exists(filePath)) continue;
                            var cacheFile = new StreamReader(filePath);
                            var factualData = JsonConvert.DeserializeObject<FactualJson>(cacheFile.ReadToEnd());
                            Console.WriteLine("Parsing Factual data into the In-Memory DB");
                            if (inMemoryDb.response == null || inMemoryDb.response.data.Count() < 0)
                            {
                                inMemoryDb = factualData; //If inMemoryDB doesn't exist, give it the new data
                            }
                            else
                            {
                                inMemoryDb.response.data = FactualHelpers.ParseJsonData(inMemoryDb, factualData);
                            }
                        }
                        fileLoader.Close();
                    }
                    else
                    {
                        Console.WriteLine("Uh oh! There is no cache available! Please try again when you have an internet connection!");
                        Environment.Exit(-1);
                    }
                }
                Console.WriteLine("Offline with In-Memory Database Loaded. Testing Commencing");
            }

            Console.WriteLine("*************************************************");
            Console.WriteLine("******************Testing Phase******************");
            Console.WriteLine("*************************************************");
            Console.WriteLine();

            FindEqualsTest(inMemoryDb);
            FindContainsTest(inMemoryDb);
        }

        private static void FindContainsTest(FactualJson inMemoryDb)
        {
            Console.WriteLine("Testing Contains\n**********");

            foreach (var kvp in testDataContains)
            {
                Console.WriteLine("Testing FindContains with Property: " + kvp.Key + " Value: " + kvp.Value);
                Console.WriteLine("Records Found: " + FactualHelpers.FindContains(kvp.Key, kvp.Value, inMemoryDb).Count());
            }
            Console.WriteLine();
        }

        private static void FindEqualsTest(FactualJson inMemoryDb)
        {
            Console.WriteLine("Testing FindEquals\n**********");

            foreach (var kvp in testDataEquals)
            {
                Console.WriteLine("Testing FindEquals with Property: " + kvp.Key + " Value: " + kvp.Value);
                Console.WriteLine("Records Found: " + FactualHelpers.FindEquals(kvp.Key, kvp.Value, inMemoryDb).Count());
            }
            Console.WriteLine();
        }

    }
}

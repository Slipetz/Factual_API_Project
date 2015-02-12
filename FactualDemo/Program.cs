using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using FactualDriver;
using FactualDriver.Filters;

namespace FactualDemo
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
            var inMemoryDb = new FactualJson();

            //Parse for file first. If file exists, then we need to have that set up our "in-memory" DB
            if (File.Exists("FactualData.txt"))
            {
                inMemoryDb = FactualHelpers.GetInMemoryFile();
            }
            if (isOnline)
            {
                Console.WriteLine("Online! Beginning Factual API Process");
                //Since we are online, we can clear our old cache file in preparation for the new files
                FactualHelpers.ClearCachedFiles();

                var factual = new Factual(FactualHelpers.FactualKey, FactualHelpers.FactualSecret)
                {
                    ConnectionTimeout = 100000,
                    ReadTimeout = 300000
                };

                //List that will hold the fileNames we will parse shortly to compile our data
                var fileList = new List<string>();

                int count = 0;

                Console.WriteLine("Querying Factual to get updated data...");
                //Parse factual data with the test data
                foreach (var latLong in latLongArray)
                {
                    Console.WriteLine("Contacting Factual for Test " + (++count) + " with co-ordinates: Lat: " + latLong.lat + " Long: " + latLong.lon);
                    var factualInfo = factual.Fetch("places",
                        new Query().WithIn(new Circle(latLong.lat, latLong.lon, 5000)).Limit(35));

                    Console.WriteLine("Writing Factual Data to disk...");
                    //This will return us JSON. We want to save these files in JSON format to parse into our "in-memory DB"
                    var fileName = "FactualData-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt";
                    fileList.Add(fileName);
                    var fileSaver = new StreamWriter(fileName);
                    fileSaver.WriteLine(factualInfo);
                    fileSaver.Close();

                    //This gives us the Object version of our JSON
                    var factualData = JsonConvert.DeserializeObject<FactualJson>(factualInfo);
                    Console.WriteLine("Parsing Factual data into the In-Memory DB");
                    if(inMemoryDb.response == null || inMemoryDb.response.data.Count() < 0)
                    {
                        inMemoryDb = factualData; //If inMemoryDB doesn't exist, give it the new data
                    }
                    else
                    {
                        var newPoints = new List<FactualPoint>(inMemoryDb.response.data);
                        //If we already have an inMemoryDB, then we replace this stuff with the new data
                        foreach (FactualPoint data in factualData.response.data)
                        {
                            //Check to see if we can compare the factual ID and see if any of them match
                            if((FactualHelpers.FindEquals("factual_id", data.factual_id, inMemoryDb).Any()))
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
                        inMemoryDb.response.data = newPoints.ToArray();
                    }
                } //End of Factual ForEach
   
                //When we reach the end of the ForEach - We have our list of cached files, and a complete set of our inMemory code
                //Now, we need to create a master list which contains the name of each file should we need a backup
                Console.WriteLine("Creating Cache List");
                FactualHelpers.WriteListToFile(fileList, "FactualCacheList.txt");

                Console.WriteLine("Writing In-Memory DB Copy");
                //After we have accomplished this, we can begin testing below!
                FactualHelpers.WriteJsonToFile(inMemoryDb, "FactualData.txt");

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
                            var factualData = JsonConvert.DeserializeObject<FactualJson>(fileLoader.ReadToEnd());
                            Console.WriteLine("Parsing Factual data into the In-Memory DB");
                            if (inMemoryDb.response == null || inMemoryDb.response.data.Count() < 0)
                            {
                                inMemoryDb = factualData; //If inMemoryDB doesn't exist, give it the new data
                            }
                            else
                            {
                                var newPoints = new List<FactualPoint>(inMemoryDb.response.data);
                                //If we already have an inMemoryDB, then we replace this stuff with the new data
                                foreach (var data in factualData.response.data)
                                {
                                    //Check to see if we can compare the factual ID and see if any of them match
                                    if ((FactualHelpers.FindEquals("factual_id", data.factual_id, inMemoryDb).Any()))
                                    {
                                        for (var i = 0; i < inMemoryDb.response.data.Count(); i++)
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
                                inMemoryDb.response.data = newPoints.ToArray();
                            } //End of Factual ForEach
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

            Console.WriteLine("Testing FindEquals\n**********");

            foreach (var kvp in testDataEquals)
            {
                Console.WriteLine("Testing FindEquals with Property: " + kvp.Key + " Value: " + kvp.Value);
                Console.WriteLine("Records Found: " + FactualHelpers.FindEquals(kvp.Key, kvp.Value, inMemoryDb).Count());
            }
            Console.WriteLine();
            Console.WriteLine("Testing Contains\n**********");

            foreach (var kvp in testDataContains)
            {
                Console.WriteLine("Testing FindContains with Property: " + kvp.Key + " Value: " + kvp.Value);
                Console.WriteLine("Records Found: " + FactualHelpers.FindContains(kvp.Key, kvp.Value, inMemoryDb).Count());
            }
            
        }


    }
}

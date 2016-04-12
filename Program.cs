using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.Entity;
using System.Data.EntityClient;
using Microsoft.VisualBasic.FileIO;
using StackExchange.Redis;

namespace HorseArchives
{
    class Program
    {
        static void Main(string[] args)
        {          
            IList<string[]> listFilteredFields = new List<string[]>();
            IList<string[]> listSummaryRecords = new List<string[]>();
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.csv");
            foreach (var filename in files)
            {
                TextFieldParser listResult = new TextFieldParser(filename);
                listResult.TextFieldType = FieldType.Delimited;
                listResult.SetDelimiters(",");
                while (!listResult.EndOfData)
                {
                    string[] fields = listResult.ReadFields();
                    if (fields[3] == "GB" && fields[7] != "TO BE PLACED" && fields[7].IndexOf("Top") < 0 && fields[5] != "Daily" && fields[7].IndexOf("TBP") < 0 && fields[17] == "PE")
                    {
                        listFilteredFields.Add(fields);
                    }
                }
            }
            listFilteredFields = listFilteredFields.OrderBy(x => Convert.ToDateTime(x[6])).Select(x => x).ToList<string[]>();
            foreach (var row in listFilteredFields)
            {
                var count = listSummaryRecords.Where(x => x[10] == row[10] && x[1] == row[1]).Count();
                if (count == 0)
                {
                    var currentEvent = listFilteredFields.Where(x => x[10] == row[10] && x[1] == row[1]).Select(x => x).ToList<string[]>();
                    var listOrderedEvents = currentEvent.OrderByDescending(x => Convert.ToDateTime(x[14])).ToList<string[]>();
                    listSummaryRecords.Add(listOrderedEvents.First());
                }
            }
            //Write records into MS SQL
            AddRecordsToDB(listSummaryRecords);
            //Write records into Redis
            IList<RaceRedis> listRaces = AddRecordsToRedis(listSummaryRecords);
            SaveRecordsToRedis(listRaces);
           
            Console.ReadLine();
        }

        /// <summary>
        /// Add summary records into MS SQL
        /// </summary>
        private static void AddRecordsToDB(IList<string[]> list)
        {
            List<int> listId = new List<int>();
            foreach(var record in list)
            {
                using (HorseRacingEntities2 context = new HorseRacingEntities2())
                {
                    var count = listId.Where(x => x == Convert.ToInt32(record[1])).Count();
                    if (count == 0)
                    {
                        listId.Add(Convert.ToInt32(record[1]));                        
                        context.Races.Add(new Race { RaceID = Convert.ToInt32(record[1]), City = record[5], DateBegin = Convert.ToDateTime(record[6].Replace("-", "/")), TypeRace = record[7] });
                        context.SaveChanges();
                    }                   
                    context.Runners.Add(new Runner { RunnerID = 33, RaceID = Convert.ToInt32(record[1]), RunnerName = record[10], Odds = Convert.ToDouble(record[11].Replace(".", ",")), DateLatestMatches = Convert.ToDateTime(record[14].Replace("-", "/")), Win = (record[16].IndexOf("1") >= 0 ? true : false) });
                    context.SaveChanges();
                }

            }
        }

        /// <summary>
        /// Add summary records in List for further saving into Redis
        /// </summary>
        private static IList<RaceRedis> AddRecordsToRedis(IList<string[]> list)
        {
             IList<int> listId = new List<int>();
             IList<RaceRedis> listRace = new List<RaceRedis>();
             foreach (var record in list)
             {
                 var count = listId.Where(x => x == Convert.ToInt32(record[1])).Count();
                 if (count == 0)
                 {
                     listId.Add(Convert.ToInt32(record[1]));                   
                     RaceRedis race = new RaceRedis { RaceID = Convert.ToInt32(record[1]), City = record[5], DateBegin = Convert.ToDateTime(record[6].Replace("-", "/")), TypeRace = record[7] };                    
                     foreach (var row in list)
                     {
                         if (row[1] == record[1])
                         {                            
                             RunnerRedis runner = new RunnerRedis { RunnerID = Convert.ToInt32(row[9]), RunnerName = row[10], Odds = Convert.ToDouble(row[11].Replace(".", ",")), DateLatestMatches = Convert.ToDateTime(row[14].Replace("-", "/")), Win = (row[16].IndexOf("1") >= 0 ? true : false) };
                             race.runners.Add(runner);   
                         }
                     }
                     listRace.Add(race);
                 }                
             }
             return listRace;
        }

        /// <summary>
        /// Save Race records into Redis
        /// </summary>
        private static void SaveRecordsToRedis(IList<RaceRedis> listRaces)
        {
            int numberRace = 1;
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            foreach (var race in listRaces)
            {
                if (race.runners.Count > 6)
                {
                    int countRunners =race.runners.Where(x=>x.Win==true).Count();
                    if (countRunners == 1)
                    {
                        // Put an Race object into the cache
                        db.Set("Race" + numberRace.ToString(), race);
                        numberRace++;
                    }
                }
            }
        }

    }
}

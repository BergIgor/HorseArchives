using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorseArchives
{
    [Serializable]
    class RaceRedis
    {
        public int RaceID { get; set; }
        public string City { get; set; }
        public System.DateTime DateBegin { get; set; }
        public string TypeRace { get; set; }

        public IList<RunnerRedis> runners = null;

        public RaceRedis()
        {
            runners = new List<RunnerRedis>();
        }
    }
}

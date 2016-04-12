using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorseArchives
{
    [Serializable]
    class RunnerRedis
    {
        public int RunnerID { get; set; }
        public string RunnerName { get; set; }
        public double Odds { get; set; }
        public System.DateTime DateLatestMatches { get; set; }
        public bool Win { get; set; }
    }
}

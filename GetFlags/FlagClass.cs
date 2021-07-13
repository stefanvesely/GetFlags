using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetFlags
{
    class FlagClass
    {
        public string IncidentType, Product;
        public decimal FlagTransAmount, FlagTankDifference;
        public List<int> Tanks = new List<int>();
        public List<int> Pumps = new List<int>();
        public List<int> Hoses = new List<int>();
        public DateTime Flagtime;
    }
}

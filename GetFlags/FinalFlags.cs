using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetFlags
{
    class FinalFlags
    {
        public string FlagType, FlagFuel;
        public decimal FlagRating, ActualGainLoss, GainLossPer100, TransactionGainLoss, TransactionalTankDiff, TransAmount;
        public int HoseNum, PumpNum, IncidentCount;
        public List<int> Tanks = new List<int>();

    }
}

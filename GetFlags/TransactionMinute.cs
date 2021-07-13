using System;
using System.Collections.Generic;

namespace GetFlags
{
    internal class TransactionMinute
    {
        public string Product;
        public DateTime Transminute;
        public List<int> Pumps;
        public List<int> Hoses;
        public decimal TotalTransVolumePump;
        public decimal TankTotalVolume;
        public decimal TankDifference;
        
        public List<int> TankNumber;
        
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace GetFlags
{
    public partial class Form1 : Form
    {

        public DataTable Pumps = new DataTable();
        public DataTable Tanks = new DataTable();
        private List<Pumps> PumpList = new List<Pumps>();
        private List<Tanks> TankList = new List<Tanks>();
        private List<ConnectionClass> Connection = new List<ConnectionClass>();
        private List<HosesTanks> ConfigList = new List<HosesTanks>();
        private List<TransactionMinute> AllATGTransactions = new List<TransactionMinute>();
        private List<TransactionMinute> TestClass = new List<TransactionMinute>();
        private List<string> products = new List<string>();
        private List<FlagClass> InitialFlags = new List<FlagClass>();
        private List<FinalFlags> FinalFlags = new List<FinalFlags>();
        private List<FinalFlags> SingleFlags = new List<FinalFlags>();
        private List<Pumps> PL = new List<Pumps>();
        decimal decTotalGain, decTotalLoss, decTotalPSales, decTotalDSales;

        public Form1()
        {
            InitializeComponent();
            GetConfig();
        }

        public DateTime Truncate(DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // do not modify "guard" values
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public void GetConfig()
        {
            HosesTanks tank = new HosesTanks();
            tank.TankNumber = 1;
            tank.TankProduct = "ULP95";
            ConfigList.Add(tank);
            tank.TankNumber = 2;
            tank.TankProduct = "ULP95";
            ConfigList.Add(tank);
            tank.TankNumber = 3;
            tank.TankProduct = "D50";
            ConfigList.Add(tank);
            tank.TankNumber = 4;
            tank.TankProduct = "ULP5";
            ConfigList.Add(tank);

            for (int i = 1; i < 9; i++)
            {
                ConnectionClass CC = new ConnectionClass();
                CC.PumpNumber = i;
                CC.Product = "ULP95";
                CC.HoseNumber = 1;
                List<int> tanks = new List<int>();
                tanks.Add(1);
                tanks.Add(2);
                tanks.Add(4);
                CC.Tanks = tanks;
                Connection.Add(CC);
            }

            for (int i = 1; i < 9; i++)
            {
                ConnectionClass C2 = new ConnectionClass();
                C2.PumpNumber = i;
                C2.Product = "D50";
                C2.HoseNumber = 2;
                List<int> Tanks = new List<int>();
                Tanks.Add(3);
                C2.Tanks = Tanks;
                Connection.Add(C2);
            }
            ConnectionClass c3 = new ConnectionClass();
            c3.PumpNumber = 9;
            c3.Product = "D50";
            c3.HoseNumber = 1;
            Connection.Add(c3);
        }

        private void GetFlags_Click(object sender, EventArgs e)
        {
            lstbxDatareprisentaiton.Items.Clear();
            lstbxSTF.Items.Clear();
            listboxminutes.Items.Clear();
            lstbxFlagData.Items.Clear();
            lstbxFinalScore.Items.Clear();
            AllATGTransactions.Clear();
            InitialFlags.Clear();
            PumpList.Clear();
            TankList.Clear();
            Pumps.Clear();
            Tanks.Clear();
            TestClass.Clear();
            FinalFlags.Clear();
            SingleFlags.Clear();
            DateTime dtStart = dtPicker1.Value;
            TimeSpan tsZeroHour = new TimeSpan(0, 0, 0);
            dtStart = dtStart.Date + tsZeroHour;
            DateTime dtend = dtStart;
            dtend = dtend.AddDays(1);
            //MessageBox.Show(dtStart.ToString() + " Start  " + dtend.ToString() + " End");
            string sConnString = "" ;
            using (SqlConnection sqlConnection = new SqlConnection(sConnString))
            {
                SqlCommand sCommand = new SqlCommand("SELECT * FROM PtsReading WHERE TakenAt BETWEEN '" + dtStart + "' AND '" + dtend + "'", sqlConnection);
                SqlDataAdapter sqlDA = new SqlDataAdapter();
                sqlDA.SelectCommand = sCommand;
                sqlDA.Fill(Pumps);
            }
            foreach (DataRow PumpRow in Pumps.Rows)
            {
                Pumps pump = new Pumps();
                DateTime dt = DateTime.Parse(PumpRow["TakenAt"].ToString());
                dt = Truncate(dt, TimeSpan.FromMilliseconds(1)); // Truncate to whole ms
                dt = Truncate(dt, TimeSpan.FromSeconds(1)); // Truncate to whole second
                dt = Truncate(dt, TimeSpan.FromMinutes(1));
                pump.TakenAt = dt;
                pump.Volume = decimal.Parse(PumpRow["Volume"].ToString());
                pump.Pump = int.Parse(PumpRow["Pump"].ToString());
                pump.Hose = int.Parse(PumpRow["Hose"].ToString());
                PumpList.Add(pump);
                // PL.Add(pump);
            }
            using (SqlConnection sqlConnection = new SqlConnection(sConnString))
            {
                SqlCommand sCommand = new SqlCommand("SELECT * FROM AtgReading WHERE TakenAt BETWEEN '" + dtStart + "' AND '" + dtend + "'", sqlConnection);
                SqlDataAdapter sqlDA = new SqlDataAdapter();
                sqlDA.SelectCommand = sCommand;
                sqlDA.Fill(Tanks);
            }
            foreach (DataRow TankRow in Tanks.Rows)
            {
                Tanks tank = new Tanks();
                DateTime dt = DateTime.Parse(TankRow["TakenAt"].ToString());
                dt = Truncate(dt, TimeSpan.FromMilliseconds(1)); // Truncate to whole ms
                dt = Truncate(dt, TimeSpan.FromSeconds(1)); // Truncate to whole second
                dt = Truncate(dt, TimeSpan.FromMinutes(1));
                tank.TakenAt = dt;
                tank.VolumeP = decimal.Parse(TankRow["VolumeP"].ToString());
                tank.TankNumber = int.Parse(TankRow["TankNumber"].ToString());
                TankList.Add(tank);
            }
            GetConfig();
            GetProductsTanks();
            GetproductsPumps();
            CombineTanksIntoMinutes();
            CombineThePetrol();
            GetDifferences();
            EliminateFalseTanks();
            GetInitialFlags();
            GetFinalFlags();
            GetFinalSingalFlags();
            GetDeliveries();
            nudPump.Enabled = true;
            btnGetPump.Enabled = true;
            int iNUDmax = 0;
            int iNUDmin = 1000;
            foreach (string p in products)
                lstbxFuel.Items.Add(p);
            foreach (TransactionMinute TM in AllATGTransactions)
            {
                if (TM.Pumps != null)
                {
                    foreach (int i in TM.Pumps)
                    {
                        if (i != 0)
                        {
                            if (i > iNUDmax)
                            {
                                iNUDmax = i;
                            }
                            if (i < iNUDmin)
                            {
                                iNUDmin = i;
                            }
                        }
                    }
                }
            }
            nudPump.Minimum = iNUDmin;
            nudPump.Maximum = iNUDmax;
        }

        public void GetProductsTanks()
        {
            foreach (Tanks SingleTank in TankList)
            {
                foreach (ConnectionClass ConClass in Connection)
                {
                    if (ConClass.Tanks.Contains(SingleTank.TankNumber))
                    {
                        SingleTank.Product = ConClass.Product;
                        if (!products.Contains(ConClass.Product))
                        {
                            products.Add(ConClass.Product);
                        }
                    }
                }
            }
        }

        public void GetproductsPumps()
        {
            foreach (Pumps pump in PumpList)
            {
                foreach (ConnectionClass con in Connection)
                {
                    if (con.PumpNumber == pump.Pump)
                    {
                        if (con.HoseNumber == pump.Hose)
                        {
                            pump.Product = con.Product;
                        }
                    }
                }
            }
            PL.AddRange(PumpList);
        }

        public void CombineTanksIntoMinutes()
        {
            foreach (string product in products)
            {
                List<Tanks> WorkingTanks = new List<Tanks>();
                foreach (Tanks tank in TankList)
                {
                    if (tank.Product == product)
                    {
                        WorkingTanks.Add(tank);
                    }
                }
                int WorkingCount = WorkingTanks.Count;
                while (WorkingTanks.Count > 0)
                {
                    if (AllATGTransactions.Count == 0)
                    {
                        TransactionMinute NewMinute = new TransactionMinute();
                        NewMinute.Transminute = WorkingTanks[0].TakenAt;
                        NewMinute.Product = WorkingTanks[0].Product;
                        if (NewMinute.TankNumber == null)
                        {
                            List<int> list = new List<int>();
                            list.Add(WorkingTanks[0].TankNumber);
                            NewMinute.TankNumber = list;
                        }
                        else
                        {
                            NewMinute.TankNumber.Add(WorkingTanks[0].TankNumber);
                        }
                        NewMinute.TankTotalVolume = WorkingTanks[0].VolumeP;
                        NewMinute.TankDifference = 0;

                        AllATGTransactions.Add(NewMinute);

                        TankList.Remove(WorkingTanks[0]);
                    }
                    else
                    {
                        if (WorkingTanks[0].TakenAt == AllATGTransactions[AllATGTransactions.Count - 1].Transminute)
                        {
                            if (!AllATGTransactions[AllATGTransactions.Count - 1].TankNumber.Contains(WorkingTanks[0].TankNumber))
                            {
                                if (AllATGTransactions[AllATGTransactions.Count - 1].Product == WorkingTanks[0].Product)
                                {
                                    AllATGTransactions[AllATGTransactions.Count - 1].TankNumber.Add(WorkingTanks[0].TankNumber);
                                    AllATGTransactions[AllATGTransactions.Count - 1].TankTotalVolume = AllATGTransactions[AllATGTransactions.Count - 1].TankTotalVolume + WorkingTanks[0].VolumeP;
                                }
                            }
                            else
                            {
                                WorkingTanks.Remove(WorkingTanks[0]);
                            }
                        }
                        else
                        {
                            TransactionMinute NewMinute = new TransactionMinute();
                            NewMinute.Transminute = WorkingTanks[0].TakenAt;
                            NewMinute.Product = WorkingTanks[0].Product;
                            List<int> io = new List<int>();
                            NewMinute.TankNumber = io;
                            NewMinute.TankNumber.Add(WorkingTanks[0].TankNumber);
                            NewMinute.TankTotalVolume = WorkingTanks[0].VolumeP;
                            NewMinute.TankDifference = 0;

                            AllATGTransactions.Add(NewMinute);
                            //CurrentMinute = TankList[0].TakenAt;
                            WorkingTanks.Remove(WorkingTanks[0]);
                        }
                    }
                }
            }
        }

        public void CombineThePetrol()
        {
            int WorkingCount = PumpList.Count;
            while (PumpList.Count > 0)
            {
                DateTime CurrentMinute = PumpList[0].TakenAt;
                foreach (TransactionMinute EachMinute in AllATGTransactions)
                {
                    if (PumpList.Count > 0)
                    {
                        if (EachMinute.Transminute == PumpList[0].TakenAt)
                        {
                            if (EachMinute.Product == PumpList[0].Product)
                            {
                                EachMinute.TotalTransVolumePump = EachMinute.TotalTransVolumePump + PumpList[0].Volume;
                                if (EachMinute.TotalTransVolumePump > 0)
                                {
                                    //EachMinute.TotalTransVolumePump = EachMinute.TotalTransVolumePump;
                                }

                                if (EachMinute.Hoses == null)
                                {
                                    List<int> ii = new List<int>();
                                    ii.Add(PumpList[0].Hose);
                                    EachMinute.Hoses = ii;
                                }
                                else
                                {
                                    EachMinute.Hoses.Add(PumpList[0].Hose);
                                    if (PumpList[0].Hose > 2)
                                    {
                                        Console.Write("boop");
                                    }
                                }

                                EachMinute.Pumps = new List<int>();
                                EachMinute.Pumps.Add(PumpList[0].Pump);
                                PumpList.Remove(PumpList[0]);
                            }
                        }
                    }
                }
                if (WorkingCount == PumpList.Count)
                {
                    PumpList.Clear();
                }
                else
                {
                    WorkingCount = PumpList.Count;
                }
            }
        }

        public void GetDifferences()
        {
            //List<TransactionMinute> FinalList = new List<TransactionMinute>();
            List<TransactionMinute> CurrentProductList = new List<TransactionMinute>();
            int counter = 0;
            foreach (TransactionMinute Trans in AllATGTransactions)
            {
                if (!products.Contains(Trans.Product))
                {
                    products.Add(Trans.Product);
                }
            }
            foreach (string product in products)
            {
                foreach (TransactionMinute CurrentItem in AllATGTransactions)
                {
                    if (CurrentItem.Product == product)
                    {
                        CurrentProductList.Add(CurrentItem);
                    }
                }
                foreach (TransactionMinute tt in CurrentProductList)
                {
                    if (counter > 0)
                    {
                        tt.TankDifference = tt.TankTotalVolume - CurrentProductList[counter - 1].TankTotalVolume;
                        if (tt.TankDifference != 0)
                        {
                            // MessageBox.Show(tt.TankDifference.ToString());
                        }
                    }
                    counter++;
                }
                foreach (TransactionMinute tt2 in CurrentProductList)
                {
                    foreach (TransactionMinute ct in AllATGTransactions)
                    {
                        if (tt2.Transminute == ct.Transminute)
                        {
                            if (tt2.Product == ct.Product)
                            {
                                ct.TankDifference = tt2.TankDifference;
                            }
                        }
                    }
                }
                counter = 0;
                CurrentProductList.Clear();
            }
        }

        public void EliminateFalseTanks()
        {
            List<TransactionMinute> CurTransactions = new List<TransactionMinute>();
            foreach (string product in products)
            {
                foreach (TransactionMinute min in AllATGTransactions)
                {
                    if (min.Product == product)
                    {
                        bool CanAdd = false;
                        if (min.TankDifference != 0)
                        {
                            CanAdd = true;
                        }
                        if (min.TotalTransVolumePump != 0)
                        {
                            CanAdd = true;
                        }
                        if (CanAdd == true)
                        {
                            CurTransactions.Add(min);
                        }
                    }
                }
            }
            AllATGTransactions.Clear();
            AllATGTransactions = CurTransactions;
            foreach (TransactionMinute t in AllATGTransactions)
            {
                string pumps = "";
                if (t.Pumps != null)
                {
                    foreach (int pump in t.Pumps)
                    {
                        pumps += pump.ToString() + ";";
                    }
                }
                string tester = "";
                tester += "Pumps : " + pumps + t.Transminute.ToString() + " " + t.Product + " " + t.TankDifference.ToString() + " " + t.TotalTransVolumePump.ToString();
                listboxminutes.Items.Add(tester + Environment.NewLine);
            }
            foreach (TransactionMinute tt in AllATGTransactions)
            {
                if (tt.Hoses != null)
                {
                    foreach (int hose in tt.Hoses)
                        if (hose > 2)
                            Console.WriteLine("boop");
                }
            }
        }

        public void GetInitialFlags()
        {
            List<TransactionMinute> CurTransactions = new List<TransactionMinute>();
            List<FlagClass> CurrentFlags = new List<FlagClass>();
            foreach (string Product in products)
            {
                foreach (TransactionMinute trans in AllATGTransactions)
                {
                    if (trans.Product == Product)
                    { 
                        CurTransactions.Add(trans);
                    }
                }
                foreach (TransactionMinute trans2 in CurTransactions)
                {
                    FlagClass NewFlag = new FlagClass();
                    NewFlag.Product = trans2.Product;
                    if (trans2.Hoses == null)
                    {
                        List<int> i = new List<int>();
                        trans2.Hoses = i;
                    }
                    if (trans2.Pumps == null)
                    {
                        List<int> j = new List<int>();
                        trans2.Pumps = j;
                    }
                    NewFlag.Hoses = trans2.Hoses;
                    foreach (int hose in trans2.Hoses)
                    {
                        if (hose > 2)
                            Console.WriteLine("boop");
                    }
                    NewFlag.Pumps = trans2.Pumps;
                    NewFlag.Tanks = trans2.TankNumber;
                    NewFlag.FlagTransAmount = trans2.TotalTransVolumePump;
                    NewFlag.FlagTankDifference = trans2.TankDifference;
                    NewFlag.Flagtime = trans2.Transminute;
                    if (CurrentFlags.Count > 0)
                    {
                        TimeSpan time = NewFlag.Flagtime - CurrentFlags[CurrentFlags.Count - 1].Flagtime;
                        if (time.Minutes > 3)
                        {
                            CurrentFlags.Add(NewFlag);
                        }
                        else
                        {
                            CurrentFlags[CurrentFlags.Count - 1].Flagtime = NewFlag.Flagtime;
                            CurrentFlags[CurrentFlags.Count - 1].FlagTransAmount += NewFlag.FlagTransAmount;
                            CurrentFlags[CurrentFlags.Count - 1].FlagTankDifference += NewFlag.FlagTankDifference;
                            if (NewFlag.Hoses == null)
                            {
                                List<int> i = new List<int>();
                                i.Add(0);
                                NewFlag.Hoses = i;
                                NewFlag.Pumps = i;
                            }
                            CurrentFlags[CurrentFlags.Count - 1].Hoses.AddRange(NewFlag.Hoses);
                            CurrentFlags[CurrentFlags.Count - 1].Pumps.AddRange(NewFlag.Pumps);
                        }
                    }
                    else
                    {
                        CurrentFlags.Add(NewFlag);
                    }
                }
                InitialFlags.AddRange(CurrentFlags);
                CurrentFlags.Clear();
                CurTransactions.Clear();
            }
            List<FlagClass> replacer = new List<FlagClass>();
            foreach (FlagClass flag in InitialFlags)
            {
                bool A = true;
                if (flag.FlagTankDifference == 0)
                {
                    if (flag.FlagTransAmount == 0)
                    {
                        A = false;
                    }
                }
                if (A == true)
                {
                    replacer.Add(flag);
                }
            }
            InitialFlags.Clear();
            InitialFlags = replacer;
            foreach (FlagClass flag in InitialFlags)
            {
                decimal d = flag.FlagTransAmount / 100;
                flag.FlagTransAmount = d;
            }
            foreach (FlagClass flag in InitialFlags)
            {
                string tester = "";
                List<int> ints = new List<int>();
                foreach (int i in flag.Pumps)
                {
                    if (!ints.Contains(i))
                    {
                        if (i > 0)
                            ints.Add(i);
                    }
                }
                flag.Pumps = ints;
                string pumps = "";
                foreach (int i in flag.Pumps)
                {
                    if (i != 0)
                    {
                        pumps += i.ToString() + ";";
                    }
                }
                //ints.Clear();
                tester += "Pump :" + pumps + "Time :" + flag.Flagtime.ToString() + " Product :" + flag.Product + " Trans Amount: " + flag.FlagTransAmount.ToString() + " Tank Diff : " + flag.FlagTankDifference.ToString();
                lstbxDatareprisentaiton.Items.Add(tester + Environment.NewLine);
            }

            foreach (FlagClass ThisFlag in InitialFlags)
            {
                if (ThisFlag.FlagTransAmount + ThisFlag.FlagTankDifference < 0)
                {
                    ThisFlag.IncidentType = "Possible Line Leak/Totalizer";
                }
                if (ThisFlag.FlagTransAmount + ThisFlag.FlagTankDifference > 0)
                {
                    ThisFlag.IncidentType = "Possible Totalizer/Pump Issue";
                }
                if (ThisFlag.FlagTransAmount == 0)
                {
                    if (ThisFlag.FlagTankDifference > 0)
                    {
                        ThisFlag.IncidentType = "Possible Refilling";
                    }
                    else
                    {
                        ThisFlag.IncidentType = "Possible Tank Leak";
                    }
                }
                string tester = "";
                string pumps = "";
                //foreach (FlagClass CurFlag in )
                foreach (int i in ThisFlag.Pumps)
                {
                    if (i != 0)
                    {
                        pumps += i.ToString() + ";";
                    }
                }

                tester += "Pump :" + pumps + "Time :" + ThisFlag.Flagtime.ToString() + " Product :" + ThisFlag.Product + " Trans Amount: " + ThisFlag.FlagTransAmount.ToString() + " Tank Diff : " + ThisFlag.FlagTankDifference.ToString() + " Flag Type :" + ThisFlag.IncidentType;
                lstbxFlagData.Items.Add(tester + Environment.NewLine);
            }
            //InitialFlags.AddRange(CurrentFlags);
        }

        private void GetFinalFlags()
        {
            List<FinalFlags> PreFinalList = new List<FinalFlags>();
            PreFinalList.Clear();

            foreach (FlagClass InitialFlag in InitialFlags)
            {
                FinalFlags FlagToAdd = new FinalFlags();
                List<int> ListofPumps = new List<int>();
                if (InitialFlag.Pumps.Count == 1)
                {
                    FlagToAdd.ActualGainLoss = InitialFlag.FlagTransAmount + InitialFlag.FlagTankDifference;
                    if (InitialFlag.FlagTransAmount < 100)
                    {
                        FlagToAdd.GainLossPer100 = 100 / InitialFlag.FlagTransAmount * FlagToAdd.ActualGainLoss;
                    }
                    else
                    {
                        decimal dWorkingFigure = InitialFlag.FlagTransAmount / 100;
                        FlagToAdd.GainLossPer100 = (InitialFlag.FlagTransAmount + InitialFlag.FlagTankDifference) / dWorkingFigure;
                    }

                    FlagToAdd.PumpNum = InitialFlag.Pumps[0];

                    foreach (ConnectionClass con in Connection)
                    {
                        if (con.Product == FlagToAdd.FlagFuel)
                        {
                            FlagToAdd.Tanks = con.Tanks;
                        }
                    }

                    FlagToAdd.IncidentCount = 1;
                }
                else
                {
                    foreach (int Pump in InitialFlag.Pumps)
                    {
                        if (Pump != 0)
                        {
                            if (!ListofPumps.Contains(Pump))
                            {
                                ListofPumps.Add(Pump);
                            }
                        }
                    }

                    FlagToAdd.ActualGainLoss = InitialFlag.FlagTransAmount + InitialFlag.FlagTankDifference;
                    if (ListofPumps.Count > 0)
                    {
                        if (InitialFlag.FlagTransAmount < 100)
                        {
                            FlagToAdd.GainLossPer100 = (100 / InitialFlag.FlagTransAmount * FlagToAdd.ActualGainLoss) / ListofPumps.Count;
                        }
                        else
                        {
                            decimal dWorkingFigure = InitialFlag.FlagTransAmount / 100;
                            FlagToAdd.GainLossPer100 = ((InitialFlag.FlagTransAmount + InitialFlag.FlagTankDifference) / dWorkingFigure) / ListofPumps.Count;
                        }
                    }
                    else
                    {
                        FlagToAdd.GainLossPer100 = InitialFlag.FlagTankDifference;
                    }
                }
                FlagToAdd.FlagType = InitialFlag.IncidentType;
                FlagToAdd.FlagRating = GetFlagSeverity(FlagToAdd.GainLossPer100);
                if (InitialFlag.Hoses.Count > 0)
                {
                    FlagToAdd.HoseNum = InitialFlag.Hoses[0];
                }
                else
                {
                    FlagToAdd.HoseNum = 0;
                }
                FlagToAdd.FlagFuel = InitialFlag.Product;
                bool CanAdd = true;
                foreach (int Pump in ListofPumps)
                {
                    foreach (FinalFlags FinalFlag in PreFinalList)
                    {
                        if (FinalFlag.PumpNum == Pump)
                        {
                            if (FinalFlag.PumpNum == 0)
                            {
                                FinalFlag.IncidentCount++;
                                FinalFlag.ActualGainLoss += FlagToAdd.ActualGainLoss;
                                FinalFlag.GainLossPer100 = (FinalFlag.GainLossPer100 + FlagToAdd.GainLossPer100) / 2;
                                FinalFlag.FlagRating = (FinalFlag.FlagRating + FlagToAdd.FlagRating) / 2;
                                CanAdd = false;
                            }
                            else
                            {
                                FinalFlag.IncidentCount++;
                                FinalFlag.ActualGainLoss += FlagToAdd.ActualGainLoss;
                                FinalFlag.GainLossPer100 = (FinalFlag.GainLossPer100 + FlagToAdd.GainLossPer100) / 2;
                                FinalFlag.FlagRating = (FinalFlag.FlagRating + FlagToAdd.FlagRating) / 2;
                                CanAdd = false;
                            }
                        }
                    }
                }
                if (CanAdd == true)
                {
                    PreFinalList.Add(FlagToAdd);
                }
            }
            List<FinalFlags> Comparative = new List<FinalFlags>();
            foreach (FinalFlags FirstFlag in PreFinalList)
            {
                bool CanAdd = true;
                if (Comparative.Count > 0)
                {
                    foreach (FinalFlags SecondFlag in Comparative)
                    {
                        if (FirstFlag.FlagFuel == SecondFlag.FlagFuel)
                        {
                            if (FirstFlag.PumpNum == SecondFlag.PumpNum)
                            {
                                FirstFlag.GainLossPer100 = (FirstFlag.GainLossPer100 + SecondFlag.GainLossPer100) / 2;
                                FirstFlag.IncidentCount += SecondFlag.IncidentCount;
                                FirstFlag.FlagRating = (FirstFlag.FlagRating + SecondFlag.GainLossPer100) / 2;
                                FirstFlag.ActualGainLoss += SecondFlag.ActualGainLoss;
                                CanAdd = false;
                            }
                        }
                    }
                }
                else
                {
                    Comparative.Add(FirstFlag);
                    CanAdd = false;
                }
                if (CanAdd == true)
                {
                    Comparative.Add(FirstFlag);
                }
            }

            foreach (FinalFlags Flag in Comparative)
            {
                string Tester = "";
                string Tanks = "";
                foreach (int Tank in Flag.Tanks)
                {
                    Tanks += Tank.ToString() + ";";
                }
                Tester += Flag.FlagFuel + " Pump: " + Flag.PumpNum.ToString() + " Gain/Loss Per 100 :" + Flag.GainLossPer100.ToString() + " A-Gain/Loss: " + Flag.ActualGainLoss.ToString() + " Severity :" + Flag.FlagRating.ToString() + " Count :" + Flag.IncidentCount.ToString();
                lstbxFinalScore.Items.Add(Tester + Environment.NewLine);
            }
            FinalFlags = Comparative;
        }

        private decimal GetFlagSeverity(decimal dGainLossPerHundred)
        {
            decimal dSeverity = 0;
            if (dGainLossPerHundred < 0)
            {
                dGainLossPerHundred = dGainLossPerHundred * -1;
            }

            if (dGainLossPerHundred < 0.5M)
            {
                dSeverity = 1;
            }
            if (dGainLossPerHundred > 1)
            {
                dSeverity = 2;
            }
            if (dGainLossPerHundred > 2)
            {
                dSeverity = 3;
            }
            return dSeverity;
        }

        private void tbpL_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnGetPump_Click(object sender, EventArgs e)
        {
            string sPetrol = lstbxFuel.SelectedItem.ToString();
            int iPumpSelection = int.Parse(nudPump.Value.ToString());
            int i = AllATGTransactions.Count;
            foreach (FinalFlags Flag in FinalFlags)
            {
                if (Flag.FlagFuel == sPetrol)
                {
                    if (Flag.PumpNum == iPumpSelection)
                    {
                        tbLG.Text = Flag.ActualGainLoss.ToString();
                        tbSeverity.Text = (Math.Round(Flag.FlagRating, 2)).ToString();
                        tbLG100.Text = (Math.Round(Flag.GainLossPer100, 2)).ToString();
                        if (Flag.ActualGainLoss < 0)
                        {
                            tbLossorGain.Text = "Loss";
                        }
                        else
                        {
                            tbLossorGain.Text = "Gain";
                        }
                    }
                }
            }
            decimal TotalTransAmount = 0;
            int Transcount = 0;
            foreach (Pumps pump in PL)
            {
                if (pump.Product == sPetrol)
                {
                    if (pump.Pump == iPumpSelection)
                    {
                        Transcount++;
                        TotalTransAmount += pump.Volume;
                    }
                }
            }

            TotalTransAmount = TotalTransAmount / 100;
            tbTT.Text = TotalTransAmount.ToString();
            tbTrans.Text = Transcount.ToString();
        }

        public void GetFinalSingalFlags()
        {
            decimal decPetrolGains = 0;
            decimal decDieselGains = 0;
            decimal decPetrolLoss = 0;
            decimal decDieselLoss = 0;
            decTotalGain = 0;
            decTotalLoss = 0;
            decTotalPSales = 0;
            decTotalDSales = 0;
            foreach (FlagClass flaggie in InitialFlags)
            {
                if (flaggie.Product == "ULP95")
                {
                    decTotalPSales = decTotalPSales + flaggie.FlagTransAmount;
                }
                else
                {
                    decTotalDSales = decTotalDSales + flaggie.FlagTransAmount;
                }
                decimal decGainLoss = flaggie.FlagTransAmount + flaggie.FlagTankDifference;
                {
                    if (decGainLoss > 0)
                    {
                        decTotalGain += decGainLoss;
                        if (flaggie.Product == "ULP95")
                        { decPetrolGains = decPetrolGains + flaggie.FlagTransAmount + flaggie.FlagTankDifference; }
                        if (flaggie.Product == "D50")
                        { decDieselGains = decDieselGains + flaggie.FlagTransAmount + flaggie.FlagTankDifference; }
                    }
                    else
                    {

                        decGainLoss = decGainLoss * -1;
                        decTotalLoss += decGainLoss;
                        if (flaggie.Product == "ULP95")
                        { decPetrolLoss = decPetrolLoss + decGainLoss; }
                        if (flaggie.Product == "D50")
                        { decDieselLoss = decDieselLoss + decGainLoss; }
                    }
                }
                if (flaggie.Pumps.Count == 1)
                {
                    if (flaggie.Pumps[0] != 0)
                    {
                        FinalFlags newflag = new FinalFlags();
                        newflag.HoseNum = flaggie.Hoses[0];
                        newflag.PumpNum = flaggie.Pumps[0];
                        newflag.Tanks = flaggie.Tanks;
                        newflag.FlagFuel = flaggie.Product;
                        newflag.ActualGainLoss = flaggie.FlagTransAmount + flaggie.FlagTankDifference;
                        newflag.TransactionalTankDiff = flaggie.FlagTankDifference * -1;
                        if (newflag.ActualGainLoss > 0)
                        {
                            newflag.FlagType = "Gain";
                        }
                        else
                        {
                            newflag.FlagType = "Loss";
                            newflag.ActualGainLoss = newflag.ActualGainLoss * -1;
                            flaggie.FlagTankDifference = flaggie.FlagTankDifference * -1;
                        }

                        newflag.TransAmount = flaggie.FlagTransAmount;
                        decimal perc;
                        if (flaggie.FlagTankDifference != 0)
                        {
                            perc = 100 / flaggie.FlagTankDifference;
                            perc = perc * newflag.ActualGainLoss;
                            newflag.GainLossPer100 = Math.Round(perc, 2);
                        }
                        else
                        {
                            newflag.GainLossPer100 = 100;
                        }

                        
                        newflag.IncidentCount = 1;
                        if (SingleFlags.Count > 0)
                        {
                            bool isCanAdd = true;
                            foreach (FinalFlags Comparitor in SingleFlags)
                            {
                                if (Comparitor.PumpNum == newflag.PumpNum)
                                {
                                    if (Comparitor.FlagFuel == newflag.FlagFuel)
                                    {
                                        if (Comparitor.FlagType == newflag.FlagType)
                                        {
                                            Comparitor.GainLossPer100 = (Comparitor.GainLossPer100 + newflag.GainLossPer100) / 2;
                                            Comparitor.TransactionGainLoss = Comparitor.TransactionGainLoss + newflag.TransactionGainLoss;
                                            Comparitor.ActualGainLoss = Comparitor.ActualGainLoss + newflag.ActualGainLoss;
                                            Comparitor.TransactionalTankDiff = Comparitor.TransactionalTankDiff + newflag.TransactionalTankDiff;
                                            Comparitor.IncidentCount = Comparitor.IncidentCount + newflag.IncidentCount;
                                            Comparitor.TransAmount = Comparitor.TransAmount + newflag.TransAmount;
                                            isCanAdd = false;
                                        }
                                    }
                                }
                            }
                            if (isCanAdd == true)
                            {
                                SingleFlags.Add(newflag);
                            }
                        }
                        else
                        {
                            SingleFlags.Add(newflag);
                        }
                    }
                }
            }
            tbTotalDieselSales.Clear();
            tbTotalPetrolSales.Clear();
            tbTotalPetrolSales.Text = decTotalPSales.ToString();
            tbTotalDieselSales.Text = decTotalDSales.ToString();
            tbpG.Clear();
            tbpL.Clear();
            tbdG.Clear();
            tbdL.Clear();
            tbG.Clear();
            tbL.Clear();
            lstbxDeliveries.Items.Clear();
            tbpL.Text = decPetrolLoss.ToString();
            tbpG.Text = decPetrolGains.ToString();
            tbdL.Text = decDieselLoss.ToString();
            tbdG.Text = decDieselGains.ToString();
            tbG.Text = decTotalGain.ToString();
            tbL.Text = decTotalLoss.ToString();
            foreach (FinalFlags SingleFlag in SingleFlags)
            {
                string Tester = "";
                string Tanks = "";
                foreach (int Tank in SingleFlag.Tanks)
                {
                    Tanks += Tank.ToString() + ";";
                }
                Tester += SingleFlag.FlagFuel + " Pump: " + SingleFlag.PumpNum.ToString() + " A-GL: " + SingleFlag.ActualGainLoss.ToString() + " Tank Movement :" + SingleFlag.TransactionalTankDiff.ToString() + " Trans T: " + SingleFlag.TransAmount.ToString() + " Count :" + SingleFlag.IncidentCount.ToString() + " GL/100:  " + SingleFlag.GainLossPer100.ToString() + " Flag Type: " + SingleFlag.FlagType;
                lstbxSTF.Items.Add(Tester + Environment.NewLine);
            }
        }
        public void GetDeliveries ()
        {
            List<string> deliveries = new List<string>();
            decimal delivery = 0;
            foreach (string sProduct in products)
            {
                foreach (FlagClass flaggie in InitialFlags)
                {
                    if (flaggie.Product == sProduct)
                    {
                        if (flaggie.FlagTankDifference > 0)
                        {
                            if (flaggie.FlagTankDifference > 1000)
                            {
                                delivery = delivery + flaggie.FlagTankDifference - flaggie.FlagTransAmount;
                                
                            }
                        }
                        else
                        {
                            if (flaggie.FlagTankDifference < -1000)
                            {
                                delivery = delivery + flaggie.FlagTankDifference + flaggie.FlagTransAmount;
                               
                            }
                        }
                       
                    }
                }
                deliveries.Add(delivery.ToString());
                lstbxDeliveries.Items.Add(delivery.ToString() + " " + sProduct + Environment.NewLine);
                delivery = 0;
            }
        }
    }
}
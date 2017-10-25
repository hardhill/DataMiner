using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace ProcessMiner
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // 
            Log.w("============================= Start ============================================");
            Miner miner = new Miner();
            miner.ConnectToDB2();
            miner.ConnectToMySQL();
            
            long lastDateIC = miner.LastDateMySQL();
            miner.InsertNewProcessFromDate(lastDateIC,1/*один день*/);
            miner.CheckComplitedProcess();
            
            miner.CloseMySQL();
            miner.CloseDB2();
            //Console.ReadLine();
        }
    }
}
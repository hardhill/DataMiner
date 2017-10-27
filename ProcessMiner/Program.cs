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
            Log.w("============================= DataMiner 2017-10-27_1 ============================================");
            Miner miner = new Miner();
            miner.ConnectToDB2();
            miner.ConnectToMySQL();
            
            long lastDateICProcess = miner.LastDateProcessMySQL();
            long lastDateICTasks = miner.LastDateTasksMySQL();
            miner.InsertNewProcessFromDate(lastDateICProcess,1/*один день*/);
            miner.InsertNewTasksFromDate(lastDateICTasks,1);
            miner.CheckComplitedProcess();
            
            miner.CloseMySQL();
            miner.CloseDB2();
            //Console.ReadLine();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace DataMiner
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // read file json
            Miner miner = new Miner();
            miner.ConnectToDB2();
            miner.ConnectToMySQL();
            long lastDateIC = miner.LastDateMySQL();
            miner.CloseMySQL();
            miner.CloseDB2();
            //Console.ReadLine();
        }
    }
}
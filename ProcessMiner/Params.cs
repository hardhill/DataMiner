﻿namespace ProcessMiner
{
    public class Params
    {
        public string dbHost { get; set; }
        public string dbDatabase { get; set; }
        public string dbUser { get; set; }
        public string dbPassword { get; set; }
        public string dbProtocol { get; set; }
        public string dbPort { get; set; }
        public string msqlHost { get; set; }
        public string msqlDatabase { get; set; }
        public string msqlUser { get; set; }
        public string msqlPassword { get; set; }
        public string msqlProtocol { get; set; }
        public string msqlPort { get; set; }
        public string StartDateProcess { get; set; }
        public string StartDateTasks { get; set; }
        public int Department { get; set; }
        
    }
}
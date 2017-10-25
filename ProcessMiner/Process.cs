using System;

namespace ProcessMiner
{
    public class OneProcess
    {
        public long Id_Process { get; set; }
        public DateTime Dateofcomming { get; set; }
        public DateTime Dateofcomletion { get; set; }
        public int id_Status { get; set; }
        public int id_Type_process { get; set; }
        public int id_Department { get; set; }
    }
}
using System;
using System.Data.OleDb;
using System.IO;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace ProcessMiner
{
    public class Miner
    {
        private Params _params;
        private const string JSONFILE = "params.json";
        private const string DATEFORMAT = "yyyy-MM-dd HH:mm:ss.fff";
        private OleDbConnection connectionDb2;
        private MySqlConnection connectionMySQL;

        public Miner()
        {
            _params = new Params();
            if (!File.Exists(JSONFILE))
            {
                Console.WriteLine("Не найден файл параметров <params.json>. ");
                Console.WriteLine("Будет создан файл default <params.json>. Запустите программу снова ");
                GenerateJsonFile();
                Environment.Exit(0);
            }

            using (StreamReader file = File.OpenText(JSONFILE))
            {
                JsonSerializer serializer = new JsonSerializer();
                try
                {
                    _params = (Params) serializer.Deserialize(file, typeof(Params));
                    Console.Out.WriteLine("Прочитан файл " + JSONFILE);
                }
                catch (Exception e)
                {
                    Log.we(DateTime.Now, "Чтение файла настроек", e.Message);
                    Environment.Exit(0);
                }
                
            }
        }

        public void ConnectToDB2()
        {
            if (_params != null)
            {
                string conn = String.Format(
                    "Provider=IBMDADB2;Database={0};Hostname={1};Protocol={2};Port={3};Uid={4};Pwd={5};",
                    _params.dbDatabase, _params.dbHost,
                    _params.dbProtocol,
                    _params.dbPort, _params.dbUser, _params.dbPassword);
                try
                {
                    Console.WriteLine("Соединяемся с сервером БД {0}.", _params.dbDatabase);
                    Log.wi(DateTime.Now, "Соединение с БД","БД ПФР");
                    connectionDb2 = new OleDbConnection(conn);
                    connectionDb2.Open();
//                    OleDbCommand command = connection.CreateCommand();
//                    command.CommandText = "query";
                }
                catch (Exception e)
                {
                    Console.WriteLine("Соединения с БД не случилось! Ошибка:{0}", e.Message);
                    Log.we(DateTime.Now, "Проблема соединения с БД ПФР",e.Message);
                    Environment.Exit(0);
                }
            }
        }


        private void GenerateJsonFile()
        {
            if (_params != null)
            {
                Console.WriteLine("Генерация файла параметров \"params.json\"");
                _params.dbHost = "127.0.0.1";
                _params.dbPort = "50001";
                _params.dbDatabase = "DBTEST";
                _params.dbProtocol = "TCPIP";
                _params.Department = 35;
                try
                {
                    DateTime d = new DateTime(2010, 1, 1);
                    _params.StartDate = d.ToString(DATEFORMAT);
                }
                catch (Exception e)
                {
                    _params.StartDate = "2010-01-01 00:00:00.000";
                }
                _params.dbUser = "db2admin";
                _params.dbPassword = "`123qwer";
                _params.msqlDatabase = "infocenter";
                _params.msqlHost = "127.0.0.1";
                _params.msqlProtocol = "TCPIP";
                _params.msqlUser = "icadmin";
                _params.msqlPassword = "Inf0Center";
                _params.msqlPort = "3306";
                using (StreamWriter file = File.CreateText(@"params.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, _params);
                }
            }
        }

        public void CloseDB2()
        {
            if (connectionDb2 != null)
            {
                connectionDb2.Close();
                Console.WriteLine("Соединение с БД ПФР закрыто.");
                Log.wi(DateTime.Now, "Соединение закрыто","БД ПФР");
            }
        }

        public void ConnectToMySQL()
        {
            string conn = String.Format("Database={0};Data Source={1};User Id={2};Password={3}",
                _params.msqlDatabase, _params.msqlHost, _params.msqlUser, _params.msqlPassword, _params.msqlPort);
            try
            {
                Console.WriteLine("Соединяемся с сервером БД {0}.", _params.msqlDatabase);
                connectionMySQL = new MySqlConnection(conn);
                connectionMySQL.Open();
                Log.wi(DateTime.Now, "Соединение прошло успешно","БД 'ИнформЦентр'");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Log.we(DateTime.Now, "Соединение с БД 'ИнформЦентр'",e.Message );
                Environment.Exit(0);
            }
        }

        public void CloseMySQL()
        {
            if (connectionMySQL != null)
            {
                connectionMySQL.Close();
                Console.WriteLine("Соединение с БД 'ИнформЦентр' закрыто.");
                Log.wi(DateTime.Now, "Закрыто соединение","БД 'ИнформЦентр'");
            }
        }

        public long LastDateMySQL()
        {
            long lastTick = DateTime.Parse(_params.StartDate).Ticks;
            if (connectionMySQL != null)
            {
                string commandString = "CALL GetLastDateofcomming();";
                MySqlCommand command = new MySqlCommand(commandString,connectionMySQL);
                
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if(!reader.IsDBNull(0))
                            lastTick = reader.GetInt64(0);
                        }
                    }  
                }
            }
            Log.wi(DateTime.Now, "Последняя дата в БД ИнфоЦентр",new DateTime(lastTick).ToString("yyyy-MM-dd HH:mm:ss.fff"));
            return lastTick;
        }

        public int InsertNewProcessFromDate(long lastDateIc, int iday)
        {
            int alladd = 0;
            DateTime dtfrom = new DateTime(lastDateIc);
            DateTime dtto = dtfrom.AddDays(iday);
            string sDateFrom = dtfrom.ToString("yyyy-MM-dd hh:mm:ss.fff");
            string sDateTo = dtto.ToString("yyyy-MM-dd hh:mm:ss.fff");
            if (connectionDb2 != null)
            {
                OleDbCommand command = new OleDbCommand();
                ;
                string commandString =
                    String.Format(
                        "SELECT * FROM DB2ADMIN.PROCESSES AS T WHERE (DATE(T.DATEOFCOMMING)>='{0}')AND(DATE(T.DATEOFCOMMING)<='{1}')AND(T.ID_DEPARTMENT={2})",
                        sDateFrom, sDateTo, _params.Department);
                command.CommandText = commandString;
                command.Connection = connectionDb2;
                OleDbDataReader reader = command.ExecuteReader();
                if (reader.HasRows)            // выбраны новые данные из БД ИЦ
                {
                    while (reader.Read())
                    {
                        long id_process = reader.GetInt64(0);
                        DateTime dt1 = reader.GetDateTime(1);
                        DateTime dt2 = reader.GetDateTime(2);
                        int id_status = reader.GetInt32(3);
                        int id_type_process = reader.GetInt32(4);
                        int id_department = reader.GetInt32(5);
                        Log.wi(DateTime.Now, "Добавление данных в ИнфоЦентр","");
                    }
                }
                else //в БД ИЦ ничего не нашлось
                {
                    //если в БД ИЦ нет выбраны данные то счетчик даты увеличиваем на день, что-бы стартовать потом со след. даты
                    lastDateIc = IncrementDate(lastDateIc, 1);
                    SaveLastDate(lastDateIc);
                }
            }
            return alladd;
        }

        private void SaveLastDate(long lastDateIc)
        {
            _params.StartDate = new DateTime(lastDateIc).ToString(DATEFORMAT);
            using (StreamWriter file = File.CreateText(@"params.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, _params);
            }
        }

        private long IncrementDate(long lastDateIc, short days)
        {
            DateTime dtfrom = new DateTime(lastDateIc);
            DateTime dtto = dtfrom.AddDays(days);
            return dtto.Ticks;
        }
    }
}
using System;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlTypes;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Runtime.CompilerServices;
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
                string conn = string.Format(
                    "Provider=IBMDADB2;Database={0};Hostname={1};Protocol={2};Port={3};Uid={4};Pwd={5};",
                    _params.dbDatabase, _params.dbHost,
                    _params.dbProtocol,
                    _params.dbPort, _params.dbUser, _params.dbPassword);
                try
                {
                    Console.WriteLine("Соединяемся с сервером БД {0}.", _params.dbDatabase);
                    Log.wi(DateTime.Now, "Соединение с БД", "БД ПФР");
                    connectionDb2 = new OleDbConnection(conn);
                    connectionDb2.Open();
//                    OleDbCommand command = connection.CreateCommand();
//                    command.CommandText = "query";
                }
                catch (Exception e)
                {
                    Console.WriteLine("Соединения с БД не случилось! Ошибка:{0}", e.Message);
                    Log.we(DateTime.Now, "Проблема соединения с БД ПФР", e.Message);
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
                Log.wi(DateTime.Now, "Соединение закрыто", "БД ПФР");
            }
        }

        public void ConnectToMySQL()
        {
            string conn = string.Format("Database={0};Data Source={1};User Id={2};Password={3}",
                _params.msqlDatabase, _params.msqlHost, _params.msqlUser, _params.msqlPassword, _params.msqlPort);
            try
            {
                Console.WriteLine("Соединяемся с сервером БД {0}.", _params.msqlDatabase);
                connectionMySQL = new MySqlConnection(conn);
                connectionMySQL.Open();
                Log.wi(DateTime.Now, "Соединение прошло успешно", "БД 'ИнформЦентр'");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Log.we(DateTime.Now, "Соединение с БД 'ИнформЦентр'", e.Message);
                Environment.Exit(0);
            }
        }

        public void CloseMySQL()
        {
            if (connectionMySQL != null)
            {
                connectionMySQL.Close();
                Console.WriteLine("Соединение с БД 'ИнформЦентр' закрыто.");
                Log.wi(DateTime.Now, "Закрыто соединение", "БД 'ИнформЦентр'");
            }
        }

        public long LastDateMySQL()
        {
            long lastTick = DateTime.Parse(_params.StartDate).Ticks;
            if (connectionMySQL != null)
            {
                string commandString = "CALL GetLastDateofcomming();";
                MySqlCommand command = new MySqlCommand(commandString, connectionMySQL);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                lastTick = reader.GetDateTime(0).Ticks;
                        }
                    }
                }
            }
            SaveLastDate(lastTick);
            Log.wi(DateTime.Now, "Последняя дата в БД ИнфоЦентр", new DateTime(lastTick).ToString("yyyy-MM-dd HH:mm:ss.fff"));
            return lastTick;
        }

        public int InsertNewProcessFromDate(long lastDateIc, int iday)
        {
            int alladd = 0;
            string nnn = "NULL";
            string sql;
            int allAdded = 0;
            OneProcess oneProcess = new OneProcess();
            DateTime dtfrom = new DateTime(lastDateIc);
            DateTime dtto = dtfrom.AddDays(iday);
            string sDateFrom = dtfrom.ToString("yyyy-MM-dd hh:mm:ss.fff");
            string sDateTo = dtto.ToString("yyyy-MM-dd hh:mm:ss.fff");
            if (connectionDb2 != null)
            {
                OleDbCommand commandDB2 = new OleDbCommand();
                ;
                string commandString =
                    string.Format(
                        "SELECT * FROM DB2ADMIN.PROCESSES AS T WHERE (DATE(T.DATEOFCOMMING)>='{0}')AND(DATE(T.DATEOFCOMMING)<='{1}')AND(T.ID_DEPARTMENT={2})",
                        sDateFrom, sDateTo, _params.Department);
                commandDB2.CommandText = commandString;
                commandDB2.Connection = connectionDb2;
                OleDbDataReader readerDB2 = commandDB2.ExecuteReader();
                if (readerDB2.HasRows) // выбраны новые данные из БД ИЦ
                {
                    while (readerDB2.Read())
                    {
                        try
                        {
                            oneProcess.Id_Process = readerDB2.GetInt64(0);
                            oneProcess.Dateofcomming = readerDB2.GetDateTime(1);
                            if (!readerDB2.IsDBNull(2))
                            {
                                oneProcess.Dateofcomletion = readerDB2.GetDateTime(2);
                                nnn = oneProcess.Dateofcomletion.ToString(DATEFORMAT);
                            }
                            else
                            {
                                nnn = "NULL";
                            }
                            if (!readerDB2.IsDBNull(3))
                            {
                                oneProcess.id_Status = readerDB2.GetInt16(3);
                            }
                            if (!readerDB2.IsDBNull(4))
                            {
                                oneProcess.id_Type_process = readerDB2.GetInt16(4);
                            }
                            oneProcess.id_Department = readerDB2.GetInt16(5);

                            
                        }
                        catch (Exception e)
                        {
                            Log.we(DateTime.Now, "Добавление данных в ИнфоЦентр",e.Message);
                        }
                        if (nnn != "NULL")
                        {
                            sql = string.Format(
                                "INSERT INTO PROCESSES (ID_PROCESS, DATEOFCOMMING, DATEOFCOMPLETION, ID_STATUS, ID_TYPE_PROCESS, ID_DEPARTMENT)" +
                                "VALUES({0},'{1}','{2}',{3},{4},{5});", oneProcess.Id_Process, oneProcess.Dateofcomming.ToString(DATEFORMAT), nnn,
                                oneProcess.id_Status, oneProcess.id_Type_process, oneProcess.id_Department);
                        }
                        else
                        {
                            //вариант запроса с пустым значением даты
                            sql = string.Format(
                                "INSERT INTO PROCESSES (ID_PROCESS, DATEOFCOMMING, DATEOFCOMPLETION, ID_STATUS, ID_TYPE_PROCESS, ID_DEPARTMENT)" +
                                "VALUES({0},'{1}',{2},{3},{4},{5});", oneProcess.Id_Process, oneProcess.Dateofcomming.ToString(DATEFORMAT), nnn,
                                oneProcess.id_Status, oneProcess.id_Type_process, oneProcess.id_Department);
                        }
                        //подготовим вставку в БД Инфоцентра
                        MySqlCommand commandMySQL = new MySqlCommand(sql, connectionMySQL);
                        //добавляем запись
                        try
                        {
                            alladd = commandMySQL.ExecuteNonQuery();
                            allAdded += alladd;
                        }
                        catch (Exception e)
                        {
                            Log.we(DateTime.Now, "Запись в БД Инфоцентр", e.Message);
                        }
                    } //цикл по найденым в ПФР
                    
                    Log.wi(DateTime.Now, "Новые процессы", i: string.Format("Добавлено:{0}", allAdded));
                }
                else /*в БД ИЦ ничего не нашлось*/
                {
                    Log.wi(DateTime.Now, "Выборка данных из БД ПФР",
                        i: string.Format("За {0} + 1день ничего не нашлось", new DateTime(lastDateIc).ToString(DATEFORMAT)));
                    //если в БД ИЦ нет выбраны данные то счетчик даты увеличиваем на день, что-бы стартовать потом со след. даты
                    lastDateIc = IncrementDate(lastDateIc, 1);
                    SaveLastDate(lastDateIc);
                }
            } //selected data

            return allAdded;
        }

        //сохраняет дату в файле настроек
        private void SaveLastDate(long lastDateIc)
        {
            _params.StartDate = new DateTime(lastDateIc).ToString(DATEFORMAT);
            using (StreamWriter file = File.CreateText(@"params.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, _params);
            }
        }

        //увеличивает на Н дней текущую, крайнюю дату проверки процессов
        private long IncrementDate(long lastDateIc, short days)
        {
            DateTime dtto;
            DateTime dtfrom = new DateTime(lastDateIc);
            if (DateTime.Now > dtfrom)
            {
               dtto  = dtfrom.AddDays(days);
            }
            else
            {
                dtto = DateTime.Now;
            }
            return dtto.Ticks;
        }

        // проверка выполненных процессов
        public void CheckComplitedProcess()
        {
            string sql = String.Format("SELECT ID_PROCESS FROM PROCESSES AS T WHERE (T.DATEOFCOMPLETION IS NULL)",
                _params.Department);
            MySqlCommand commandMySQL = new MySqlCommand(sql, connectionMySQL);
            using (MySqlDataReader readerMySQL = commandMySQL.ExecuteReader())
            {
                if (readerMySQL.HasRows)
                {
                    while (readerMySQL.Read())
                    {
                        long id_processMySQL = readerMySQL.GetInt64(0);
                        //теперь по этому значению вытаскиваем данные из БД ПФР
                        OneProcess processMySQL = new OneProcess();
                        //и записываем в класс
                        processMySQL = GetProcessFromDB2(id_processMySQL);
                        // обновляем данные в БД ИнфоЦентра
                        UpdateMySQLProcess(processMySQL);
                    }
                }
            }
        }

        private void UpdateMySQLProcess(OneProcess pr)
        {
            string sql = String.Format(
                "UPDATE PROCESSES SET DATEOFCOMPLETION = '{0}', ID_STATUS = {1}, ID_TYPE_PROCESS = {2} WHERE ID_PROCESS = {3}",
                pr.Dateofcomletion.ToString(DATEFORMAT),
                pr.id_Status,
                pr.id_Type_process,
                pr.Id_Process);
            try
            {
                MySqlCommand commandMySQL = new MySqlCommand(sql, connectionMySQL);
                if (commandMySQL.ExecuteNonQuery() > 0)
                {
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                
            }
        }

        private OneProcess GetProcessFromDB2(long idProcessMySql)
        {
            OneProcess process = new OneProcess();
            try
            {
                string sql = String.Format("SELECT * FROM DB2.ADMIN AS T WHERE T.ID_PROCESS = {0}", idProcessMySql);
                OleDbCommand commandDb2 = new OleDbCommand(sql, connectionDb2);
                using (OleDbDataReader readerDb2 = commandDb2.ExecuteReader())
                {
                    if (readerDb2.HasRows)
                    {
                        if (readerDb2.Read())
                        {
                            process.Id_Process = readerDb2.GetInt64(0);
                            process.Dateofcomming = readerDb2.GetDateTime(1);
                            process.Dateofcomletion = readerDb2.GetDateTime(2);
                            process.id_Status = readerDb2.GetInt16(3);
                            process.id_Type_process = readerDb2.GetInt16(4);
                            process.id_Department = readerDb2.GetInt16(5);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return process;
        }
    }
}
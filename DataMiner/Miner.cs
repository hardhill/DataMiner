using System;
using System.ComponentModel;
using System.Data.OleDb;
using System.IO;
using System.Net;
using System.Security.Permissions;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace DataMiner
{
    public class Miner
    {
        private Params _params;
        private string jsonfile = "params.json";
        private OleDbConnection connectionDb2;
        private MySqlConnection connectionMySQL;

        public Miner()
        {
            _params = new Params();
            if (!File.Exists(jsonfile))
            {
                Console.WriteLine("Не найден файл параметров <params.json>. ");
                Console.WriteLine("Будет создан файл default <params.json>. Запустите программу снова ");
                GenerateJsonFile();
                Environment.Exit(0);
            }

            using (StreamReader file = File.OpenText(jsonfile))
            {
                JsonSerializer serializer = new JsonSerializer();
                _params = (Params) serializer.Deserialize(file, typeof(Params));
                Console.Out.WriteLine("Прочитан файл " + jsonfile);
            }
        }

        public void ConnectToDB2()
        {
            if (_params != null)
            {
                string conn = String.Format("Provider=IBMDADB2;Database={0};Hostname={1};Protocol={2};Port={3};Uid={4};Pwd={5};", _params.dbDatabase, _params.dbHost,
                    _params.dbProtocol,
                    _params.dbPort, _params.dbUser, _params.dbPassword);
                try
                {
                    Console.WriteLine("Соединяемся с сервером БД {0}.",_params.dbDatabase);
                    connectionDb2 = new OleDbConnection(conn);
                    connectionDb2.Open();
//                    OleDbCommand command = connection.CreateCommand();
//                    command.CommandText = "query";
                }
                catch (Exception e)
                {
                    Console.WriteLine("Соединения с БД не случилось! Ошибка:{0}",e.Message);
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
                    DateTime d = new DateTime(2010,1,1);
                    _params.StartDate = d.Ticks;
                }
                catch (Exception e)
                {
                    _params.StartDate = 1;
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
                Console.WriteLine("Соединение с БД DB2 закрыто.");
            }
        }

        public void ConnectToMySQL()
        {
            string conn = String.Format("Database={0};Data Source={1};User Id={2};Password={3}",
                _params.msqlDatabase, _params.msqlHost, _params.msqlUser, _params.msqlPassword, _params.msqlPort);
            try
            {
                Console.WriteLine("Соединяемся с сервером БД {0}.",_params.msqlDatabase);
                connectionMySQL = new MySqlConnection(conn);
                connectionMySQL.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            
        }

        public void CloseMySQL()
        {
            if (connectionMySQL != null)
            {
                connectionMySQL.Close();
                Console.WriteLine("Соединение с БД MySQL закрыто.");
            }
        }

        public long LastDateMySQL()
        {
            long lastTick =0;
            return lastTick;
        }
    }
}
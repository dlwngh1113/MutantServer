using mutant_server.Packets;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace mutant_server.Objects.Networking
{
    public class DBConnector
    {
        private MySqlConnection _connection;
        public DBConnector()
        {
            _connection = new MySqlConnection("Server=localhost;Database=lulus;Uid=root;Pwd=ljh13485727");

            CheckConnection();
        }

        private void CheckConnection()
        {
            try
            {
                _connection.Open();
                Console.WriteLine("DB 연결이 정상적으로 이루어짐");
                _connection.Close();
            }
            catch(MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public bool InsertData(LoginPacket packet)
        {
            string myQuery = "insert into lulus.mutant (nameMutant, pwMutant) values(\"" + packet.name + "\", \"" + packet.passwd + "\")";
            Console.WriteLine("System(DB): " + myQuery);
            _connection.Open();
            MySqlCommand command = new MySqlCommand(myQuery, _connection);
            try
            {
                if (command.ExecuteNonQuery() == 1)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("InsertData to Database is somethig wrong");
                    return false;
                }
            } 
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            _connection.Close();
            return true;
        }

        public bool isValidData(LoginPacket packet)
        {
            DataSet dataSet = new DataSet();
            string myQuery = "select * from lulus.mutant where nameMutant=\"" + packet.name + "\" and pwMutant=\"" + packet.passwd + "\"";

            MySqlDataAdapter adapter = new MySqlDataAdapter(myQuery, _connection);
            adapter.Fill(dataSet, "lulus.mutant");
            if(dataSet.Tables.Count == 1)
            {
                return true;
            }

            return false;
        }

        public Client GetUserData(string name, string passWd)
        {
            string myQuery = "select * from lulus.mutant where nameMutant=\"" + name + "\" and pwMutant=\"" + passWd + "\"";

            MySqlCommand command = new MySqlCommand(myQuery, _connection);
            MySqlDataReader table = command.ExecuteReader();

            Client c = new Client(0);
            while(table.Read())
            {
                c.userName = name;
                c.passWd = passWd;
                c.userID = (int)table["idMutant"];
                c.winCountTrator = (int)table["winCountTrator"];
                c.winCountNocturn = (int)table["winCountNocturn"];
                c.winCountPsychy = (int)table["winCountPsychy"];
                c.winCountResearcher = (int)table["winCountResearcher"];
                c.winCountTanker = (int)table["winCountTanker"];
            }
            return c;
        }

        public void UpdateData(Client client)
        {
            string myQuery = "update lulus.mutant set winCountTrator=" + client.winCountTrator.ToString() + ", winCountResearcher=" + client.winCountResearcher.ToString() +
                ", winCountNocturn=" + client.winCountNocturn.ToString() + ", winCountPsychy=" + client.winCountPsychy.ToString() + ", winCountTanker=" + client.winCountTanker.ToString() + 
                " where nameMutant=\"" + client.userName + "\"";
            Console.WriteLine("System(DB): " + myQuery);
            _connection.Open();
            MySqlCommand command = new MySqlCommand(myQuery, _connection);
            try
            {
                if(command.ExecuteNonQuery() == 1)
                {
                    return;
                }
                else
                {
                    Console.WriteLine("UpdateData to Database is something wrong");
                }
            }
            catch(MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            _connection.Close();
        }
    }
}

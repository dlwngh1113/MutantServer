using mutant_server.Packets;
using MySql.Data.MySqlClient;
using System;

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
                    _connection.Close();
                    return true;
                }
                else
                {
                    Console.WriteLine("InsertData to Database is somethig wrong");
                    _connection.Close();
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
            string myQuery = "select * from lulus.mutant where nameMutant=\"" + packet.name + "\" and pwMutant=\"" + packet.passwd + "\"";

            _connection.Open();
            MySqlCommand command = new MySqlCommand(myQuery, _connection);
            MySqlDataReader table = command.ExecuteReader();

            if(table.Read())
            {
                _connection.Close();
                return true;
            }

            Console.WriteLine("System(DB): id({0}), pwd({1}) is not created account", packet.name, packet.passwd);
            _connection.Close();
            return false;
        }

        public Client GetUserData(string name, string passWd)
        {
            string myQuery = "select * from lulus.mutant where nameMutant=\"" + name + "\" and pwMutant=\"" + passWd + "\"";

            _connection.Open();
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

                c.playCountTrator = (int)table["playCountTrator"];
                c.playCountNocturn = (int)table["playCountNocturn"];
                c.playCountResearcher = (int)table["playCountResearcher"];
                c.playCountTanker = (int)table["playCountTanker"];
                c.playCountPsychy = (int)table["playCountPsychy"];

                Console.WriteLine("System(DB): id({0}), name({1}), pwd({2}) get user data", c.userID, c.userName, c.passWd);
            }

            _connection.Close();
            return c;
        }

        public void UpdateData(Client client)
        {
            string myQuery = "update lulus.mutant set winCountTrator=" + client.winCountTrator.ToString() + ", winCountResearcher=" + client.winCountResearcher.ToString() +
                ", winCountNocturn=" + client.winCountNocturn.ToString() + ", winCountPsychy=" + client.winCountPsychy.ToString() + ", winCountTanker=" + client.winCountTanker.ToString() +
                ", playCountTrator=" + client.playCountTrator.ToString() + ", playCountResearcher=" + client.playCountResearcher.ToString() +
                ", playCountNocturn=" + client.playCountNocturn.ToString() + ", playCountPsychy=" + client.playCountPsychy.ToString() + ", playCountTanker=" + client.playCountTanker.ToString() +
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

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
            MySqlCommand command = new MySqlCommand("InsertMethod", _connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new MySqlParameter("id", packet.name));
            command.Parameters.Add(new MySqlParameter("passwd", packet.passwd));
            try
            {
                command.Connection.Open();
                if (command.ExecuteNonQuery() == 1)
                {
                    command.Connection.Close();
                    return true;
                }
                else
                {
                    Console.WriteLine("InsertData to Database is somethig wrong");
                    command.Connection.Close();
                    return false;
                }
            }
            catch(MySqlException ex)
            {
                command.Connection.Close();
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        public bool isValidData(LoginPacket packet)
        {
            MySqlCommand command = new MySqlCommand("SelectMethod", _connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new MySqlParameter("id", packet.name));
            command.Parameters.Add(new MySqlParameter("passwd", packet.passwd));
            //커넥션이 이미 있다고 에러생김

            _connection.Open();

            MySqlDataReader table = command.ExecuteReader();

            if (table.Read())
            {
                _connection.Close();
                return true;
            }

            Console.WriteLine("System(DB): id({0}), pwd({1}) is not created account", packet.name, packet.passwd);
            _connection.Close();
            table.Close();

            return false;
        }

        public Client GetUserData(string name, string passWd)
        {
            MySqlCommand command = new MySqlCommand("SelectMethod", _connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new MySqlParameter("id", name));
            command.Parameters.Add(new MySqlParameter("passwd", passWd));

            Client c = new Client(0);

            _connection.Open();

            MySqlDataReader table = command.ExecuteReader();

            while (table.Read())
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

            table.Close();
            return c;
        }

        public void UpdateData(Client client)
        {
            MySqlCommand command = new MySqlCommand("UpdateMethod", _connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new MySqlParameter("winCountTrator", client.winCountTrator));
            command.Parameters.Add(new MySqlParameter("winCountResearcher", client.winCountResearcher));
            command.Parameters.Add(new MySqlParameter("winCountNocturn", client.winCountNocturn));
            command.Parameters.Add(new MySqlParameter("winCountPsychy", client.winCountPsychy));
            command.Parameters.Add(new MySqlParameter("winCountTanker", client.winCountTanker));

            command.Parameters.Add(new MySqlParameter("playCountTrator", client.playCountTrator));
            command.Parameters.Add(new MySqlParameter("playCountResearcher", client.playCountResearcher));
            command.Parameters.Add(new MySqlParameter("playCountNocturn", client.playCountNocturn));
            command.Parameters.Add(new MySqlParameter("playCountPsychy", client.playCountPsychy));
            command.Parameters.Add(new MySqlParameter("playCountTanker", client.playCountTanker));

            command.Parameters.Add(new MySqlParameter("userName", client.userName));

            _connection.Open();

            if (command.ExecuteNonQueryAsync().Result == 1)
            {
                _connection.Close();
                return;
            }
            else
            {
                Console.WriteLine("UpdateData to Database is something wrong");
            }

            _connection.Close();
        }
    }
}

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

        public void InsertData(LoginPacket packet)
        {
            string myQuery = "insert into lulus.mutant (nameMutant, pwMutant) values(\"" + packet.name + "\", \"" + packet.passwd + "\")";
            Console.WriteLine(myQuery);
            _connection.Open();
            MySqlCommand command = new MySqlCommand(myQuery, _connection);
            try
            {
                if (command.ExecuteNonQuery() == 1)
                {
                    return;
                }
                else
                {
                    Console.WriteLine("InsertData to Database is somethig wrong");
                }
            } 
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            _connection.Close();
        }

        public void UpdateData(Client client)
        {
            string myQuery = "update lulus.mutant set winCountTrator=" + client.winCoundTrator.ToString() + ", winCountResearcher=" + client.winCountResearcher.ToString() +
                ", winCountNocturn=" + client.winCountNocturn.ToString() + ", winCountPsychy=" + client.winCountPsychy.ToString() + ", winCountTanker=" + client.winCountTanker.ToString() + 
                " where nameMutant=\"" + client.userName + "\"";
            Console.WriteLine(myQuery);
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

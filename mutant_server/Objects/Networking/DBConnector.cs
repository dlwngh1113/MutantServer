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
            }
            catch(MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void InsertData()
        {
            //This is my connection string i have assigned the database file address path  
            string MyConnection2 = "datasource=localhost;port=3307;username=root;password=root";
            //This is my insert query in which i am taking input from the user through windows forms  
            string Query = "insert into student.studentinfo(idStudentInfo,Name,Father_Name,Age,Semester) values('" + this.IdTextBox.Text + "','" + this.NameTextBox.Text + "','" + this.FnameTextBox.Text + "','" + this.AgeTextBox.Text + "','" + this.SemesterTextBox.Text + "');";
            //This is  MySqlConnection here i have created the object and pass my connection string.  
            MySqlConnection MyConn2 = new MySqlConnection(MyConnection2);
            //This is command class which will handle the query and connection object.  
            MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
            MySqlDataReader MyReader2;
            MyConn2.Open();
            MyReader2 = MyCommand2.ExecuteReader();     // Here our query will be executed and data saved into the database.  
            while (MyReader2.Read())
            {
            }
            MyConn2.Close();
        }

        public void UpdateData()
        {
            //This is my connection string i have assigned the database file address path  
            string MyConnection2 = "datasource=localhost;port=3307;username=root;password=root";
            //This is my update query in which i am taking input from the user through windows forms and update the record.  
            string Query = "update student.studentinfo set idStudentInfo='" + this.IdTextBox.Text + "',Name='" + this.NameTextBox.Text + "',Father_Name='" + this.FnameTextBox.Text + "',Age='" + this.AgeTextBox.Text + "',Semester='" + this.SemesterTextBox.Text + "' where idStudentInfo='" + this.IdTextBox.Text + "';";
            //This is  MySqlConnection here i have created the object and pass my connection string.  
            MySqlConnection MyConn2 = new MySqlConnection(MyConnection2);
            MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
            MySqlDataReader MyReader2;
            MyConn2.Open();
            MyReader2 = MyCommand2.ExecuteReader();
            while (MyReader2.Read())
            {
            }
            MyConn2.Close();//Connection closed here 
        }
    }
}

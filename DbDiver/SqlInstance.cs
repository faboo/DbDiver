using System;
using System.Text;

namespace DbDiver
{
    public class SqlInstance : NetworkInstance
    {
        protected override string Type
        {
            get { return "MS SQL"; }
        }

        public override Connection CreateConnection(string username, string password)
        {
            var connection = new SqlConnection();
            var connString = new StringBuilder();

            if(!String.IsNullOrWhiteSpace(Instance))
                connString.AppendFormat("Server={0}\\{1};", Server, Instance);
            else if(!String.IsNullOrWhiteSpace(Port))
                connString.AppendFormat("Server={0},{1};", Server, Port);
            else
                connString.AppendFormat("Server={0};", Server);

            if(String.IsNullOrWhiteSpace(username))
                connString.Append("Trusted_Connection=True;");
            else
                connString.AppendFormat(
                    "User Id={0};Password={1};",
                    username,
                    password);

            connection.ConnectionString = connString.ToString();

            return connection;
        }
    }
}

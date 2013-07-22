using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbDiver
{
    public class MySqlInstance : DbInstance
    {
        protected override string Type
        {
            get { return "MySQL"; }
        }

        public override Connection CreateConnection(string username, string password)
        {
            var connection = new MySqlConnection();

            var connString = new StringBuilder();

            connString.AppendFormat("Server={0};", Server);

            if(!String.IsNullOrWhiteSpace(Port))
                connString.AppendFormat("Port={1};", Port);

            if(String.IsNullOrWhiteSpace(username))
                connString.Append("IntegratedSecurity=True;Uid=auth_windows;");
            else
                connString.AppendFormat(
                    "Uid={0};Pwd={1};",
                    username,
                    password);

            connString.Append("sqlservermode=True;SslMode=Preferred;");

            connection.ConnectionString = connString.ToString();

            return connection;
        }
    }
}

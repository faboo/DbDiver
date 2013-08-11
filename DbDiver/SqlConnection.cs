using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DbDiver
{
    public class SqlConnection : Connection
    {
        protected override DbConnection Create(string connString)
        {
            return new System.Data.SqlClient.SqlConnection(connString);
        }

        public override DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public override List<string> GetDatabases()
        {
            List<string> databases = new List<string>();

            using (var conn = Get())
            {
                using (var getDatabases = conn.CreateCommand())
                {
                    DbDataReader read;

                    getDatabases.CommandText = "sp_databases";
                    using (read = getDatabases.ExecuteReader())
                    {
                        while (read.Read())
                            databases.Add(read.GetString(read.GetOrdinal("DATABASE_NAME")));

                        read.Close();
                    }
                }
                conn.Close();
            }

            return databases;
        }

        public override DataTable GetProcedures()
        {
            DataTable results = new DataTable();

            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    DbDataAdapter adapter = new SqlDataAdapter();

                    command.CommandText = "SELECT name as 'Procedure', sys.objects.type_desc as 'Type' "
                                          + "FROM sys.sql_modules "
                                          + "JOIN sys.objects ON sql_modules.object_id = objects.object_id";

                    adapter.SelectCommand = command;
                    adapter.Fill(results);
                }

                conn.Close();
            }

            return results;
        }

        public override string DescribeProcedure(string name)
        {
            string description;

            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {

                    command.CommandText =
                        "SELECT Definition " +
                        "FROM sys.sql_modules m JOIN sys.objects o ON m.object_id = o.object_id " +
                        //"AND TYPE IN ('FN', 'IF', 'TF', 'P') " +
                        "WHERE Name = @objname;";
                    command.AddWithValue("@objname", name.Trim());
                    description = command.ExecuteScalar() as string;
                }
                conn.Close();
            }

            return description;
        }

        public override DataTable GetTables()
        {
            DataTable results = new DataTable();

            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    DbDataAdapter adapter = new SqlDataAdapter();

                    command.CommandText = "SELECT name as 'Table', SUM(rows) as 'Rows' "
                        + "FROM sys.tables "
                        + "INNER JOIN sys.partitions "
                        + "ON sys.partitions.OBJECT_ID = sys.tables.OBJECT_ID "
                        + "WHERE is_ms_shipped = 0 AND index_id IN (1,0) "
                        + "GROUP BY name";

                    adapter.SelectCommand = command;
                    adapter.Fill(results);
                }
            }

            return results;
        }

        protected override void GetPrimaryKeys(string table, Dictionary<string, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT ordinal_position, column_name "
                        + "FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE "
                        + "WHERE OBJECTPROPERTY(OBJECT_ID(constraint_name), 'IsPrimaryKey') = 1 "
                        + "AND table_name = @tableName";
                    command.AddWithValue("@tableName", table);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Key key = new Key
                            {
                                Type = KeyType.Primary,
                                Column = reader.GetInt32(0),
                                Name = reader.GetString(1),
                            };
                            if (!keys.ContainsKey(key.Name))
                                keys[key.Name] = key;
                            else
                                keys[key.Name].Type = KeyType.Primary;
                        }

                        reader.Close();
                    }
                }
            }
        }
    }
}

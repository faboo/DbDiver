using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace DbDiver
{
    public class MySqlConnection : Connection
    {
        override protected DbConnection Create(string connString)
        {
            return new MySql.Data.MySqlClient.MySqlConnection(connString);
        }

        public override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        public override List<string> GetDatabases()
        {
            List<string> databases = new List<string>();

            using (var conn = Get())
            {
                using (var getDatabases = conn.CreateCommand())
                {
                    DbDataReader read;

                    getDatabases.CommandText = "show databases";
                    using (read = getDatabases.ExecuteReader())
                    {
                        while (read.Read())
                            databases.Add(read.GetString(read.GetOrdinal("Database")));

                        read.Close();
                    }
                }
                conn.Close();
            }

            return databases;
        }

        public override DataTable GetProcedures()
        {
            DataTable procedures = new DataTable();

            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    DbDataAdapter adapter = CreateDataAdapter();

                    command.CommandText = "show procedure status where [Db] = @database";
                    command.AddWithValue("@database", conn.Database);
                    adapter.SelectCommand = command;
                    adapter.Fill(procedures);
                }
                conn.Close();
            }


            for (int idx = 0; idx < procedures.Columns.Count; )
            {
                if (procedures.Columns[idx].ColumnName.Equals("Name"))
                {
                    procedures.Columns[idx].ColumnName = "Procedure";
                    idx += 1;
                }
                else if (!procedures.Columns[idx].ColumnName.Equals("Type"))
                {
                    procedures.Columns.RemoveAt(idx);
                }
                else
                {
                    idx += 1;
                }
            }

            return procedures;
        }

        public override string DescribeProcedure(string name)
        {
            string description;

            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {

                    command.CommandText =
                        "show create procedure @objname";
                    command.AddWithValue("@objname", name.Trim());
                    description = command.ExecuteScalar() as string;
                }
                conn.Close();
            }

            return description;
        }

        protected override void GetPrimaryKeys(string table, Dictionary<int, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    DataTable indexes = new DataTable();
                    DbDataAdapter adapter = CreateDataAdapter();

                    command.CommandText =
                        "show indexes from [" + table + "] "
                        + "where [Key_name] = 'PRIMARY'";
                    adapter.SelectCommand = command;
                    adapter.Fill(indexes);

                    foreach(DataRow index in indexes.Rows)
                    {
                        int position;
                        Key key;

                        command.CommandText =
                            "select [ORDINAL_POSITION] from [INFORMATION_SCHEMA].[COLUMNS] "
                            + "where [COLUMNS].[COLUMN_NAME] = @columnName "
                            + "and [COLUMNS].[TABLE_NAME] = @tableName "
                            + "and [COLUMNS].[TABLE_SCHEMA] = @database ";
                        command.AddWithValue("@columnName", index["column_name"]);
                        command.AddWithValue("@tableName", table);
                        command.AddWithValue("@database", conn.Database);
                        position = command.ExecuteScalar().ToInt();

                        key = new Key
                        {
                            Type = KeyType.Primary,
                            Column = position,
                        };
                        if (!keys.ContainsKey(key.Column))
                            keys[key.Column] = key;
                        else
                            keys[key.Column].Type = KeyType.Primary;
                    }
                }
            }
        }

        protected override void GetForeignKeys(string table, Dictionary<int, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText =
                        "select [ORDINAL_POSITION] as [parent_column_id], [REFERENCED_TABLE_NAME] as [name], "
                        + "[REFERENCED_COLUMN_NAME] as [key name] "
                        + "from "
                        + "[INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] where "
                        + "[REFERENCED_TABLE_NAME] is not null and [TABLE_NAME] = @tableName";
                    command.AddWithValue("@tableName", table);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Key key = new Key
                            {
                                Type = KeyType.Foreign,
                                Column = (int)reader.GetInt64(0),
                                ForeignTable = reader.GetString(1),
                                ForeignColumn = reader.GetString(2),
                            };
                            keys[key.Column] = key;
                        }

                        reader.Close();
                    }
                }
            }
        }
    }
}

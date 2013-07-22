using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;

namespace DbDiver
{
    public class CeConnection : Connection
    {
        override protected DbConnection Create(string connString)
        {
            return new SqlCeConnection(connString);
        }

        public override DbDataAdapter CreateDataAdapter()
        {
            return new SqlCeDataAdapter();
        }

        public override List<string> GetDatabases()
        {
            return null;
        }

        public override DataTable GetProcedures()
        {
            return null;
        }

        public override string DescribeProcedure(string name)
        {
            throw new NotImplementedException();
        }

/*        public override DataTable GetTables()
        {
            DataTable results = new DataTable();

            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    DbDataAdapter adapter = new SqlCeDataAdapter();

                    command.CommandText = "select TABLE_NAME as [Table], 0 as [Rows] "
                        + "from INFORMATION_SCHEMA.TABLES "
                        + "group by TABLE_NAME";

                    adapter.SelectCommand = command;
                    adapter.Fill(results);

                    foreach (DataRow table in results.Rows)
                    {
                        command.CommandText = "select count(*) from [" + table.ItemArray[0] + "]";
                        table.BeginEdit();
                        table[1] = (int)command.ExecuteScalar();
                        table.EndEdit();
                    }
                }
            }

            return results;
        }*/

/*        protected override void GetPrimaryKeys(string table, Dictionary<int, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select [ordinal_position] from [INFORMATION_SCHEMA].[INDEXES] "
                        + "where [PRIMARY_KEY] = 1 AND [TABLE_NAME] = @tableName";
                    command.AddWithValue("@tableName", table);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Key key = new Key
                            {
                                Type = KeyType.Primary,
                                Column = reader.GetInt32(0),
                            };
                            if (!keys.ContainsKey(key.Column))
                                keys[key.Column] = key;
                            else
                                keys[key.Column].Type = KeyType.Primary;
                        }

                        reader.Close();
                    }
                }
            }
        }*/
    }
}

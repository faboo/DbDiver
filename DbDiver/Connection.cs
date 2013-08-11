using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading;

namespace DbDiver
{
    public abstract class Connection
    {
        const string DB_CHECK = "(([TABLE_CATALOG] is null and [TABLE_SCHEMA] is null) or ([TABLE_CATALOG] is null and [TABLE_SCHEMA] = @database) or [TABLE_CATALOG] = @database)";
        Mutex mutex;
        string database;
        string connString;

        public Connection()
        {
            mutex = new Mutex();
        }

        public string ConnectionString
        {
            get
            {
                string connString = null;
                mutex.WaitOne();
                connString = this.connString;
                mutex.ReleaseMutex();
                return connString;
            }
            set
            {
                mutex.WaitOne();
                connString = value;
                mutex.ReleaseMutex();
            }
        }
        public string Database {
            get {
                string db = null;
                mutex.WaitOne();
                db = database;
                mutex.ReleaseMutex();
                return db;
            }
            set {
                mutex.WaitOne();
                database = value;
                mutex.ReleaseMutex();
            }
        }

        protected abstract DbConnection Create(string connString);

        public abstract DbDataAdapter CreateDataAdapter();

        public DbConnection Get()
        {
            string db = null;
            string connString = null;
            DbConnection conn = null;

            mutex.WaitOne();
            db = database;
            connString = this.connString;
            mutex.ReleaseMutex();

            conn = Create(connString);

            conn.Open();
            if (db != null)
                conn.ChangeDatabase(db);

            return conn;
        }

        public abstract List<string> GetDatabases();

        public abstract DataTable GetProcedures();

        public abstract string DescribeProcedure(string name);

        public virtual DataTable GetTables()
        {
            DataTable results = new DataTable();

            using (var conn = Get())
            using (var command = conn.CreateCommand())
            using (var adapter = CreateDataAdapter())
            {
                command.CommandText = "select TABLE_NAME as [Table], 0 as [Rows] "
                                        + "from INFORMATION_SCHEMA.TABLES "
                                        + "where " + DB_CHECK + " "
                                        + "order by TABLE_NAME";
                command.AddWithValue("@database", conn.Database);

                adapter.SelectCommand = command;
                adapter.Fill(results);

                foreach (DataRow table in results.Rows)
                {
                    command.CommandText = "select count(*) from [" + table.ItemArray[0] + "]";
                    table.BeginEdit();
                    table[1] = command.ExecuteScalar();
                    table.EndEdit();
                }
            }

            return results;
        }

        private Dictionary<string, Key> GetKeys(string table)
        {
            Dictionary<string, Key> keys = new Dictionary<string, Key>();

            GetForeignKeys(table, keys);
            GetPrimaryKeys(table, keys);

            return keys;
        }

        public IEnumerable<Key> GetPrimaryKeys(string table){
            var keys = new Dictionary<string, Key>();
                   
            GetPrimaryKeys(table, keys);

			return keys.Values;
		}

        protected virtual void GetPrimaryKeys(string table, Dictionary<string, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select [ordinal_position], [column_name] from [INFORMATION_SCHEMA].[INDEXES] "
                        + "where [PRIMARY_KEY] = 1 and [TABLE_NAME] = @tableName "
                        + "and " + DB_CHECK;
                    command.AddWithValue("@tableName", table);
                    command.AddWithValue("@database", conn.Database);

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

        protected virtual void GetForeignKeys(string table, Dictionary<string, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = 
                        "select [parent_column].[ORDINAL_POSITION] as [parent_column_id], [parent_column].[COLUMN_NAME] as [parent_column_name], [referenced_constraint].[TABLE_NAME] as [name], [referenced_key].[COLUMN_NAME] as [key name] "
                        + "from   [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] as [parent_constraints] "

                        + "inner join [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] [parent_key] "
                        + "on [parent_constraints].[CONSTRAINT_SCHEMA] = [parent_key].[CONSTRAINT_SCHEMA] "
                        + "and [parent_constraints].[CONSTRAINT_NAME] = [parent_key].[CONSTRAINT_NAME] "
                        + "inner join [INFORMATION_SCHEMA].[REFERENTIAL_CONSTRAINTS] as [rc] "
                        + "on [parent_constraints].[CONSTRAINT_SCHEMA] = [rc].[CONSTRAINT_SCHEMA] "
                        + "and [parent_constraints].[CONSTRAINT_NAME] = [rc].[CONSTRAINT_NAME] "

                        + "inner join [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] as [referenced_constraint] "
                        + "on [rc].[UNIQUE_CONSTRAINT_SCHEMA] = [referenced_constraint].[CONSTRAINT_SCHEMA] "
                        + "and [rc].[UNIQUE_CONSTRAINT_NAME] = [referenced_constraint].[CONSTRAINT_NAME] "
                        + "inner join [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] as [referenced_key] "
                        + "on [referenced_constraint].[CONSTRAINT_SCHEMA] = [referenced_key].[CONSTRAINT_SCHEMA] "
                        + "and [referenced_constraint].[CONSTRAINT_NAME] = [referenced_key].[CONSTRAINT_NAME] "
                        + "and [parent_key].[ORDINAL_POSITION] = [referenced_key].[ORDINAL_POSITION] "

                        + "inner join [INFORMATION_SCHEMA].[COLUMNS] as [parent_column] "
                        + "on [parent_constraints].[TABLE_NAME] = [parent_column].[TABLE_NAME] "
                        + "and [parent_key].[COLUMN_NAME] = [parent_column].[COLUMN_NAME] "
                        + "where [parent_constraints].[CONSTRAINT_TYPE] = 'FOREIGN KEY' "
                        + "and [parent_constraints].[TABLE_NAME] = @tableName "
                        + "and (([parent_constraints].[TABLE_CATALOG] is null and [parent_constraints].[TABLE_SCHEMA] is null) or ([parent_constraints].[TABLE_CATALOG] is null and [parent_constraints].[TABLE_SCHEMA] = @database) or [parent_constraints].[TABLE_CATALOG] = @database) ";

                    command.AddWithValue("@tableName", table);
                    command.AddWithValue("@database", conn.Database);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Key key = new Key
                            {
                                Type = KeyType.Foreign,
                                Column = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                ForeignTable = reader.GetString(2),
                                ForeignColumn = reader.GetString(3),
                            };
                            keys[key.Name] = key;
                        }

                        reader.Close();
                    }
                }
            }
        }

        protected virtual void GetColumns(Table table)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select [ORDINAL_POSITION], [COLUMN_NAME], [DATA_TYPE], [IS_NULLABLE] "
                        + "from [INFORMATION_SCHEMA].[COLUMNS] "
                        + "where [TABLE_NAME] = @tableName "
                        + "and "+DB_CHECK;
                    command.AddWithValue("@tableName", table.Name);
                    command.AddWithValue("@database", conn.Database);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            table.Columns.Add(new Column
                            {
                                Table = table,
                                Position = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Type = reader.GetString(2) +
                                       (reader.GetString(3).Equals("YES") ? " (nullable)" : ""),
                            });
                        }

                        reader.Close();
                    }
                }
                conn.Close();
            }
        }

        public Table GetTable(string name)
        {
            Dictionary<string, Key> keys = GetKeys(name);
            Table table = new Table
            {
                Name = name
            };

            GetColumns(table);

            foreach (var column in table.Columns.Where(c => keys.ContainsKey(c.Name)))
            {
                Key key = keys[column.Name];
                column.Key = key.Type;
                column.ForeignTable = key.ForeignTable;
                column.ForeignColumn = key.ForeignColumn;
                if (key.Type == KeyType.Primary)
                    table.Primary.Add(column);
            }

            return table;
        }

        public DataTable GetTableData(string name, int? max, string where)
        {
            DataSet data = new DataSet();

            using (var conn = Get())
            using (var command = conn.CreateCommand())
            using (var adapter = CreateDataAdapter())
            {
                command.CommandText = String.Format(
                    "select {2} * from [{0}] {1}",
                    name,
                    String.IsNullOrWhiteSpace(where)? "" : "where "+where,
                    max == null ? "" : "top " + max);
                adapter.SelectCommand = command;
                adapter.Fill(data);
            }

            return data.Tables.Count > 0? data.Tables[0] : new DataTable();
        }

        protected virtual string PrimaryKeysClause(Table table, DbCommand command, DataRow row) {
            StringBuilder clause = new StringBuilder();

            if(table.Primary.Count > 0) {
                for(int idx = 0; idx < table.Primary.Count; idx += 1) {
                    var primary = table.Primary[idx];
                    clause.AppendFormat("[{0}] = @key_{0} ", primary.Name);
                    if(idx < table.Primary.Count - 1)
                        clause.Append("and ");
                    command.AddWithValue("@key_" + primary.Name, row[primary.Name, DataRowVersion.Original]);
                }
            }
            else {
                for(int idx = 0; idx < row.Table.Columns.Count; idx += 1) {
                    DataColumn column = row.Table.Columns[idx];
                    clause.AppendFormat("[{0}] = @key_{0} {1}", column.ColumnName,
                                        idx < row.Table.Columns.Count - 1 ? "and" : "");
                    command.AddWithValue("@key_" + column.ColumnName, row[column, DataRowVersion.Current]);
                }
            }

            return clause.ToString();
        }

        public virtual void UpdateRow(DataRow row, Table table)
        {
            using (var conn = Get())
            using (var command = conn.CreateCommand())
            {
                StringBuilder update = new StringBuilder();

                update.AppendFormat("update {0} set ", table.Name);

                for(int idx = 0; idx < row.Table.Columns.Count; idx += 1)
                {
                    DataColumn column = row.Table.Columns[idx];
                    if (!table.Primary.Any(p => column.ColumnName.Equals(p.Name)))
                    {
                        update.AppendFormat("[{0}] = @{0},", column.ColumnName);
                        command.AddWithValue("@" + column.ColumnName, row[column, DataRowVersion.Current]);
                    }
                }

                update.Remove(update.Length - 1, 1);
                update.Append(" where ");
                update.Append(PrimaryKeysClause(table, command, row));

                command.CommandText = update.ToString();
                command.ExecuteNonQuery();
            }
        }

        public virtual void DeleteRow(DataRow row, Table table)
        {
            using (var conn = Get())
            using (var command = conn.CreateCommand())
            {
                StringBuilder delete = new StringBuilder();

                delete.AppendFormat("delete from {0} where ", table.Name);
                delete.Append(PrimaryKeysClause(table, command, row));

                command.CommandText = delete.ToString();
                command.ExecuteNonQuery();
            }
        }

        public virtual void InsertRow(DataRow row, Table table)
        {
            using (var conn = Get())
            using (var command = conn.CreateCommand())
            {
                StringBuilder insert = new StringBuilder();
                StringBuilder columns = new StringBuilder();
                StringBuilder values = new StringBuilder();


                for (int idx = 0; idx < row.Table.Columns.Count; idx += 1)
                {
                    DataColumn column = row.Table.Columns[idx];

                    columns.AppendFormat("[{0}],", column.ColumnName);
                    values.AppendFormat("@{0},", column.ColumnName);
                    command.AddWithValue("@" + column.ColumnName, row[column, DataRowVersion.Current]);
                }

                columns.Remove(columns.Length - 1, 1);
                values.Remove(values.Length - 1, 1);

                insert.AppendFormat("insert into {0} ({1}) values ({2})", table.Name, columns, values);

                command.CommandText = insert.ToString();
                command.ExecuteNonQuery();
            }
        }
    }

    public enum KeyType
    {
        None,
        Primary,
        Foreign,
        Unique,
        Comosite
    }

    public class Key
    {
        public KeyType Type { get; set; }
        public int Column { get; set; }
        public string Name { get; set; }
        public string ForeignTable { get; set; }
        public string ForeignColumn { get; set; }
    }

    public class Table
    {
        public string Name { get; set; }
        public IList<Column> Columns { get; set; }
        public IList<Column> Primary { get; set; }

        public Table()
        {
            Columns = new List<Column>();
            Primary = new List<Column>();
        }
    }

    public class Column
    {
        public int Position { get; set; }
        public Table Table { get; set; }
        public KeyType Key { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ForeignTable { get; set; }
        public string ForeignColumn { get; set; }
    }
}

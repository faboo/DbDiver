using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;

namespace DbDiver
{
    public class Connection
    {
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

        protected virtual DbConnection Create(string connString)
        {
            return new SqlConnection(connString);
        }

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

        public virtual DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public virtual List<string> GetDatabases()
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

        public virtual DataTable GetProcedures()
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

        public virtual DataTable GetTables()
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

        private Dictionary<int, Key> GetKeys(string table)
        {
            Dictionary<int, Key> keys = new Dictionary<int, Key>();

            GetForeignKeys(table, keys);
            GetPrimaryKeys(table, keys);

            return keys;
        }

        protected virtual void GetPrimaryKeys(string table, Dictionary<int, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT ordinal_position "
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
        }

        protected virtual void GetForeignKeys(string table, Dictionary<int, Key> keys)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {

                    command.CommandText = 
                        "select [parent_column].[ORDINAL_POSITION] as [parent_column_id], [referenced_constraint].[TABLE_NAME] as [name], [referenced_key].[COLUMN_NAME] as [key name] "
                        + "from   [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] [parent_constraints] "

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
                        + "and [parent_constraints].[TABLE_NAME] = @tableName";

                    command.AddWithValue("@tableName", table);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Key key = new Key
                            {
                                Type = KeyType.Foreign,
                                Column = reader.GetInt32(0),
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

        protected virtual void GetColumns(Table table)
        {
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select [ORDINAL_POSITION], [COLUMN_NAME], [DATA_TYPE], [IS_NULLABLE] "
                        + "from [INFORMATION_SCHEMA].[COLUMNS] where [TABLE_NAME] = @tableName";
                    command.AddWithValue("@tableName", table.Name);

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
            Dictionary<int, Key> keys = GetKeys(name);
            Table table = new Table
            {
                Name = name
            };

            GetColumns(table);

            foreach (var column in table.Columns.Where(c => keys.ContainsKey(c.Position)))
            {
                Key key = keys[column.Position];
                column.Key = key.Type;
                column.ForeignTable = key.ForeignTable;
                column.ForeignColumn = key.ForeignColumn;
                if (key.Type == KeyType.Primary)
                    table.Primary = column;
            }

            return table;
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
        public string ForeignTable { get; set; }
        public string ForeignColumn { get; set; }
    }

    public class Table
    {
        public string Name { get; set; }
        public IList<Column> Columns { get; set; }
        public Column Primary { get; set; }

        public Table()
        {
            Columns = new List<Column>();
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

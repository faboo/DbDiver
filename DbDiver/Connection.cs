using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;

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
                    command.CommandText = "select [column_name] from [INFORMATION_SCHEMA].[INDEXES] "
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
                                Column = reader.GetString(0),
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

        private void GetForeignKeys(string table, Dictionary<string, Key> keys, bool outward = true)
        {
            foreach(var key in GetForeignKeys(table, outward))
                keys[key.Column] = key;
        }

        public virtual IEnumerable<Key> GetForeignKeys(string table, bool outward)
        {
            List<Key> keys = new List<Key>();
            using (var conn = Get())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = 
                        "select "
                        + (outward
                            ? "[parent_key].[COLUMN_NAME] as [parent_column], [referenced_constraint].[TABLE_NAME] as [referenced_table], [referenced_key].[COLUMN_NAME] as [referenced_column] "
                            : "[referenced_key].[COLUMN_NAME] as [parent_column], [parent_constraint].[TABLE_NAME] as [referenced_table], [parent_key].[COLUMN_NAME] as [referenced_column] ")
                        + "from   [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] as [parent_constraint] "

                        + "inner join [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] as [parent_key] "
                        + "on [parent_constraint].[CONSTRAINT_SCHEMA] = [parent_key].[CONSTRAINT_SCHEMA] "
                        + "and [parent_constraint].[CONSTRAINT_NAME] = [parent_key].[CONSTRAINT_NAME] "
                        
                        + "inner join [INFORMATION_SCHEMA].[REFERENTIAL_CONSTRAINTS] as [rc] "
                        + "on [parent_constraint].[CONSTRAINT_SCHEMA] = [rc].[CONSTRAINT_SCHEMA] "
                        + "and [parent_constraint].[CONSTRAINT_NAME] = [rc].[CONSTRAINT_NAME] "

                        + "inner join [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] as [referenced_constraint] "
                        + "on [rc].[UNIQUE_CONSTRAINT_SCHEMA] = [referenced_constraint].[CONSTRAINT_SCHEMA] "
                        + "and [rc].[UNIQUE_CONSTRAINT_NAME] = [referenced_constraint].[CONSTRAINT_NAME] "

                        + "inner join [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] as [referenced_key] "
                        + "on [referenced_constraint].[CONSTRAINT_SCHEMA] = [referenced_key].[CONSTRAINT_SCHEMA] "
                        + "and [referenced_constraint].[CONSTRAINT_NAME] = [referenced_key].[CONSTRAINT_NAME] "
                        + "and [parent_key].[ORDINAL_POSITION] = [referenced_key].[ORDINAL_POSITION] "

                        + "where [parent_constraint].[CONSTRAINT_TYPE] = 'FOREIGN KEY' "
                        + "and (([parent_constraint].[TABLE_CATALOG] is null and [parent_constraint].[TABLE_SCHEMA] is null) or ([parent_constraint].[TABLE_CATALOG] is null and [parent_constraint].[TABLE_SCHEMA] = @database) or [parent_constraint].[TABLE_CATALOG] = @database) "
                        + (outward
                            ? "and [parent_constraint].[TABLE_NAME] = @tableName "
                            : "and [referenced_constraint].[TABLE_NAME] = @tableName ");

                    command.AddWithValue("@tableName", table);
                    command.AddWithValue("@database", conn.Database);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Key key = new Key
                            {
                                Type = KeyType.Foreign,
                                Column = reader.GetString(0),
                                ForeignTable = reader.GetString(1),
                                ForeignColumn = reader.GetString(2),
                            };
                            keys.Add(key);
                        }

                        reader.Close();
                    }
                }
            }

            return keys;
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

            foreach (var fk in GetForeignKeys(table.Name, false))
                table.AddDependentKey(fk);

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

        protected virtual void TraceDependents(DbConnection conn, DataRow row, Table table, List<Tuple<Table, DataRow>> rows)
        {
            //var fks = GetForeignKeys(table.Name, false);

            foreach (var fk in table.DependentKeys)
                using (var command = conn.CreateCommand())
                using (var adapter = CreateDataAdapter())
                {
                    DataSet results = new DataSet();
                    DataRow foreignRow;

                    command.Parameters.Clear();
                    command.CommandText = String.Format(
                        "select * from {0} where {1} = @key",
                        fk.ForeignTable,
                        fk.ForeignColumn);
                    command.AddWithValue("@key", row[fk.Column, DataRowVersion.Original]);

                    adapter.SelectCommand = command;
                    adapter.Fill(results);

                    if (results.Tables[0].Rows.Count > 0)
                    {
                        var foreignTable = GetTable(fk.ForeignTable);
                        foreignRow = results.Tables[0].Rows[0];

                        table.DependentKeys.Add(fk);

                        rows.Add(new Tuple<Table, DataRow>(foreignTable, foreignRow));
                        TraceDependents(conn, foreignRow, foreignTable, rows);
                    }
                }
        }

        private Queue<Tuple<Table, DataRow>> TraceDependents(DataRow row, Table table)
        {
            var rows = new List<Tuple<Table, DataRow>>();
            var sortedTables = new List<Table>();
            var sortedRows = new Queue<Tuple<Table, DataRow>>();

            using (var conn = Get())
                TraceDependents(conn, row, table, rows);

            while (rows.Count > 0)
            {
                var elligible = 
                    // All the rows...
                    rows.FindAll(r =>
                        // whose dependent tables...
                        r.Item1.DependentKeys.All(fk =>
                            // are all ready sorted out.
                            sortedTables.Any(t =>
                                t.Name.Equals(fk.ForeignTable))));
                var tables = elligible.Select(e => e.Item1).Distinct();

                sortedTables.AddRange(tables);

                foreach (var elligibleRow in elligible)
                {
                    rows.Remove(elligibleRow);
                    sortedRows.Enqueue(elligibleRow);
                }
            }

            return sortedRows;
        }

        protected virtual void DeleteRow(DbConnection conn, DataRow row, Table table)
        {
            using (var command = conn.CreateCommand())
            {
                StringBuilder delete = new StringBuilder();

                delete.AppendFormat("delete from {0} where ", table.Name);
                delete.Append(PrimaryKeysClause(table, command, row));

                command.CommandText = delete.ToString();
                command.ExecuteNonQuery();
            }
        }

        public void DeleteRow(DataRow row, Table table, bool follow)
        {
            Queue<Tuple<Table, DataRow>> rows = null;

            if(follow)
                rows = TraceDependents(row, table);

			//Although it would be nice to do these deletes in a transaction, it appears that deleted rows aren't necessarily fully deleted, so FK
			//rules keep rows higher up the FK chain from being successfullydeleted.
            //using (var transaction = new TransactionScope())
            using (var conn = Get())
            {
                while (rows != null && rows.Count > 0)
                {
                    var delete = rows.Dequeue();
                    DeleteRow(conn, delete.Item2, delete.Item1);
                }

                DeleteRow(conn, row, table);

              //  transaction.Complete();
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

        public bool HasDependents(Table table)
        {
            return GetForeignKeys(table.Name, false).Count() > 0;
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
        public string Column { get; set; }
        public string ForeignTable { get; set; }
        public string ForeignColumn { get; set; }
    }

    public class Table
    {
        public string Name { get; set; }
        public IList<Column> Columns { get; set; }
        public IList<Column> Primary { get; set; }
        public IList<Key> DependentKeys { get; set; }

        public Table()
        {
            Columns = new List<Column>();
            Primary = new List<Column>();
            DependentKeys = new List<Key>();
        }

        public void AddDependentKey(Key key)
        {
            Column column = Columns.First(c => c.Name.Equals(key.Column));

            DependentKeys.Add(key);
            if (column.Dependents == null)
                column.Dependents = new List<Key>();
            column.Dependents.Add(key);
        }

        public override bool Equals(object obj)
        {
            return obj is Table && (obj as Table).Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Column
    {
        public int Position { get; set; }
        public Table Table { get; set; }
        public KeyType Key { get; set; }
        public List<Key> Dependents { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ForeignTable { get; set; }
        public string ForeignColumn { get; set; }
    }
}

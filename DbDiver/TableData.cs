using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.ObjectModel;

namespace DbDiver
{
    public class TableData
    {
        public TableData(Table table)
        {
            Table = table;
            Data = new List<RowData>();
        }

        public Table Table { get; private set; }
        public string Name { 
            get { return Table.Name; }
        }
        public IList<RowData> Data { get; private set; }

        public void AddData(DataRow row)
        {
            List<RowData> data = Data as List<RowData>;

            data.Add(new RowData(this, data.Count, row));
        }

/*        public override bool Equals(object obj)
        {
            return obj is TableData && (obj as TableData).Name.Equals(Name)
                || obj is Table && (obj as Table).Name.Equals(Name);
        }*/
    }

    public class RowData
    {
        public int Index { get; private set; }
        public IEnumerable<ColumnData> Data { get; private set; }

        public RowData(TableData table, int index, DataRow row)
        {
            Index = index;
            Data = table.Table.Columns.Select(c =>
                    new ColumnData(table, c, row));
        }

        public override string ToString()
        {
            return Index.ToString();
        }
    }

    public class ColumnData : Column
    {
        public ColumnData(TableData set, Column plain, DataRow row)
        {
			Position = plain.Position;
            Set = set;
            Table = set.Table;
			Key = plain.Key;
            Dependents = plain.Dependents;
            Name = plain.Name;
			Type = plain.Type;
			ForeignTable = plain.ForeignTable;
			ForeignColumn = plain.ForeignColumn;
            Data = row[Name];
        }


        public TableData Set { get; private set; }
        public object Data { get; set; }
    }
}

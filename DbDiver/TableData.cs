using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DbDiver
{
    public class RowChangedEventArgs : EventArgs {
        public RowData Row { get; set; }
    }
    public delegate void RowChangedHandler(TableData sender, RowChangedEventArgs args);

    public class TableData
    {
        private Connection connection;
        private RowDataType rowDataType;

        public TableData(Table table, Connection connection)
        {
            Table = table;
            Data = new List<RowData>();
            rowDataType = new RowDataType(Table);
            this.connection = connection;
        }

        public event RowChangedHandler RowChanged;

        public Table Table { get; private set; }
        public string Name { 
            get { return Table.Name; }
        }
        public IList<RowData> Data { get; private set; }
        public void AddData(DataRow row)
        {
            RowData rowData = new RowData(this, rowDataType, Data.Count, row, connection);

            AddData(rowData);
        }

        public void AddData(RowData rowData)
        {
            rowData.Changed += OnRowChanged;
            rowData.Index = Data.Count;

            Data.Add(rowData);
        }

        private void OnRowChanged(object row, EventArgs args){
            if (RowChanged != null)
            {
                RowChanged(this, new RowChangedEventArgs { Row = row as RowData });
            }
        }

        public RowData NewRow()
        {
            return new RowData(this, rowDataType, connection);
        }

/*        public override bool Equals(object obj)
        {
            return obj is TableData && (obj as TableData).Name.Equals(Name)
                || obj is Table && (obj as Table).Name.Equals(Name);
        }*/
    }

    public class RowColumnProperty : PropertyDescriptor
    {
        public Column Column
        {
            get;
            private set;
        }

        public RowColumnProperty(Column column)
            : base(column.Name, new Attribute[0])
        {
            Column = column;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get { return typeof(RowData); }
        }

        public override object GetValue(object component)
        {
            return (component as RowData)[Column.Name];
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return Column.NativeType; }
        }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object component, object value)
        {
            (component as RowData)[Column.Name].Data = (value as ColumnData).Data;
        }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }
    }

    public class RowDataType
    {
        public RowDataType(Table table)
        {
            List<PropertyDescriptor> properties = new List<PropertyDescriptor>();
            Attributes = new AttributeCollection();
            Name = table.Name;
            if (table.Primary.Count > 0)
                DefaultProperty = new RowColumnProperty(table.Primary[0]);
            else
                DefaultProperty = new RowColumnProperty(table.Columns[0]);

            foreach(var column in table.Columns)
                if((DefaultProperty as RowColumnProperty).Column.Equals(column))
                    properties.Add(DefaultProperty);
                else
                    properties.Add(new RowColumnProperty(column));

            Properties = new PropertyDescriptorCollection(properties.ToArray());
        }

        public AttributeCollection Attributes { get; private set; }
        public string Name { get; private set; }
        public PropertyDescriptor DefaultProperty { get; private set; }
        public PropertyDescriptorCollection Properties { get; private set; }
    }

    public class RowData : ICustomTypeDescriptor
    {
        private Connection connection;
        private RowDataType typeData;

        public int Index { get; set; }
        public IEnumerable<ColumnData> Data { get; private set; }
        private Dictionary<string, int> ColumnIndex { get; set; }
        private Dictionary<string, bool> ModifiedColumns { get; set; }
        public bool New { get; set; }

        public ColumnData this[Column column]
        {
            get { return (Data as ColumnData[])[ColumnIndex[column.Name]]; }
        }
        public ColumnData this[string column]
        {
            get { return (Data as ColumnData[])[ColumnIndex[column]]; }
        }
        public ColumnData this[int index]
        {
            get { return (Data as ColumnData[])[index]; }
        }

        public event EventHandler Changed;

        public RowData(TableData table, RowDataType type, int index, DataRow row, Connection connection)
            : this(table, type, connection)
        {
            int columnIndex = 0;

            Index = index;
            New = false;

            Data = table.Table.Columns.Select(c =>
            {
                ColumnIndex[c.Name] = columnIndex++;
                var column = new ColumnData(table, c, row, connection);
                column.Changed += OnValueChanged;
                return column;
            }).ToArray();
        }

        public RowData(TableData table, RowDataType type, Connection connection)
        {
            typeData = type;
            ColumnIndex = new Dictionary<string, int>();
            ModifiedColumns = new Dictionary<string, bool>();
            New = true;

            this.connection = connection;
        }

        private void OnValueChanged(object column, EventArgs args){
            if (Changed != null)
            {
                Changed(this, new EventArgs());
                ModifiedColumns[(column as ColumnData).Name] = true;
            }
        }

        public bool ColumnModified(ColumnData column)
        {
            return ColumnModified(column.Name);
        }

        public bool ColumnModified(string name)
        {
            return ModifiedColumns.ContainsKey(name) && ModifiedColumns[name];
        }

        public override string ToString()
        {
            return Index.ToString();
        }

        public AttributeCollection GetAttributes()
        {
            return typeData.Attributes;
        }

        public string GetClassName()
        {
            return typeData.Name;
        }

        public string GetComponentName()
        {
            return Index.ToString();
        }

        public TypeConverter GetConverter()
        {
            return null;
        }

        public EventDescriptor GetDefaultEvent()
        {
            return null;
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return typeData.DefaultProperty;
        }

        public object GetEditor(Type editorBaseType)
        {
            return null;
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            // Should this be something else?
            return EventDescriptorCollection.Empty;
        }

        public EventDescriptorCollection GetEvents()
        {
            // Should this be something else?
            return EventDescriptorCollection.Empty;
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return typeData.Properties;
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return typeData.Properties;
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            // This is a silly property.
            return this;
        }
    }

    public class ColumnData : Column
    {
        private object data;

        public ColumnData(TableData set, Column plain, DataRow row, Connection connection)
        {
			Position = plain.Position;
            Set = set;
            Table = set.Table;
            Name = plain.Name;
            Data = row[Name];
			ForeignTable = plain.ForeignTable;
			ForeignColumn = plain.ForeignColumn;
			Key = plain.Is(KeyType.Foreign)
                && connection.RowExists(ForeignTable, ForeignColumn, Data)
                ? plain.Key | KeyType.Broken
                : plain.Key;
            Dependents = plain.Dependents;
			Type = plain.Type;
        }

        public event EventHandler Changed;

        public TableData Set { get; private set; }
        public object Data
        {
            get { return data; }
            set
            {
                data = value;
                if(Changed != null)
                    Changed(this, new EventArgs());
            }
        }
    }
}

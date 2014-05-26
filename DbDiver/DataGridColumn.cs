using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DbDiver
{
    public class DataGridColumn : DataGridTemplateColumn
    {
		public static readonly DependencyProperty ColumnNameProperty = DependencyProperty.Register("ColumnName", typeof(string), typeof(DataGridColumn));

		public string ColumnName
		{
			get { return (string)GetValue(ColumnNameProperty); }
			set { SetValue(ColumnNameProperty, value); }
		}

        public DataGridColumn()
        {
            CellTemplateSelector = DataDisplaySelector.Instance;
            CellEditingTemplateSelector = DataEditSelector.Instance;
        }

        protected override System.Windows.FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            RowData row = dataItem as RowData;
            ContentPresenter presenter = new ContentPresenter
                {
                    ContentTemplate = CellTemplateSelector.SelectTemplate(row[ColumnName], cell),
                };

            // Reset the Binding to the specific column. The default binding is to the DataRowView.
            BindingOperations.SetBinding(
                presenter,
                ContentPresenter.ContentProperty,
                new Binding(this.ColumnName));
            return presenter;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            RowData row = dataItem as RowData;
            ContentPresenter presenter = new ContentPresenter
            {
                ContentTemplate = CellEditingTemplateSelector.SelectTemplate(row[ColumnName], cell),
            };

            // Reset the Binding to the specific column. The default binding is to the DataRowView.
            BindingOperations.SetBinding(
                presenter,
                ContentPresenter.ContentProperty,
                new Binding(this.ColumnName));
            return presenter;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;

namespace DbDiver
{
    public static class DbParametersCollectionExtension
    {
        public static void AddWithValue(this DbCommand command, string name, object value)
        {
            DbParameter param;

            if (command is SqlCommand)
                param = new SqlParameter();
            else if (command is SqlCeCommand)
                param = new SqlCeParameter();
            else
                throw new Exception("Can't add value to type: " + command.GetType());

            param.ParameterName = name;
            param.Value = value;

            command.Parameters.Add(param);
        }
    }
}

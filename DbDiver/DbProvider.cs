using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using MySql.Data.MySqlClient;

namespace DbDiver
{
    using Provider = Tuple<string, DbProviderFactory, Func<NetworkInstance>>;

    // This may be taking the whole abstraction thing a little too far.
    public class DbProvider
    {
        public static List<Provider> NetworkProviders
        {
            get
            {
                return new List<Provider>
                           {
                               new Provider("MS SQL", SqlClientFactory.Instance, () => new SqlInstance()),
                               new Provider("MySQL", MySqlClientFactory.Instance, () => new MySqlInstance()),
                           };
            }
        }

        public static List<FileInstance> FileProviders
        {
            get
            {
                return new List<FileInstance>
                           {
                               new CeInstance()
                           };
            }
        }

        public static List<NetworkInstance> GetPublicInstances()
        {
            List<NetworkInstance> instances = new List<NetworkInstance>();

            foreach (var provider in NetworkProviders.Where(p => p.Item2.CanCreateDataSourceEnumerator))
            {
                DataTable sources = provider.Item2.CreateDataSourceEnumerator().GetDataSources();

                foreach (DataRow source in sources.Rows){
                    var instance = provider.Item3();

                    instance.Server = source.Field<string>("ServerName");
                    instance.Instance = source.Field<string>("InstanceName");

                    instances.Add(instance);
				}
			}

            return instances;
        }
    }
}

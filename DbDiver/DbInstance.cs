using System;

namespace DbDiver
{
    public abstract class DbInstance
    {
        protected abstract string Type { get; }
        public abstract string Name { get; }
    }

    public abstract class NetworkInstance : DbInstance
    {
        public string Server { get; set; }
        public string Instance { get; set; }
        public string Port { get; set; }
        public override string Name
        {
            get { return String.Format("{0} ({1}) {2}", Server, Instance, Type); }
        }

        public abstract Connection CreateConnection(string username, string password);
    }

    public abstract class FileInstance : DbInstance
    {
        public abstract string FileType { get; }

        public abstract Connection CreateConnection(string filename);
    }
}

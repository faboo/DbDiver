using System;

namespace DbDiver
{
    public abstract class DbInstance
    {
        protected abstract string Type { get; }
        public string Server { get; set; }
        public string Instance { get; set; }
        public string Port { get; set; }
        public string Name
        {
            get { return String.Format("{0} ({1}) {2}", Server, Instance, Type); }
        }

        public abstract Connection CreateConnection(string username, string password);
    }
}

using System;

namespace DbDiver
{
    public class CeInstance : FileInstance
    {
        public override string FileType
        {
            get { return ".sdf"; }
        }

        protected override string Type
        {
            get { return "MS SQL Ce"; }
        }

        public override string Name
        {
            get { return Type+" files"; }
        }

        public override Connection CreateConnection(string filename)
        {
            return new CeConnection
            {
                ConnectionString = String.Format(
                    "Data Source={0}",
                    filename)
            };
        }
    }
}

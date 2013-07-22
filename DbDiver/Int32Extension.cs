using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbDiver
{
    public static class Int32Extension
    {
        public static int ToInt(this object obj)
        {
            int value;

            if (obj is long)
                value = (int)(long)obj;
            else if (obj is int)
                value = (int)(int)obj;
            else if (obj is short)
                value = (int)(short)obj;
            else if (obj is byte)
                value = (int)(byte)obj;
            else
                throw new ArgumentException("Value is " + obj.GetType() + " and not an integer");

            return value;
        }
    }
}

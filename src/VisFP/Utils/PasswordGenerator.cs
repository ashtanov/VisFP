using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisFP.Utils
{
    public class PasswordGenerator
    {
        private static PasswordGenerator _generator;
        private static object _locker = new object();
        private const string chars = "ABCDEFGHIJKLMNOPQRSTUWXYZ1234567890";
        private static Random rand = new Random();
        public static PasswordGenerator Instance
        {
            get
            {
                if (_generator == null)
                    lock (_locker)
                    {
                        if (_generator == null)
                            _generator = new PasswordGenerator();
                    }
                return _generator;
            }
        }

        public string GeneratePassword(int length = 5)
        {
            StringBuilder sb = new StringBuilder();
            lock (_locker)
            {
                for (int i = 0; i < length; ++i)
                    sb.Append(chars[rand.Next(chars.Length)]);
            }
            return sb.ToString();
        }

    }
}

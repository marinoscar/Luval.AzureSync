using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Luval.AzureSync
{
    public class HashProvider
    {
        private readonly MD5CryptoServiceProvider _md5;
        private static HashProvider _instance;

        public HashProvider()
        {
            _md5 = new MD5CryptoServiceProvider();
        }

        public string GetHash(Stream stream)
        {
            string hash;
            using (var bytes = new MemoryStream())
            {
                using (stream)
                {
                    stream.CopyTo(bytes);
                    hash = GetHash(bytes.ToArray());
                }
            }
            return hash;
        }

        public string GetHash(byte[] data)
        {
            var hash = _md5.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        public static HashProvider Instance
        {
            get { return _instance ?? (_instance = new HashProvider()); }
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Luval.AzureSync
{
    public class KeyReader
    {
        #region Variable Declaration
        
        private readonly FileInfo _keyFile;
        public List<KeyData> Keys { get; private set; }

        #endregion

        #region Constructors
        
        public KeyReader(string fileName)
        {
            _keyFile = new FileInfo(fileName);
            if (!_keyFile.Exists)
            {
                _keyFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, fileName)); 
                if(!_keyFile.Exists) throw new ArgumentException(string.Format("Invalid file name {0}", fileName));
            }
            Keys = GetKeys();
        } 

        #endregion

        #region Method Implementation

        public KeyData GetByAccountName(string name)
        {
            return Keys.SingleOrDefault(i => i.Account == name);
        }

        private List<KeyData> GetKeys()
        {
            var serializer = new JsonSerializer();
            using (var reader = _keyFile.OpenText())
            {
                return serializer.Deserialize<List<KeyData>>(new JsonTextReader(reader));
            }
        }  

        #endregion
    }

    public class KeyData
    {
        [JsonProperty("account")]
        public string Account { get; set; }
        [JsonProperty("privateKey")]
        public string PrivateKey { get; set; }
    }
}

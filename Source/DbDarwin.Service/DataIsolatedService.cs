using Newtonsoft.Json;
using Olive;
using System.IO;
using System.IO.IsolatedStorage;

namespace DbDarwin.Service
{
    public class DataIsolatedService
    {
        public static void SaveData(string name, string data)
        {
            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            var filename = name + ".txt";
            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(filename, FileMode.Create, isoStore))
            using (StreamWriter writer = new StreamWriter(isoStream))
                writer.WriteLine(data);
        }

        public static string ReadData(string name)
        {
            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            var filename = name + ".txt";
            if (isoStore.FileExists(filename))
            {
                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(filename, FileMode.Open, isoStore))
                using (StreamReader reader = new StreamReader(isoStream))
                    return reader.ReadToEnd();
            }

            return null;
        }
    }

    public class ConnectionData
    {
        public string Password { get; set; }
        public string UserName { get; set; }

        [JsonIgnore]
        public string Json => JsonConvert.SerializeObject(this);
    }

    public class ManageConnectionData
    {
        public static void SaveConnection(string fileName, string username, string password)
        {
            var data = new ConnectionData
            {
                Password = password,
                UserName = username
            };
            DataIsolatedService.SaveData(fileName, data.Json);
        }

        public static ConnectionData ReadConnection(string fileName)
        {
            var jsonData = DataIsolatedService.ReadData(fileName);
            return jsonData.HasValue() ? JsonConvert.DeserializeObject<ConnectionData>(jsonData) : null;
        }
    }
}

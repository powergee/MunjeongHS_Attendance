using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using TcpCore;
using System.Runtime.Serialization.Formatters.Binary;

namespace 문정고등학교_출석부
{
    public class ProgramSetting
    {
        public static readonly string SETTING_PATH = "Setting.dat";

        public static readonly string LOOPBACK = "127.0.0.1";
        public static readonly string SCHOOLSERVER = "10.65.1.254";

        private Dictionary<string, object> mSettingDict = new Dictionary<string, object>
        {
            { "RememberID", false },
            { "ID", null },
            { "ServerIP", SCHOOLSERVER },
            { "ServerPort", 31006 },
            { "Width", 1280d },
            { "Height", 720d }
        };

        public object this[string key]
        {
            get
            {
                return mSettingDict[key];
            }
            set
            {
                if (mSettingDict.Keys.Contains(key))
                    mSettingDict[key] = value;
                else
                    mSettingDict.Add(key, value);
            }
        }

        private ProgramSetting() { }

        public static ProgramSetting Load()
        {
            return Load(SETTING_PATH);
        }

        public static ProgramSetting Load(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            ProgramSetting setting = new ProgramSetting();

            if (!fileInfo.Exists)
            {
                setting.Save();
            }
            else
            {
                Dictionary<string, object> loaded;
                using (FileStream fs = fileInfo.Open(FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    loaded = formatter.Deserialize(fs) as Dictionary<string, object>;
                }

                foreach (KeyValuePair<string, object> pair in loaded)
                {
                    if (setting.mSettingDict.Keys.Contains(pair.Key))
                    {
                        setting.mSettingDict[pair.Key] = pair.Value;
                    }
                    else
                    {
                        setting.mSettingDict.Add(pair.Key, pair.Value);
                    }
                }
            }

            return setting;
        }

        public void Save()
        {
            using (FileStream fs = new FileStream(SETTING_PATH, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, mSettingDict);
            }
        }
    }
}

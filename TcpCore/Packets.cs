using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Data;
using System.Collections.ObjectModel;

namespace TcpCore
{
    public enum PacketType : byte { Ping, Message, Disconnect, RSAPublic, AESKey, AESIV, RequestLogin, LoginResult,
        FileSendStart, FileSending, FileSendEnd, RequestDataSet, DataSetResult, DataInsert, DataUpdate, DataInsertCompleted, Register, RegisterResult, DataDelete }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Packet
    {
        public static readonly int SIZE = 5;

        public PacketType Type { get; set; }
        public int DataLength { get; set; }

        public static readonly Packet PingPacket = new Packet(PacketType.Ping, 0);

        public Packet(PacketType type, int dataLength = 0)
        {
            Type = type;
            DataLength = dataLength;
        }

        public static Packet BytesToPacket(byte[] bytes)
        {
            if (bytes.Length == SIZE)
            {
                IntPtr buffer = Marshal.AllocHGlobal(SIZE);
                Marshal.Copy(bytes, 0, buffer, SIZE);
                object retobj = Marshal.PtrToStructure(buffer, typeof(Packet));
                Marshal.FreeHGlobal(buffer);
                return (Packet)retobj;
            }
            else throw new ArgumentException($"Packet을 생성하는데 사용되는 바이트 배열은 길이가 {SIZE}여야 합니다.");
        }

        public byte[] ToBytes()
        {
            IntPtr buffer = Marshal.AllocHGlobal(Packet.SIZE);
            Marshal.StructureToPtr(this, buffer, false);
            byte[] rawData = new byte[Packet.SIZE];
            Marshal.Copy(buffer, rawData, 0, Packet.SIZE);
            Marshal.FreeHGlobal(buffer);
            return rawData;
        }

        public override bool Equals(object obj)
        {
            if (obj is Packet)
            {
                Packet str = (Packet)obj;
                return this.Type == str.Type && this.DataLength == str.DataLength;
            }
            else return false;
        }

        public bool Equals(Packet packet)
        {
            return this.Type == packet.Type && this.DataLength == packet.DataLength;
        }

        public override int GetHashCode()
        {
            var hashCode = 1277732778;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + DataLength.GetHashCode();
            return hashCode;
        }

        public static bool operator == (Packet p1, Packet p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Packet p1, Packet p2)
        {
            return !p1.Equals(p2);
        }
    }


    [Serializable]
    public class ImplementedSerialization<T> where T : class
    {
        public byte[] Serialize()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            byte[] buffer;

            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, this);
                buffer = ms.ToArray();
            }

            return buffer;
        }

        public static T Deserialize(byte[] data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            T ser;

            using (MemoryStream ms = new MemoryStream(data))
                ser = formatter.Deserialize(ms) as T;

            return ser;
        }

        public static T Deserialize(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (!s.CanRead)
                throw new ArgumentException("스트림에 데이터를 쓸 수 없습니다.");

            BinaryFormatter formatter = new BinaryFormatter();
            T ser = formatter.Deserialize(s) as T;

            return ser;
        }
    }

    [Serializable]
    // FileSendStart시에 데이터로 제공됨.
    public class TcpFileInfo : ImplementedSerialization<TcpFileInfo>, ICloneable
    {
        public string Name { get; set; }
        public long Length { get; set; }

        public TcpFileInfo(string name, long length)
        {
            Name = name;
            Length = length;
        }

        public object Clone()
        {
            return new TcpFileInfo(Name, Length);
        }
    }

    public class TcpTransferNotifier
    {
        public long FullSize { get; }
        public long Completed { get; private set; }
        public bool IsFinished => FullSize == Completed;

        public TcpTransferNotifier(long full)
        {
            FullSize = full;
            Completed = 0;
        }

        public void Add(long size)
        {
            if (Completed + size > FullSize)
                Completed = FullSize;
            else Completed += size;
        }

        public double Percent
        {
            get
            {
                return (double)Completed / (double)FullSize * 100d;
            }
        }
    }

    [Serializable]
    public class TcpLoginInfo : ImplementedSerialization<TcpLoginInfo>
    {
        public string ID { get; }
        public string Password { get; }

        public TcpLoginInfo(string id, string pw)
        {
            ID = id ?? throw new ArgumentNullException("id");
            Password = pw ?? throw new ArgumentNullException("pw");
        }
    }

    [Serializable]
    public class TcpLoginResult : ImplementedSerialization<TcpLoginResult>
    {
        public string ID { get; }
        public string Name { get; }
        public string Type { get; }
        public bool IDExist { get; }
        public bool PasswordCorrect { get; }
        // 계정이 서버에 승인되었는지 여부
        public bool IsAccepted { get; }

        public bool Success => IDExist && PasswordCorrect && IsAccepted;

        public TcpLoginResult(string id, string name, string type, bool idExist, bool pwCorrect, bool isAccepted)
        {
            if (!idExist && pwCorrect)
                throw new ArgumentException("유효하지 않은 인수 구성입니다. ID가 존재하지 않으면서 패스워드가 옳다는 설정은 잘못되었습니다.");
            if (!idExist && isAccepted)
                throw new ArgumentException("유효하지 않은 인수 구성입니다. ID가 존재하지 않으면서 계정이 서버에 의해 승인되었다는 설정은 잘못되었습니다.");

            ID = id ?? throw new ArgumentNullException("id");
            Name = name ?? throw new ArgumentNullException("name");
            Type = type ?? throw new ArgumentNullException("type");

            IDExist = idExist;
            PasswordCorrect = pwCorrect;
            IsAccepted = isAccepted;
        }

        public override string ToString()
        {
            if (IDExist)
            {
                if (!IsAccepted)
                    return "서버에서 아직 승인하지 않은 계정입니다. 서버 관리자에게 승인을 요청하세요.";

                if (PasswordCorrect)
                    return "로그인이 성공하였습니다.";
                else return "비밀번호가 잘못되었습니다.";
            }
            else
            {
                return "ID가 존재하지 않습니다.";
            }
        }
    }

    [Serializable]
    public class TcpAccountInfo : ImplementedSerialization<TcpAccountInfo>
    {
        public string ID { get; }
        public string Password { get; }
        public string Name { get; }
        public string Type { get; }

        public TcpAccountInfo(string id, string pw, string name, string type)
        {
            ID = id ?? throw new ArgumentNullException("id");
            Password = pw ?? throw new ArgumentNullException("pw");
            Name = name ?? throw new ArgumentNullException("name");
            Type = type ?? throw new ArgumentNullException("type");
        }
    }

    [Serializable]
    public class TcpDataSetRequirement : ImplementedSerialization<TcpDataSetRequirement>
    {
        public string[] Columns { get; }
        public string TableName { get; }
        public string Where { get; }
        public string OrderBy { get; }

        public TcpDataSetRequirement(string[] cols, string tableName, string whereStr, string orderByStr)
        {
            Columns = cols ?? throw new ArgumentNullException("cols");

            if (cols.Length == 0)
                throw new ArgumentException("TcpDataSetRequirementdml 생성자에 전달된 cols의 길이가 0입니다.");

            TableName = tableName ?? throw new ArgumentNullException("tableName");
            // null 가능
            Where = whereStr;
            // null 가능
            OrderBy = orderByStr;
        }

        // 빈 객체
        public TcpDataSetRequirement()
        {
            Columns = new string[1] { "" };
            TableName = "";
            Where = "";
        }

        public override bool Equals(object obj)
        {
            var requirement = obj as TcpDataSetRequirement;
            return requirement != null &&
                   requirement.Columns.SequenceEqual(this.Columns) &&
                   TableName == requirement.TableName &&
                   Where == requirement.Where &&
                   OrderBy == requirement.OrderBy;
        }

        public override int GetHashCode()
        {
            var hashCode = 1833619250;
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Columns);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TableName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Where);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(OrderBy);
            return hashCode;
        }
    }

    [Serializable]
    public class TcpDataSetResult : ImplementedSerialization<TcpDataSetResult>
    {
        public bool Success { get; }
        public DataSet Data { get; }
        public string ExceptionMessage { get; }
        public TcpDataSetRequirement Requirement { get; }

        public TcpDataSetResult(DataSet data, TcpDataSetRequirement req)
        {
            if (data == null)
            {
                Success = false;
                data = null;
            }

            else
            {
                Success = true;
                Data = data;
            }

            ExceptionMessage = null;
            Requirement = req;
        }

        public TcpDataSetResult(string message, TcpDataSetRequirement req)
        {
            Success = false;
            Data = null;
            ExceptionMessage = message;
            Requirement = req;
        }
    }

    [Serializable]
    public class DatabaseValues : ImplementedSerialization<DatabaseValues>
    {
        private List<string> ColumnValues = new List<string>();

        public DatabaseValues(params string[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            ColumnValues.AddRange(values);
        }

        public void Add(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("s가 null이거나 빈 문자열입니다.");

            ColumnValues.Add(s);
        }

        public override string ToString()
        {
            if (ColumnValues.Count == 0)
                return "()";

            return $"({string.Join(", ", ColumnValues)})";
        }
    }

    [Serializable]
    public class TcpDataInsert : ImplementedSerialization<TcpDataInsert>
    {
        public string TableName { get; set; }
        public string ColumnsText { get; set; }
        public List<DatabaseValues> Values { get; set; } = new List<DatabaseValues>();
    }

    [Serializable]
    public class TcpDataUpdate : ImplementedSerialization<TcpDataUpdate>
    {
        public string TableName { get; set; }
        public Dictionary<string, string> Setter { get; set; } = new Dictionary<string, string>();
        public string Where { get; set; }
    }

    [Serializable]
    public class TcpDataDelete : ImplementedSerialization<TcpDataDelete>
    {
        public string TableName { get; set; }
        public string Where { get; set; }
    }

    [Serializable]
    public class TcpRegisterResult : ImplementedSerialization<TcpRegisterResult>
    {
        public bool Success { get; set; }
        public TcpAccountInfo AccountInfo { get; set; }

        public TcpRegisterResult(bool success, TcpAccountInfo info)
        {
            Success = success;
            AccountInfo = info ?? throw new ArgumentNullException(nameof(info));
        }
    }
}

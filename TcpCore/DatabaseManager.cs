using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Data;
using System.Data.OleDb;
using System.Collections.ObjectModel;

namespace TcpCore
{
    public static class DatabaseManager
    {
        private static object LOCKER = new object();

        private static readonly string ACCDB_PATH = @"Attendance.accdb";
        private static readonly string CONN_STR = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={ACCDB_PATH};Jet OLEDB:Database Password=MunAtt31010;";

        private static bool mInitialized = false;
        private static OleDbConnection mConnection;

        public static void Initialize()
        {
            if (mInitialized)
                Close();

            lock (LOCKER)
            {
                mConnection = new OleDbConnection(CONN_STR);
                mConnection.Open();
            }

            mInitialized = true;
        }

        public static void Close()
        {
            if (mInitialized)
            {
                lock (LOCKER)
                {
                    mConnection.Close(); 
                }

                mInitialized = false;
            }
        }

        public static TcpLoginResult MatchLoginInfo(TcpLoginInfo info)
        {
            DataSet data = DBSelect("*", "[교사 목록]", $"[교사 ID]={info.ID.ToSQLString()}", null);
            DataTable mainTable = data.Tables[0];
            int rowsCount = mainTable.Rows.Count;

            if (rowsCount == 0)
                return new TcpLoginResult("", "", "", false, false, false);
            else if (rowsCount > 1)
                throw new InvalidOperationException("데이터베이스에 같은 교사 ID가 여러개 있습니다.");

            string id = info.ID;
            string name = mainTable.Rows[0]["성명"].ToString();
            string type = mainTable.Rows[0]["계정 구분"].ToString();
            string correctPWHash = mainTable.Rows[0]["PW 해시"].ToString();
            bool pwCorrect = PasswordHashManager.ValidatePassword(info.Password, correctPWHash);
            bool accepted = (bool)mainTable.Rows[0]["가입 허가 여부"];

            return new TcpLoginResult(id, name, type, true, pwCorrect, accepted);
        }

        public static bool TryToRegister(TcpAccountInfo accountInfo, bool accepted)
        {
            DataSet data = DBSelect("[교사 ID]", "[교사 목록]", $"[교사 ID]={accountInfo.ID.ToSQLString()}", null);

            if (data.Tables[0].Rows.Count != 0)
                return false;

            DBInsert("[교사 목록]", accountInfo.ID.ToSQLString(), PasswordHashManager.HashPassword(accountInfo.Password).ToSQLString(),
                accountInfo.Name.ToSQLString(), accountInfo.Type.ToSQLString(), accepted ? "True" : "False");

            return true;
        }

        public static DataSet DBSelect(string columnsStr, string tableName, string whereStr, string orderByStr)
        {
            if (string.IsNullOrWhiteSpace(columnsStr))
                throw new ArgumentException("columnsStr의 값이 null이거나 공백입니다.");
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName의 값이 null이거나 공백입니다.");

            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"SELECT {columnsStr} FROM {tableName}");

            if (!string.IsNullOrWhiteSpace(whereStr))
                sqlBuilder.Append($" WHERE {whereStr}");

            if (!string.IsNullOrWhiteSpace(orderByStr))
                sqlBuilder.Append($" ORDER BY {orderByStr}");

            DataSet data = new DataSet();

            lock (LOCKER)
            {
                OleDbDataAdapter adapter = new OleDbDataAdapter(sqlBuilder.ToString(), mConnection);
                adapter.Fill(data);
                adapter.Dispose();
            }

            return data;
        }

        public static DataSet DBSelect(TcpDataSetRequirement requirement)
        {
            if (requirement == null)
                throw new ArgumentNullException("requirement");

            return DBSelect(string.Join(", ", requirement.Columns), requirement.TableName, requirement.Where, requirement.OrderBy);
        }

        public static int DBInsert(string tableName, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("테이블명은 공백이거나 null 일 수 없습니다.", nameof(tableName));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            int changed;
            string sql = $"INSERT INTO {tableName} VALUES({string.Join(", ", values)})";

            lock (LOCKER)
            {
                OleDbCommand cmd = new OleDbCommand(sql, mConnection);
                changed = cmd.ExecuteNonQuery(); 
            }

            return changed;
        }

        public static int DBInsert(TcpDataInsert dataInsert)
        {
            if (dataInsert == null)
            {
                throw new ArgumentNullException(nameof(dataInsert));
            }

            string sql;
            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = mConnection;
            int sum = 0;

            if (string.IsNullOrWhiteSpace(dataInsert.ColumnsText))
            {
                foreach (DatabaseValues values in dataInsert.Values)
                {
                    sql = $"INSERT INTO {dataInsert.TableName} VALUES {values.ToString()}";

                    cmd.CommandText = sql;

                    lock (LOCKER)
                    {
                        sum += cmd.ExecuteNonQuery(); 
                    }
                }
            }
            else
            {
                foreach (DatabaseValues values in dataInsert.Values)
                {
                    sql = $"INSERT INTO {dataInsert.TableName} {dataInsert.ColumnsText} VALUES {values.ToString()}";

                    cmd.CommandText = sql;

                    lock (LOCKER)
                    {
                        sum += cmd.ExecuteNonQuery(); 
                    }
                }
            }

            cmd.Dispose();

            return sum;
        }

        public static int DBUpdate(TcpDataUpdate dataUpdate)
        {
            if (dataUpdate == null)
            {
                throw new ArgumentNullException(nameof(dataUpdate));
            }
            if (string.IsNullOrWhiteSpace(dataUpdate.TableName))
                throw new ArgumentException("테이블명은 공백이거나 null 일 수 없습니다.", nameof(dataUpdate.TableName));
            if (string.IsNullOrWhiteSpace(dataUpdate.Where))
                throw new ArgumentException("조건문은 공백이거나 null 일 수 없습니다.", nameof(dataUpdate.Where));
            if (dataUpdate.Setter == null)
                throw new ArgumentNullException(nameof(dataUpdate.Setter));
            if (dataUpdate.Setter.Count == 0)
                throw new ArgumentException("치환할 값이 설정되지 않았습니다.");
            
            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = mConnection;
            int sum = 0;

            StringBuilder sb = new StringBuilder();
            sb.Append($"UPDATE {dataUpdate.TableName} SET ");
            KeyValuePair<string, string>[] setters = dataUpdate.Setter.ToArray();

            for (int i = 0; i < setters.Length; ++i)
            {
                if (i == setters.Length -1)
                {
                    sb.Append($"{setters[i].Key}={setters[i].Value} ");
                }
                else
                {
                    sb.Append($"{setters[i].Key}={setters[i].Value}, ");
                }
            }

            sb.Append($"WHERE {dataUpdate.Where}");

            cmd.CommandText = sb.ToString();

            lock (LOCKER)
            {
                sum += cmd.ExecuteNonQuery();
            }

            return sum;
        }

        public static int DBDelete(TcpDataDelete dataDelete)
        {
            if (dataDelete == null)
            {
                throw new ArgumentNullException(nameof(dataDelete));
            }
            if (string.IsNullOrWhiteSpace(dataDelete.TableName))
                throw new ArgumentException("테이블명은 공백이거나 null 일 수 없습니다.", nameof(dataDelete.TableName));
            if (string.IsNullOrWhiteSpace(dataDelete.Where))
                throw new ArgumentException("조건문은 공백이거나 null 일 수 없습니다.", nameof(dataDelete.Where));

            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = mConnection;
            cmd.CommandText = $"DELETE FROM {dataDelete.TableName} WHERE {dataDelete.Where}";

            int count;
            lock (LOCKER)
            {
                count = cmd.ExecuteNonQuery();
            }

            cmd.Dispose();

            return count;
        }
    }

    public static class StringExtension
    {
        public static string ToSQLString(this string str)
        {
            return $"\"{str}\"";
        }
    }

    public static class DateTimeExtension
    {
        public static string ToSQLString(this DateTime date)
        {
            return $"#{date.Year}-{date.Month}-{date.Day}#";
        }
    }
}

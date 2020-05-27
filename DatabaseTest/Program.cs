using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpCore;

namespace DatabaseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            DatabaseManager.Initialize();

            TcpAccountInfo accInfo = new TcpAccountInfo("root", "123qweasd", "김정현", "Super");

            if (DatabaseManager.TryToRegister(accInfo, false))
                Console.WriteLine("root 계정을 추가하였습니다.");
            else Console.WriteLine("root 계정이 이미 있으므로 추가하지 않았습니다.");

            TcpLoginInfo loginInfo1 = new TcpLoginInfo("wrong", "123qweasd");
            TcpLoginInfo loginInfo2 = new TcpLoginInfo("root", "wrong");
            TcpLoginInfo loginInfo3 = new TcpLoginInfo("root", "123qweasd");

            Console.WriteLine(DatabaseManager.MatchLoginInfo(loginInfo1).ToString());
            Console.WriteLine(DatabaseManager.MatchLoginInfo(loginInfo2).ToString());
            Console.WriteLine(DatabaseManager.MatchLoginInfo(loginInfo3).ToString());

            TcpDataInsert insert = new TcpDataInsert();
            List<DatabaseValues> values = new List<DatabaseValues>();

            values.Add(new DatabaseValues("#2000-8-14#", "99".ToSQLString(), "99".ToSQLString(), "99".ToSQLString(), "김정현".ToSQLString(), "1", "1", "1", "1", "1", "1", "1", "1", "1"));
            values.Add(new DatabaseValues("#2000-8-14#", "99".ToSQLString(), "99".ToSQLString(), "100".ToSQLString(), "김정현".ToSQLString(), "1", "1", "1", "1", "1", "1", "1", "1", "1"));
            values.Add(new DatabaseValues("#2000-8-14#", "99".ToSQLString(), "99".ToSQLString(), "101".ToSQLString(), "김정현".ToSQLString(), "1", "1", "1", "1", "1", "1", "1", "1", "1"));

            insert.ColumnsText = "";
            insert.TableName = "출석부";
            insert.Values = values;

            Console.WriteLine($"출석부에 {DatabaseManager.DBInsert(insert)}개 줄이 추가됨.");

            TcpDataUpdate update = new TcpDataUpdate();
            update.TableName = "출석부";
            update.Setter.Add("[0교시]", "2");
            update.Setter.Add("[1교시]", "2");
            update.Setter.Add("[2교시]", "2");
            update.Setter.Add("[3교시]", "2");
            update.Where = "[일자]=#2000-8-14# AND 학년=\"99\" AND 반=\"99\" AND 번호=\"99\"";


            Console.WriteLine($"출석부에서 {DatabaseManager.DBUpdate(update)}개 줄이 업데이트됨.");

            TcpDataDelete delete = new TcpDataDelete();
            delete.TableName = "[출석부]";
            delete.Where = "[일자]=#2000-8-14# AND 학년=\"99\" AND 반=\"99\" AND 번호=\"99\"";

            Console.WriteLine($"출석부에서 {DatabaseManager.DBDelete(delete)}개 줄이 삭제됨.");

            DatabaseManager.Close();
        }
    }
}

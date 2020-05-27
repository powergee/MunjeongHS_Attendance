using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 문정고등학교_출석부
{
    public enum AccessType { Inquire, Edit }
    public enum ObjectToAccess { Attendance, Accounts, StudentList }

    public static class AuthorityChecker
    {
        public static bool CheckAuthority(string accountType, string gradeToAccess, string classToAccess, AccessType accessType, ObjectToAccess objectToAccess)
        {
            string accountGrade, accountClass;

            if (accountType == "Super")
            {
                switch (objectToAccess)
                {
                    case ObjectToAccess.Attendance:
                        if (accessType == AccessType.Inquire)
                            return true;
                        else return false;

                    case ObjectToAccess.Accounts:
                        return true;

                    case ObjectToAccess.StudentList:
                        if (accessType == AccessType.Inquire)
                            return true;
                        else return false;

                    default:
                        throw new ArgumentException("CheckAuthority 메서드에 전달된 objectToAccess가 올바르지 않은 값입니다.");
                }
            }

            else if (IsHomeType(accountType, out accountGrade, out accountClass))
            {
                switch (objectToAccess)
                {
                    case ObjectToAccess.Attendance:
                        if (accountGrade == gradeToAccess && accountClass == classToAccess)
                            return true;
                        else return false;

                    case ObjectToAccess.Accounts:
                        return false;

                    case ObjectToAccess.StudentList:
                        return true;

                    default:
                        throw new ArgumentException("CheckAuthority 메서드에 전달된 objectToAccess가 올바르지 않은 값입니다.");
                }
            }

            else
            {
                return false;
            }
        }

        public static string ToDescription(string accountType)
        {
            string accountGrade, accountClass;

            if (accountType == "Super")
            {
                return "전 학년 총괄 관리자";
            }

            else if (IsHomeType(accountType, out accountGrade, out accountClass))
            {
                return $"{accountGrade}학년 {accountClass}반 담임";
            }

            else
            {
                throw new ArgumentException($"accountType의 내용이 올바르지 않습니다 : {accountType}");
            }
        }

        private static bool IsHomeType(string accountType, out string gradeStr, out string classStr)
        {
            string[] parts = accountType.Split(' ');

            if (parts.Length != 2)
            {
                gradeStr = null;
                classStr = null;
                return false;
            }

            int gradeTemp;
            if (!int.TryParse(parts[0], out gradeTemp))
            {
                gradeStr = null;
                classStr = null;
                return false;
            }

            int classTemp;
            if (!int.TryParse(parts[1], out classTemp))
            {
                gradeStr = null;
                classStr = null;
                return false;
            }

            gradeStr = parts[0];
            classStr = parts[1];
            return true;
        }
    }
}

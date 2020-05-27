using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 문정고등학교_출석부
{
    public static class Attendance
    {
        public enum AttEnum
        { NoData = 1, Attended, Absent, EarlyLeft, Late, NotAppeared }

        public static readonly Dictionary<AttEnum, string> AttToStrDict = new Dictionary<AttEnum, string>()
        {
            { AttEnum.NoData, "입력되지 않음" },
            { AttEnum.Attended, "출석 및 인정결" },
            { AttEnum.Absent, "무단 결석" },
            { AttEnum.EarlyLeft, "무단 조퇴" },
            { AttEnum.Late, "무단 지각" },
            { AttEnum.NotAppeared, "무단 결과" }
        };

        public static readonly Dictionary<string, AttEnum> StrToAttDict = new Dictionary<string, AttEnum>()
        {
            { "입력되지 않음", AttEnum.NoData },
            { "출석 및 인정결", AttEnum.Attended },
            { "무단 결석", AttEnum.Absent },
            { "무단 조퇴", AttEnum.EarlyLeft },
            { "무단 지각", AttEnum.Late },
            { "무단 결과", AttEnum.NotAppeared }
        };
    }
}

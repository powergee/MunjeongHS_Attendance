using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static 문정고등학교_출석부.Attendance;

namespace 문정고등학교_출석부
{
    public class AttOfDay
    {
        private List<AttEnum> mAttEnums = new List<AttEnum>(9);
        public DateTime Date { get; private set; }

        public AttOfDay(DateTime date)
        {
            Date = date;
        }

        public bool HasFaults
        {
            get
            {
                for (int i = 0; i < mAttEnums.Count; ++i)
                {
                    AttEnum att = mAttEnums[i];

                    if (att != AttEnum.NoData && att != AttEnum.Attended)
                        return true;
                }

                return false;
            }
        }

        public void Add(AttEnum att)
        {
            if (mAttEnums.Count >= 9)
                throw new InvalidOperationException("AttOfDay에는 0교시부터 8교시 즉, 9개의 AttEnum만 추가할 수 있습니다.");

            mAttEnums.Add(att);
        }

        public string Description
        {
            get
            {
                if (HasFaults)
                {
                    HashSet<string> faultsDes = new HashSet<string>();

                    bool absent = false;
                    bool late_before = false;
                    bool late_after = false;
                    int earlyLeft_Count = 0;
                    int notAppeared_Count = 0;

                    for (int i = 0; i < mAttEnums.Count; ++i)
                    {
                        switch (mAttEnums[i])
                        {
                            case AttEnum.Absent:
                                absent = true;
                                break;

                            case AttEnum.Late:
                                if (i <= 3)
                                    late_before = true;
                                else
                                    late_after = true;
                                break;

                            case AttEnum.EarlyLeft:
                                ++earlyLeft_Count;
                                break;

                            case AttEnum.NotAppeared:
                                ++notAppeared_Count;
                                break;

                            default:
                                break;
                        }
                    }

                    if (absent)
                        faultsDes.Add("무단 결석");

                    if (late_after)
                        faultsDes.Add("무단 지각 (4교시 이상)");
                    else if (late_before)
                        faultsDes.Add("무단 지각 (3교시 이내)");

                    if (earlyLeft_Count != 0)
                    {
                        if (earlyLeft_Count < 4)
                            faultsDes.Add("무단 조퇴 (4시간 미만 결손)");
                        else
                            faultsDes.Add("무단 조퇴 (4시간 이상 결손)");
                    }

                    if (notAppeared_Count != 0)
                    {
                        if (notAppeared_Count < 4)
                            faultsDes.Add("무단 결과 (4시간 미만 결손)");
                        else
                            faultsDes.Add("무단 결과 (4시간 이상 결손)");
                    }

                    return string.Join(", ", faultsDes);
                }
                else
                {
                    return "불량 사항 없음";
                }
            }
        }

        public int PenaltyPoints
        {
            get
            {
                if (HasFaults)
                {
                    int sum = 0;

                    bool absent = false;
                    bool late_before = false;
                    bool late_after = false;
                    int earlyLeft_Count = 0;
                    int notAppeared_Count = 0;

                    for (int i = 0; i < mAttEnums.Count; ++i)
                    {
                        switch (mAttEnums[i])
                        {
                            case AttEnum.Absent:
                                absent = true;
                                break;

                            case AttEnum.Late:
                                if (i <= 3)
                                    late_before = true;
                                else
                                    late_after = true;
                                break;

                            case AttEnum.EarlyLeft:
                                ++earlyLeft_Count;
                                break;

                            case AttEnum.NotAppeared:
                                ++notAppeared_Count;
                                break;

                            default:
                                break;
                        }
                    }

                    if (absent)
                        sum += 2;

                    if (late_after)
                        sum += 2;
                    else if (late_before)
                        sum += 1;

                    if (earlyLeft_Count != 0)
                    {
                        if (earlyLeft_Count < 4)
                            sum += 1;
                        else
                            sum += 2;
                    }

                    if (notAppeared_Count != 0)
                    {
                        if (notAppeared_Count < 4)
                            sum += 1;
                        else
                            sum += 2;
                    }

                    return sum >= 2 ? 2 : sum;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}

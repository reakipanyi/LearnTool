namespace UnifiedLearningAssistant.Models.ValueObjects
{
    /// <summary>
    /// 学习时间值对象
    /// 表示以分钟为单位的学习时间，确保值非负
    /// </summary>
    public class StudyTime : ValueObject
    {
        public int Minutes { get; }

        public StudyTime(int minutes)
        {
            if (minutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minutes), "学习时间不能为负数");
            }

            Minutes = minutes;
        }

        public StudyTime AddMinutes(int minutesToAdd)
        {
            return new StudyTime(Minutes + minutesToAdd);
        }

        public override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Minutes;
        }

        public override string ToString()
        {
            return $"{Minutes} 分钟";
        }

        public static StudyTime FromMinutes(int minutes)
        {
            return new StudyTime(minutes);
        }

        public static StudyTime FromHoursAndMinutes(int hours, int minutes)
        {
            return new StudyTime(hours * 60 + minutes);
        }
    }
}

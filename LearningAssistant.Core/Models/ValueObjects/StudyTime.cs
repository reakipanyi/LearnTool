namespace LearningAssistant.Models.ValueObjects
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
            var newMinutes = Minutes + minutesToAdd;
            if (newMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minutesToAdd), 
                    $"添加 {minutesToAdd} 分钟后结果为负数，这是不允许的");
            }
            return new StudyTime(newMinutes);
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

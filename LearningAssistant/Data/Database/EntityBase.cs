using System.ComponentModel.DataAnnotations;

namespace LearningAssistant.Data.Database
{
    /// <summary>
    /// 包含审计字段的基类（不定义主键，由子类自行定义）
    /// </summary>
    public abstract class AuditableEntityBase
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 包含用户ID和审计字段的基类（不定义主键，由子类自行定义）
    /// </summary>
    public abstract class UserEntityBase : AuditableEntityBase
    {
        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = string.Empty;
    }
}

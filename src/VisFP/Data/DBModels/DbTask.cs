using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class DbTask
    {
        public Guid TaskId { get; set; }
        [Display(Name = "Название задачи")]
        public string TaskTitle { get; set; }
        [Display(Name = "Номер задачи")]
        public int TaskNumber { get; set; }
        [Display(Name = "Количество попыток")]
        public int MaxAttempts { get; set; }
        [Display(Name = "Баллы за верный ответ")]
        public int SuccessScore { get; set; }
        [Display(Name = "Списание баллов за неудачную попытку")]
        public int FailTryScore { get; set; }
        public bool IsControl { get; set; }

        public Guid TaskTypeId { get; set; }
        public DbTaskType TaskType { get; set; }

        public Guid? TeacherTaskId { get; set; }
        public DbTeacherTaskType TeacherTask { get; set; }

        public Guid ExternalTaskId { get; set; }

        public ICollection<DbTaskProblem> Problems { get; set; }
    }
}

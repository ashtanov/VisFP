using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.StatisticViewModels
{
    public class GroupIdName
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class VariantStat
    {
        public string TasksType { get; set; } //какому типу задач принадлежит вариант
        public Guid Id { get; set; }
        public int SuccessProblems { get; set; }
        public int FailProblems { get; set; }
        public DateTime DateStart { get; set; }
        public int UnfinishedProblems { get; set; }
        public int TotalScore { get; set; }
    }

    public class UserStatViewModel
    {
        [Display(Name = "Логин")]
        public string Login { get; set; }
        [Display(Name = "Группа")]
        public string Group { get; set; }
        [Display(Name = "ФИО")]
        public string RealName { get; set; }
        public string Id { get; set; }
        public IEnumerable<VariantStat> Variants { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.StatisticViewModels
{
    public class GroupStatViewModel
    {
        [Display(Name = "Группа")]
        public string Name { get; set; }
        public Guid Id { get; set; }
        public IEnumerable<string> TasksType { get; set; }
        public IEnumerable<UserStatViewModel> Users { get; set; }
    }
}

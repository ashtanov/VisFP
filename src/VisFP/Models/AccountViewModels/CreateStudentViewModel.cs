using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.AccountViewModels
{
    public class CreateStudentViewModel
    {
        [Required]
        [Display(Name = "ФИО")]
        public string RealName { get; set; }
        [Display(Name = "Дополнительная информация")]
        public string Meta { get; set; }
        public string GroupName { get; set; }
        public Guid GroupId { get; set; }
    }
}

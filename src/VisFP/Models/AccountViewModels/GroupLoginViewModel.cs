using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.AccountViewModels
{
    public class GroupLoginViewModel
    {
        public Guid GroupId { get; set; }
        [Display(Name = "Название группы")]
        public string GroupName { get; set; }
        public IEnumerable<ApplicationUser> Users { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public enum DbRole
    {
        [Display(Name = "Администратор")]
        Admin,
        [Display(Name = "Преподаватель")]
        Teacher,
        [Display(Name = "Пользователь")]
        User
    }
}

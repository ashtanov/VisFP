using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.AccountViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "Логин")]
        public string Login { get; set; }
        [Required]
        [Display(Name = "ФИО")]
        public string RealName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }
        [Required]
        [Display(Name = "Роль")]
        public DbRole Role { get; set; }
        [Display(Name = "Дополнительная информация")]
        public string Meta { get; set; }
        public Guid GroupId { get; set; }
    }
}

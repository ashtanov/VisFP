using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.AdminViewModels
{
    public class CreateUserViewModel
    {
        [Display(Name = "Логин")]
        public string Login { get; set; }
        [Required]
        [Display(Name = "ФИО")]
        public string RealName { get; set; }
        [Display(Name = "Пароль")]
        public string Password { get; set; }
        [Display(Name = "Email")]
        public string Email { get; set; }
        [Display(Name = "Дополнительная информация")]
        public string Meta { get; set; }
    }
}

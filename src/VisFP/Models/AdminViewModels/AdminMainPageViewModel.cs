using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.AdminViewModels
{
    public class UserForView
    {
        [Display(Name = "Логин")]
        public string UserName { get; set; }
        [Display(Name = "Имя пользователя")]
        public string RealName { get; set; }
        [Display(Name = "Роль")]
        public DbRole Role { get; set; }
        [Display(Name = "Дополнительная информация")]
        public string Meta { get; set; }
        public string Id { get; set; }

        public UserForView(ApplicationUser user, DbRole role)
        {
            UserName = user.UserName;
            RealName = user.RealName;
            Role = role;
            Meta = user.Meta;
            Id = user.Id;
        }
    }
    public class AdminMainPageViewModel
    {
        public IEnumerable<UserForView> AllUsers { get; set; }
        public string Error { get; set; }
    }
}

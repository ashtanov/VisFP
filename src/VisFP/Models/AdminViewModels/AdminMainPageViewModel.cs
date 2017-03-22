using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Models.DBModels;

namespace VisFP.Models.AdminViewModels
{
    public class UserForView
    {
        [Display(Name = "Логин")]
        public string UserName { get; set; }
        [Display(Name = "Имя пользователя")]
        public string RealName { get; set; }
        [Display(Name = "Администратор")]
        public bool IsAdmin { get; set; }
        [Display(Name = "Дополнительная информация")]
        public string Meta { get; set; }

        public UserForView(ApplicationUser user, bool isAdmin)
        {
            UserName = user.UserName;
            RealName = user.RealName;
            IsAdmin = isAdmin;
            Meta = user.Meta;
        }
    }
    public class AdminMainPageViewModel
    {
        public IEnumerable<UserForView> AllUsers;
    }
}

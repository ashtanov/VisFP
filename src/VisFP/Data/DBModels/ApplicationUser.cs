using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisFP.Data.DBModels
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string RealName { get; set; }
        public string Meta { get; set; }

        public ICollection<RgTaskProblem> Problems { get; set; }
        public ICollection<UserGroup> OwnedGroups { get; set; } //группы, которые создал пользователь

        
        public Guid UserGroupId { get; set; }
        [ForeignKey(nameof(UserGroupId))]
        public UserGroup UserGroup { get; set; } //группа, к которой привязан пользователь
    }
}

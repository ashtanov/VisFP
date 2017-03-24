using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace VisFP.Data.DBModels
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string RealName { get; set; }
        public string Meta { get; set; }

        public ICollection<RgTaskProblem> Problems { get; set; }
    }
}

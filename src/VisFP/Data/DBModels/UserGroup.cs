using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class UserGroup
    {
        [Key]
        public string GroupId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ApplicationUser Creator { get; set; }

        public ICollection<ApplicationUser> Members { get; set; }
    }
}

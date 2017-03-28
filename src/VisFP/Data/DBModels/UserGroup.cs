using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class UserGroup
    {
        public Guid GroupId { get; set; }
        [Display(Name="Название")]
        public string Name { get; set; }
        [Display(Name = "Описание")]
        public string Description { get; set; }

        public string CreatorId { get; set; }
        public ApplicationUser Creator { get; set; }

        public ICollection<ApplicationUser> Members { get; set; }
        public ICollection<RgTask> RgTasks { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class DbControlVariant
    {
        [Display(Name = "Идентификатор варианта")]
        public Guid VariantId { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsFinished { get; set; }
        public string VariantType { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public ICollection<DbTaskProblem> Problems { get; set; }
    }
}

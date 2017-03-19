using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.DBModels
{
    public class RgTask
    {
        [Key]
        public Guid TaskId { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public int TaskNumber { get; set; }

        public string TaskGrammar { get; set; }
        public string RightAnswer { get; set; }
        public int MaxAttempts { get; set; }
        public ICollection<RgAttempt> Attempts { get; set; }
    }
}

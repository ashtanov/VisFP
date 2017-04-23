using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class DbTeacherTaskType
    {
        public Guid Id { get; set; }

        public string TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; }
        public bool IsAvailable { get; set; }

        public Guid TypeId { get; set; }
        public DbTaskType Type { get; set; }

        public ICollection<DbTask> Tasks { get; set; }
    }
}

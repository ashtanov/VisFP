using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class DbTaskType
    {
        public Guid TaskTypeId { get; set; }
        public string TaskTypeName { get; set; }
        public string TaskTypeNameToView { get; set; }

        public ICollection<DbTask> Tasks { get; set; }
        public ICollection<DbControlVariant> Variants { get; set; }
        public ICollection<DbTeacherTaskType> TeacherTaskTypes { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.TeacherViewModels
{
    public class TaskModuleType
    {
        public string TypeName { get; set; }
        public Guid TypeId { get; set; }
        public string TypeNameForView { get; set; }
        public bool ControlAvailable { get; set; }
        public bool TestAvailable { get; set; }
        public bool ModuleAvailable { get; set; }
    }

    public class TeacherIndexViewModel
    {
        public IEnumerable<UserGroup> Groups { get; set; }
        public IEnumerable<TaskModuleType> Modules { get; set; }
    }
}

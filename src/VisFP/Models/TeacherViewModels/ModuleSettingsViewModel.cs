using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;
using VisFP.Data.DBModels;

namespace VisFP.Models.TeacherViewModels
{
    public class CombinedTaskViewModel
    {
        public DbTask InternalSettings { get; set; }
        public IEnumerable<ITaskSetting> ExternalSettings { get; set; }
    }

    public class ModuleTaskSettingsViewModel
    {
        public IEnumerable<CombinedTaskViewModel> Tasks { get; set; }
    }
}

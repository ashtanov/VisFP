using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.TaskProblemViewModels;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class FsmController : TaskProblemController
    {
        public FsmController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, ILoggerFactory loggerFactory) : base(userManager, dbContext, loggerFactory)
        {
        }

        protected override ITaskModule SetCurrentModule()
        {
            return ModulesRepository.GetModule<FsmTaskModule>();
        }
    }
}

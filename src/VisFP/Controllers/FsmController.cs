using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Models.RGViewModels;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.TaskProblemSharedViewModels;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class FsmController : RegGramController
    {
        public FsmController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            : base(userManager, dbContext, loggerFactory)
        {
            ControllerType = Constants.FsmType;
            AreaName = "Конечные автоматы";
        }

        protected override async Task<RGProblemResult> GetProblem(ApplicationUser user, RgTask template, DbControlVariant variant = null)
        {
            return await (new FSMProblemBuilder(_dbContext)).GenerateProblemAsync(template, user, variant);
        }
    }
}

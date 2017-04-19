using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;

namespace VisFP.Controllers
{
    public class PetryNetController : TaskProblemController
    {
        private ILogger _logger;
        public PetryNetController(
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext dbContext, 
            ILoggerFactory loggerFactory) : base(userManager, dbContext)
        {
            _logger = loggerFactory.CreateLogger<PetryNetController>();
        }

        protected override string AreaName
        {
            get
            {
                return "Сети Петри";
            }
        }

        protected override DbTaskType ControllerTaskType
        {
            get
            {
                return DbWorker.TaskTypes[Constants.PetryNetType];
            }
        }

        protected override ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public override Task<IActionResult> Task(int id, Guid? problemId)
        {
            throw new NotImplementedException();
        }

        protected override Task<ExamVariantViewModel> AddTasksToVariant(ApplicationUser user, DbControlVariant variant)
        {
            throw new NotImplementedException();
        }
    }
}
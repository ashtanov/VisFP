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
    public class RegGramController : TaskProblemController
    {
        protected readonly ILogger _logger;
        private string _areaName;
        private DbTaskType _taskType;
        protected ITaskModule _taskModule;

        protected override string AreaName
        {
            get
            {
                return _areaName ?? (_areaName = "Регулярные грамматики");
            }
        }

        protected override DbTaskType ControllerTaskType
        {
            get
            {
                return _taskType ?? (_taskType = DbWorker.TaskTypes[Constants.RgType]);
            }
        }

        protected override ITaskModule TaskModule
        {
            get
            {
                return _taskModule ?? (
                    _taskModule = 
                    new RgTaskModule(
                        (ApplicationDbContext)HttpContext
                        .RequestServices
                        .GetService(typeof(ApplicationDbContext))) //хз
                        );
            }
        }

        protected override ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public RegGramController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            : base(userManager, dbContext)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        [HttpPost]
        public JsonResult SaveGraph(string graph)
        {
            try
            {
                var d = Newtonsoft.Json.JsonConvert.DeserializeObject(graph);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }
    }
}

using System;
using Microsoft.AspNetCore.Mvc;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class RegGramController : TaskProblemController
    {
        public RegGramController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            : base(userManager, dbContext, loggerFactory)
        {
        }

        protected override RgTaskModule SetCurrentModule()
        {
            return ModulesRepository.GetModule<RgTaskModule>();
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

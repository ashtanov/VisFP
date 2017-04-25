using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class PetryNetController : TaskProblemController
    {
        public PetryNetController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            : base(userManager, dbContext, loggerFactory)
        {
        }

        protected override ITaskModule SetCurrentModule()
        {
            return ModulesRepository.GetModule<PnTaskModule>();
        }
    }
}

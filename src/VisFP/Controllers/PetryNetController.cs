using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VisFP.Data;
using VisFP.Data.DBModels;

namespace VisFP.Controllers
{
    public class PetryNetController : TaskProblemController
    {
        public PetryNetController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) : base(userManager, dbContext)
        {
        }

        protected override string AreaName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override DbTaskType ControllerTaskType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Task<IActionResult> ExamVariant()
        {
            throw new NotImplementedException();
        }

        public override Task<IActionResult> Index()
        {
            throw new NotImplementedException();
        }

        public override Task<IActionResult> Task(int id, Guid? problemId)
        {
            throw new NotImplementedException();
        }
    }
}
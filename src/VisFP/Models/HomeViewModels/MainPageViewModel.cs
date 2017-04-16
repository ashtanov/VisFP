using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;

namespace VisFP.Models.HomeViewModels
{
    public class MainPageViewModel
    {
        public bool IsAdmin { get; set; }
        public bool IsTeacher { get; set; }
        public PetryNetGraph png { get; set; }
    }
}

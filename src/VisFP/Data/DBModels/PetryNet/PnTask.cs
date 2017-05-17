using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class PnTask
    {
        public Guid Id { get; set; }
        public string TaskTitle { get; set; }
        public int TaskNumber { get; set; }
        public TaskAnswerType AnswerType { get; set; }
        public string PetryNetJson { get; set; }
        public string Answers { get; set; }
        public string Question { get; set; }
        public string RightAnswers { get; set; }
        public bool IsSeed { get; set; }
        public bool ShowMarkup { get; set; }
    }
}

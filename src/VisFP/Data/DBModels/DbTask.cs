﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class DbTask
    {
        public Guid TaskId { get; set; }
        [Display(Name = "Название задачи")]
        public string TaskTitle { get; set; }
        [Display(Name = "Номер задачи")]
        public int TaskNumber { get; set; }
        [Display(Name = "Количество попыток")]
        public int MaxAttempts { get; set; }

    }
}
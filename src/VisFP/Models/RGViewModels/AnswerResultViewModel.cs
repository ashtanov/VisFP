﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.RGViewModels
{
    public class AnswerResultViewModel
    {
        public bool IsCorrect { get; set; }
        public int CurrentAttempt { get; set; }
        public int AttemptsLeft { get; set; }
    }
}
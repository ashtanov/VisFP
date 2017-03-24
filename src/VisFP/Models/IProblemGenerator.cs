﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models
{
    public interface IProblemGenerator<T>
    {
        IProblem GenerateProblem(T seed);
    }
}

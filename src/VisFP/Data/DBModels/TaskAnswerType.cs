using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public enum TaskAnswerType
    {
        [Display(Name = "Символьный")]
        SymbolsAnswer,
        [Display(Name = "Да/нет")]
        YesNoAnswer,
        [Display(Name = "Текст")]
        Text,
        [Display(Name = "Текст (возможно несколько верных ответов)")]
        TextMulty,
        [Display(Name = "Чекбоксы")]
        CheckBoxAnswer,
        [Display(Name = "Радиокнопки")]
        RadioAnswer
    }
}

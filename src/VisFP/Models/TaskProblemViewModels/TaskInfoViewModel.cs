using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;
using VisFP.Data.DBModels;

namespace VisFP.Models.TaskProblemViewModels
{
    public class TaskBaseInfo
    {
        public Guid ProblemId { get; set; }
        [Display(Name = "Попытки")]
        public int LeftAttempts { get; set; }
        public bool IsControlProblem { get; set; }
        public bool GotRightAnswer { get; set; }
    }
    public class TaskInfoViewModel
    {
        public TaskInfoViewModel(TaskBaseInfo taskBase, ComponentRepository components)
        {
            if (taskBase == null)
                throw new ArgumentNullException("TaskBaseInfo равен null!");
            BaseInfo = taskBase;
            MainInfo = components.GetComponent<MainInfoComponent>();
            if (MainInfo == null)
                throw new ArgumentNullException("Должен присутствовать компонент MainInfoComponent!");
            TopInfo = components.GetComponent<TaskInfoTopComponent>();
            ListInfo = components.GetComponent<TaskInfoListComponent>();
            Graph = components.GetComponent<GraphComponent>();
        }
        
        public TaskBaseInfo BaseInfo { get; set; }

        #region Components

        public MainInfoComponent MainInfo { get; set; }
        public TaskInfoTopComponent TopInfo { get; set; }
        public TaskInfoListComponent ListInfo { get; set; }
        public GraphComponent Graph { get; set; }

        #endregion

        public AnswerViewModel GetAnswerModel()
        {
            return new AnswerViewModel
            {
                TaskProblemId = BaseInfo.ProblemId,
                LeftAttemptsCount = BaseInfo.LeftAttempts,
                SymbolsCheckBox = MainInfo.SymbolsForAnswer,
                AnswerType = MainInfo.AnswerType,
                IsControl = BaseInfo.IsControlProblem,
                GotRightAnswer = BaseInfo.GotRightAnswer
            };
        }
    }

    public class ComponentRepository
    {
        List<IComponent> _components;
        
        public ComponentRepository(MainInfoComponent main)
        {
            _components.Add(main);
        }
        public void AddComponent(IComponent component)
        {
            _components.Add(component);
        }
        public T GetComponent<T>() 
            where T : IComponent
        {
            var component = (T)_components.FirstOrDefault(x => x.GetType() == typeof(T));
            return component;
        }
    }

    public interface IComponent
    {

    }

    public class MainInfoComponent : IComponent
    {
        [Display(Name = "Название")]
        public string TaskTitle { get; set; }
        [Display(Name = "Задание")]
        public string TaskQuestion { get; set; }
        public TaskAnswerType AnswerType { get; set; }
        public IEnumerable<char> SymbolsForAnswer { get; set; }
        public int Generation { get; set; }
    }

    public class TaskInfoTopComponent : IComponent
    {
        public string Header { get; set; }
        public Dictionary<string, string> Fields { get; set; }
    }
    public class TaskInfoListComponent : IComponent
    {
        public string Header { get; set; }
        public bool IsOrdered { get; set; }
        public IEnumerable<string> Items { get; set; }
    }
    public class GraphComponent : IComponent
    {
        public IGraph Graph { get; set; }
    }

}

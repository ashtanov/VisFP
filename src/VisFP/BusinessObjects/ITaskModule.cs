using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;

namespace VisFP.BusinessObjects
{
    public class ProblemResult
    {
        public ComponentRepository ProblemComponents { get; set; }
        public Guid ExternalProblemId { get; set; }
        public string Answer { get; set; }
    }

    public interface ITaskSetting
    {
        string Name { get; }
        string NameForView { get; }
        Type ValueType { get; }
    }

    public class TaskSettingsSet
    {
        public Guid TaskId { get; set; }
        public List<ITaskSetting> TaskSettings { get; set; }
    }
    public class TaskSetting<T> : ITaskSetting
    {
        public string Name { get; set; }
        public string NameForView { get; set; }
        public T Value { get; set; }

        public Type ValueType
        {
            get
            {
                return typeof(T);
            }
        }
    }

    public class NewTaskResult
    {
        public Guid ExternalTaskId { get; set; }
        public string TaskTitle { get; set; }
        public int TaskNumber { get; set; }
        public TaskAnswerType AnswerType { get; set; }
    }

    public interface ITaskModule
    {
        string GetModuleName();
        string GetModuleNameToView();
        Task<ProblemResult> CreateNewProblemAsync(DbTask taskTemplate);
        Task<ComponentRepository> GetExistingProblemAsync(DbTaskProblem problem);
        Task<List<TaskSettingsSet>> GetAllTasksSettingsAsync(List<Guid> externalTaskIds);
        Task<TaskSettingsSet> GetTaskSettingsAsync(Guid externalTaskId);
        Task SaveTaskSettingsAsync(Guid externalTaskId, List<ITaskSetting> updatedSettings);
        Task<List<NewTaskResult>> CreateNewTaskSetAsync();
        Task<List<NewTaskResult>> GetSeedTasks();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;
using VisFP.Utils;

namespace VisFP.BusinessObjects
{
    public class PetryNetTaskModule : ITaskModule
    {
        IServiceScopeFactory _scopeFactory;
        /// <summary>
        /// Создание модуля
        /// </summary>
        public PetryNetTaskModule(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public string GetModuleName()
        {
            return Constants.PetryNetType;
        }

        public string GetModuleNameToView()
        {
            return "Сети Петри";
        }

        public Task<ProblemResult> CreateNewProblemAsync(DbTask taskTemplate)
        {
            throw new NotImplementedException();
        }

        public Task<List<NewTaskResult>> CreateNewTaskSetAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<TaskSettingsSet>> GetAllTasksSettingsAsync(List<Guid> externalTaskIds)
        {
            throw new NotImplementedException();
        }

        public Task<ComponentRepository> GetExistingProblemAsync(DbTaskProblem problem)
        {
            throw new NotImplementedException();
        }

        public async Task<List<NewTaskResult>> GetSeedTasks()
        {
            List<NewTaskResult> result = new List<NewTaskResult>();
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                if(await dbContext.PnTasks.AnyAsync())
                {
                    //Добавляем таски
                    dbContext.PnTasks.Add(new PnTask
                    {
                        Answers = new List<string> { "A", "B", "C", "D" }.SerializeJsonListOfStrings(),
                        AnswerType = TaskAnswerType.CheckBoxAnswer,
                        PetryNetJson = "",
                        Question = "Выберите A",
                        RightAnswerNum = 1,
                        TaskNumber = 1,
                        TaskTitle = "Первак"
                    });
                }
                result.Add(new NewTaskResult
                {
                    AnswerType = TaskAnswerType.RadioAnswer,
                    ExternalTaskId = new Guid(),
                    TaskNumber = 1,
                    TaskTitle = "Тест"
                });
            }
            return result;
        }

        public Task<TaskSettingsSet> GetTaskSettingsAsync(Guid externalTaskId)
        {
            throw new NotImplementedException();
        }

        public bool IsAvailableAddTask()
        {
            throw new NotImplementedException();
        }

        public bool IsAvailableControlProblems()
        {
            throw new NotImplementedException();
        }

        public bool IsAvailableTestProblems()
        {
            throw new NotImplementedException();
        }

        public Task SaveTaskSettingsAsync(Guid externalTaskId, ICollection<SettingValue> updatedSettings)
        {
            throw new NotImplementedException();
        }
    }
}

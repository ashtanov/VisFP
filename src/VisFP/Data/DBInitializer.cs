using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;
using VisFP.Data.DBModels;

namespace VisFP.Data
{
    public static class DbInitializer
    {
        public static async void Initialize(IServiceProvider services)
        {
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbcontext.Database.EnsureCreated();
                
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                //извлечение и/или добавление базовой группы
                var baseGroup = dbcontext.UserGroups.FirstOrDefault(x => x.Name == Constants.BaseGroupName);
                if (baseGroup == null)
                {
                    //Добавляем 0 группу
                    baseGroup = new UserGroup
                    {
                        Description = "Базовая группа",
                        IsOpen = false,
                        Name = Constants.BaseGroupName
                    };
                    await dbcontext.UserGroups.AddAsync(baseGroup);
                    await dbcontext.SaveChangesAsync();
                    DbWorker.BaseGroupId = baseGroup.GroupId;
                }
                else
                    DbWorker.BaseGroupId = baseGroup.GroupId;

                //init modules
                var rgModuleId = ModulesRepository.RegisterModule(
                    new RgTaskModule(services.GetRequiredService<IServiceScopeFactory>()),
                    dbcontext);
                if (dbcontext.Tasks.Count(x => x.TaskTypeId == rgModuleId) == 0)
                    await InitializeRgTasks(dbcontext);

                //Добавление админа
                if (dbcontext.Users.Count() == 0)
                    await AddRolesAndUsers(manager, roleManager, dbcontext);

                
                //if (dbcontext.RgTasks.Count(x => x.TaskType == DbWorker.TaskTypes[Constants.FsmType]) == 0)
                //    AddFsmTasks(dbcontext);

                await dbcontext.SaveChangesAsync();
            }
        }

        private static async Task AddRolesAndUsers(UserManager<ApplicationUser> manager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            foreach (var role in Enum.GetNames(typeof(DbRole)))
            {
                await roleManager.CreateAsync(new IdentityRole { Name = role });
            }
            var adminUser = new ApplicationUser { UserName = "Admin", RealName = "Администратор", UserGroupId = DbWorker.BaseGroupId };
            await manager.CreateAsync(adminUser, "q1w2e3r4");
            await manager.AddToRoleAsync(adminUser, Enum.GetName(typeof(DbRole), DbRole.Admin));

            var ttlink = await context.TeacherTasks.SingleOrDefaultAsync(x => x.TeacherId == adminUser.Id);
            if (ttlink == null)
            {
                ttlink = new DbTeacherTask
                {
                    TeacherId = adminUser.Id
                };
                context.TeacherTasks.Add(ttlink);
            }
            await context.SetTasksToNewTeacherAsync(ModulesRepository.GetModule<RgTaskModule>(), ttlink.Id, true);
            await context.SetTasksToNewTeacherAsync(ModulesRepository.GetModule<RgTaskModule>(), ttlink.Id, false);
        }

        private static async Task InitializeRgTasks(ApplicationDbContext dbcontext)
        {
            var rgModule = ModulesRepository.GetModule<RgTaskModule>();
            var currentTaskTypeId = ModulesRepository.GetModuleId<RgTaskModule>();
            var seedTasks = await rgModule.GetSeedTasks();
            List<DbTask> tasks = new List<DbTask>();
            foreach (var seedTask in seedTasks)
            {
                tasks.Add(new DbTask
                {
                    TaskTitle = seedTask.TaskTitle,
                    TaskTypeId = currentTaskTypeId,
                    SuccessScore = 5,
                    FailTryScore = 0,
                    MaxAttempts = seedTask.AnswerType == TaskAnswerType.YesNoAnswer ? 1 : 3,
                    TaskNumber = seedTask.TaskNumber,
                    IsControl = false,
                    ExternalTaskId = seedTask.ExternalTaskId
                });
            }
            await dbcontext.Tasks.AddRangeAsync(tasks);
        }

        //private static void AddFsmTasks(ApplicationDbContext dbcontext)
        //{
        //    var currentTaskType = DbWorker.TaskTypes[Constants.FsmType];
        //    for (int i = 0; i < 2; ++i)
        //    {
        //        bool isControl = i % 2 == 0;
        //        dbcontext.RgTasks.Add(new RgTask
        //        {
        //            TaskTitle = "Непустые языки",
        //            TaskType = currentTaskType,
        //            NonTerminalRuleCount = 7,
        //            IsGrammarGenerated = true,
        //            TerminalRuleCount = 3,
        //            AlphabetNonTerminalsCount = 5,
        //            AlphabetTerminalsCount = 3,
        //            SuccessScore = 5,
        //            FailTryScore = 0,
        //            MaxAttempts = 1,
        //            TaskNumber = 1,
        //            IsControl = isControl
        //        });

        //        dbcontext.RgTasks.Add(new RgTask
        //        {
        //            TaskTitle = "Построение цепочки",
        //            TaskType = currentTaskType,
        //            NonTerminalRuleCount = 5,
        //            IsGrammarGenerated = true,
        //            TerminalRuleCount = 2,
        //            AlphabetNonTerminalsCount = 4,
        //            AlphabetTerminalsCount = 2,
        //            ChainMinLength = 6,
        //            SuccessScore = 5,
        //            FailTryScore = 0,
        //            MaxAttempts = 3,
        //            TaskNumber = 2,
        //            IsControl = isControl
        //        });

        //        dbcontext.RgTasks.Add(new RgTask
        //        {
        //            TaskTitle = "Бесконечные языки",
        //            TaskType = currentTaskType,
        //            NonTerminalRuleCount = 5,
        //            IsGrammarGenerated = true,
        //            TerminalRuleCount = 3,
        //            AlphabetNonTerminalsCount = 5,
        //            AlphabetTerminalsCount = 3,
        //            SuccessScore = 5,
        //            FailTryScore = 0,
        //            MaxAttempts = 1,
        //            TaskNumber = 3,
        //            IsControl = isControl
        //        });

        //        dbcontext.RgTasks.Add(new RgTask
        //        {
        //            TaskTitle = "Детерминированность автомата",
        //            TaskType = currentTaskType,
        //            NonTerminalRuleCount = 7,
        //            IsGrammarGenerated = true,
        //            TerminalRuleCount = 3,
        //            AlphabetNonTerminalsCount = 5,
        //            AlphabetTerminalsCount = 3,
        //            SuccessScore = 5,
        //            FailTryScore = 0,
        //            MaxAttempts = 1,
        //            TaskNumber = 4,
        //            IsControl = isControl
        //        });

        //        dbcontext.RgTasks.Add(new RgTask
        //        {
        //            TaskTitle = "Допустимость цепочки",
        //            TaskType = currentTaskType,
        //            NonTerminalRuleCount = 6,
        //            IsGrammarGenerated = true,
        //            TerminalRuleCount = 3,
        //            AlphabetNonTerminalsCount = 4,
        //            AlphabetTerminalsCount = 2,
        //            SuccessScore = 5,
        //            ChainMinLength = 6,
        //            FailTryScore = 0,
        //            MaxAttempts = 1,
        //            TaskNumber = 5,
        //            IsControl = isControl
        //        });
        //    }

        //}
    }
}

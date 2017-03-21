using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Models.DBModels;

namespace VisFP.Data
{
    public static class DbInitializer
    {
        public static UserManager<ApplicationUser> UserManager { get; private set; }

        public static async void Initialize(IServiceProvider services)
        {
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await roleManager.CreateAsync(new IdentityRole { Name = "Admin" });
                await roleManager.CreateAsync(new IdentityRole { Name = "User" });

                var adminUser = new ApplicationUser { UserName = "Alex" };
                await manager.CreateAsync(adminUser, "q1w2e3r4");
                await manager.AddToRoleAsync(adminUser, "Admin");

                var simpleUser = new ApplicationUser { UserName = "Test" };
                await manager.CreateAsync(simpleUser, "1234");
                await manager.AddToRoleAsync(simpleUser, "User");

                var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbcontext.Tasks.Add(new RgTask
                {
                    TaskText = "Отметьте ВСЕ недостижимые символы (нетерминалы)",
                    TaskTitle = "Задача 1. Недостижимые символы",
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 3,
                    AlphabetNonTerminalsCount = 5,
                    AlphabetTerminalsCount = 3,
                    MaxAttempts = 3,
                    TaskNumber = 1,
                    AnswerType = Models.TaskAnswerType.SymbolsAnswer
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskText = "Отметьте ВСЕ пустые символы (нетерминалы)",
                    TaskTitle = "Задача 2. Пустые символы",
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 3,
                    AlphabetNonTerminalsCount = 5,
                    AlphabetTerminalsCount = 3,
                    MaxAttempts = 3,
                    TaskNumber = 2,
                    AnswerType = Models.TaskAnswerType.SymbolsAnswer
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskText = "Отметьте ВСЕ циклические символы (нетерминалы)",
                    TaskTitle = "Задача 3. Циклические символы",
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 3,
                    AlphabetNonTerminalsCount = 5,
                    AlphabetTerminalsCount = 3,
                    MaxAttempts = 3,
                    TaskNumber = 3,
                    AnswerType = Models.TaskAnswerType.SymbolsAnswer
                });
                await dbcontext.SaveChangesAsync();
            }
        }
    }
}

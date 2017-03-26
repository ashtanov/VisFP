﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

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

                foreach (var role in Enum.GetNames(typeof(DbRole)))
                {
                    await roleManager.CreateAsync(new IdentityRole { Name = role });
                }
                var adminUser = new ApplicationUser { UserName = "Admin", RealName = "Администратор" };
                await manager.CreateAsync(adminUser, "q1w2e3r4");
                await manager.AddToRoleAsync(adminUser, Enum.GetName(typeof(DbRole), DbRole.Admin));

                var simpleUser = new ApplicationUser { UserName = "Test", RealName = "Тест Тестович" };
                await manager.CreateAsync(simpleUser, "1234");
                await manager.AddToRoleAsync(simpleUser, Enum.GetName(typeof(DbRole), DbRole.User));

                var teacherUser = new ApplicationUser { UserName = "Teacher", RealName = "Преподаватель" };
                await manager.CreateAsync(teacherUser, "1234");
                await manager.AddToRoleAsync(teacherUser, Enum.GetName(typeof(DbRole), DbRole.Teacher));

                var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbcontext.Tasks.Add(new RgTask
                {
                    TaskTitle = "Задача 1. Недостижимые символы",
                    NonTerminalRuleCount = 7,
                    IsGrammarGenerated = true,
                    TerminalRuleCount = 3,
                    AlphabetNonTerminalsCount = 5,
                    AlphabetTerminalsCount = 3,
                    MaxAttempts = 3,
                    TaskNumber = 1,
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskTitle = "Задача 2. Пустые символы",
                    IsGrammarGenerated = true,
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 3,
                    AlphabetNonTerminalsCount = 5,
                    AlphabetTerminalsCount = 3,
                    MaxAttempts = 3,
                    TaskNumber = 2,
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskTitle = "Задача 3. Циклические символы",
                    IsGrammarGenerated = true,
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 3,
                    AlphabetNonTerminalsCount = 5,
                    AlphabetTerminalsCount = 3,
                    MaxAttempts = 3,
                    TaskNumber = 3,
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskTitle = "Задача 4. Приведенные грамматики",
                    IsGrammarGenerated = true,
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 2,
                    AlphabetNonTerminalsCount = 4,
                    AlphabetTerminalsCount = 2,
                    MaxAttempts = 1,
                    TaskNumber = 4,
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskTitle = "Задача 5. Пустые языки",
                    IsGrammarGenerated = true,
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 2,
                    AlphabetNonTerminalsCount = 3,
                    AlphabetTerminalsCount = 2,
                    MaxAttempts = 1,
                    TaskNumber = 5,
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskTitle = "Задача 6. Построение цепочки",
                    IsGrammarGenerated = true,
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 2,
                    AlphabetNonTerminalsCount = 3,
                    AlphabetTerminalsCount = 2,
                    MaxAttempts = 3,
                    TaskNumber = 6,
                    ChainMinLength = 5
                });

                dbcontext.Tasks.Add(new RgTask
                {
                    TaskTitle = "Задача 7. Выводима ли цепочка?",
                    IsGrammarGenerated = true,
                    NonTerminalRuleCount = 7,
                    TerminalRuleCount = 2,
                    AlphabetNonTerminalsCount = 3,
                    AlphabetTerminalsCount = 2,
                    MaxAttempts = 1,
                    TaskNumber = 7,
                    ChainMinLength = 5
                });
                await dbcontext.SaveChangesAsync();
            }
        }
    }
}

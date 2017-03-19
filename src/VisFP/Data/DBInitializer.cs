using Microsoft.AspNetCore.Identity;
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

                var user = new ApplicationUser { UserName = "Alex" };
                var result = await manager.CreateAsync(user, "1234");
            }
        }
    }
}

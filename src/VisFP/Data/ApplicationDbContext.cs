using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VisFP.Data.DBModels;

namespace VisFP.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<RgTask> Tasks { get; set; }
        public DbSet<RgTaskProblem> TaskProblems { get; set; }
        public DbSet<RgAttempt> Attempts { get; set; }
        public DbSet<RGrammar> RGrammars { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<RgAttempt>().HasOne(x => x.Problem).WithMany(y => y.Attempts).HasForeignKey(p => p.ProblemId);
            builder.Entity<RgTaskProblem>().HasOne(x => x.User).WithMany(y => y.Problems).HasForeignKey(p => p.UserId);
            builder.Entity<RgTaskProblem>().HasOne(x => x.Task).WithMany(y => y.Problems).HasForeignKey(p => p.TaskNumber);
            builder.Entity<RgTaskProblem>().HasOne(x => x.CurrentGrammar).WithMany(y => y.Problems).HasForeignKey(p => p.GrammarId);
            builder.Entity<RgTask>().HasOne(x => x.FixedGrammar).WithMany(y => y.Tasks).HasForeignKey(p => p.FixedGrammarId);
        }
    }
}

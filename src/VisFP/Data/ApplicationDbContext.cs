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
        public DbSet<RgTask> RgTasks { get; set; }
        public DbSet<RgTaskProblem> RgTaskProblems { get; set; }
        public DbSet<RgAttempt> Attempts { get; set; }
        public DbSet<RGrammar> RGrammars { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<RgControlVariant> Variants { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(
                entity =>
                {
                    entity.HasOne(x => x.UserGroup).WithMany(y => y.Members).HasForeignKey(p => p.UserGroupId);
                }
            );

            builder.Entity<RgAttempt>(
                entity =>
                {
                    entity.HasKey(x => x.AttemptId);
                    entity.HasOne(x => x.Problem).WithMany(y => y.Attempts).HasForeignKey(p => p.ProblemId);
                }
            );

            builder.Entity<RgTaskProblem>(
                entity =>
                {
                    entity.HasKey(x => x.ProblemId);
                    entity.HasOne(x => x.User).WithMany(y => y.RgProblems).HasForeignKey(p => p.UserId);
                    entity.HasOne(x => x.Task).WithMany(y => y.Problems).HasForeignKey(p => p.TaskId);
                    entity.HasOne(x => x.CurrentGrammar).WithMany(y => y.Problems).HasForeignKey(p => p.GrammarId);
                    entity.HasOne(x => x.Variant).WithMany(y => y.Problems).HasForeignKey(p => p.VariantId);
                }
            );

            builder.Entity<RgTask>(
                entity =>
                {
                    entity.HasKey(x => x.TaskId);
                    entity.HasOne(x => x.FixedGrammar).WithMany(y => y.Tasks).HasForeignKey(p => p.FixedGrammarId);
                    entity.HasOne(x => x.UserGroup).WithMany(y => y.RgTasks).HasForeignKey(p => p.GroupId);
                    entity.HasIndex(x => new { x.TaskNumber, x.GroupId }).IsUnique();
                }
            );

            builder.Entity<UserGroup>(
                entity =>
                {
                    entity.HasIndex(x => x.Name).IsUnique();
                    entity.HasKey(x => x.GroupId);
                    entity.HasOne(x => x.Creator).WithMany(y => y.OwnedGroups).HasForeignKey(p => p.CreatorId);
                });

            builder.Entity<RgControlVariant>(
                entity =>
                {
                    entity.HasKey(x => x.VariantId);
                    entity.HasOne(x => x.User).WithMany(y => y.RgControlVariants).HasForeignKey(p => p.UserId);
                });
        }
    }
}

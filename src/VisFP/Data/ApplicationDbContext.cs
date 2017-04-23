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
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<DbTask> Tasks { get; set; }
        public DbSet<DbTaskProblem> TaskProblems { get; set; }
        public DbSet<DbControlVariant> Variants { get; set; }
        public DbSet<DbAttempt> Attempts { get; set; }
        public DbSet<DbTeacherTaskType> TeacherTasks { get; set; }
        public DbSet<DbTaskType> TaskTypes { get; set; }

        //База для модуля РГ
        public DbSet<RgTask> RgTasks { get; set; }
        public DbSet<RgTaskProblem> RgTaskProblems { get; set; }
        public DbSet<RGrammar> RGrammars { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>(entity =>
                {
                    entity.HasOne(x => x.UserGroup).WithMany(y => y.Members).HasForeignKey(p => p.UserGroupId);
                });

            builder.Entity<UserGroup>(entity =>
                {
                    entity.HasIndex(x => x.Name).IsUnique();
                    entity.HasKey(x => x.GroupId);
                    entity.HasOne(x => x.Creator).WithMany(y => y.OwnedGroups).HasForeignKey(p => p.CreatorId);
                });

            builder.Entity<DbTask>(entity =>
                {
                    entity.HasKey(x => x.TaskId);
                    entity.HasOne(x => x.TeacherTask).WithMany(x => x.Tasks).HasForeignKey(x => x.TeacherTaskId);
                    entity.HasOne(x => x.TaskType).WithMany(x => x.Tasks).HasForeignKey(x => x.TaskTypeId);
                    entity.HasIndex(x => x.ExternalTaskId).IsUnique();
                });
            builder.Entity<DbTaskProblem>(entity =>
                {
                    entity.HasKey(x => x.ProblemId);
                    entity.HasOne(x => x.User).WithMany(y => y.Problems).HasForeignKey(p => p.UserId);
                    entity.HasOne(x => x.Variant).WithMany(y => y.Problems).HasForeignKey(p => p.VariantId).OnDelete(Microsoft.EntityFrameworkCore.Metadata.DeleteBehavior.Cascade);
                    entity.HasOne(x => x.Task).WithMany(y => y.Problems).HasForeignKey(p => p.TaskId);
                    entity.HasIndex(x => x.ExternalProblemId).IsUnique();
                }
            );
            builder.Entity<DbAttempt>(entity =>
                {
                    entity.HasKey(x => x.AttemptId);
                    entity.HasOne(x => x.Problem).WithMany(y => y.Attempts).HasForeignKey(p => p.ProblemId).OnDelete(Microsoft.EntityFrameworkCore.Metadata.DeleteBehavior.Cascade);
                }
            );
            builder.Entity<DbControlVariant>(entity =>
            {
                entity.HasKey(x => x.VariantId);
                entity.HasOne(x => x.TaskType).WithMany(x => x.Variants).HasForeignKey(x => x.TaskTypeId);
                entity.HasOne(x => x.User).WithMany(y => y.ControlVariants).HasForeignKey(p => p.UserId).OnDelete(Microsoft.EntityFrameworkCore.Metadata.DeleteBehavior.Cascade);
            });
            builder.Entity<DbTeacherTaskType>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasOne(x => x.Teacher).WithMany(x => x.TeacherTasks).HasForeignKey(x => x.TeacherId);
                entity.HasOne(x => x.Type).WithMany(x => x.TeacherTaskTypes).HasForeignKey(x => x.TypeId);
            });

            builder.Entity<DbTaskType>(entity =>
            {
                entity.HasKey(x => x.TaskTypeId);
                entity.HasIndex(x => x.TaskTypeName).IsUnique();
            });


            builder.Entity<RgTask>(entity =>
                {
                    entity.HasKey(x => x.Id);
                    entity.HasOne(x => x.FixedGrammar).WithMany(y => y.Tasks).HasForeignKey(p => p.FixedGrammarId);
                });        

            builder.Entity<RgTaskProblem>(entity =>
                {
                    entity.HasKey(x => x.Id);
                    entity.HasOne(x => x.CurrentGrammar).WithMany(y => y.Problems).HasForeignKey(p => p.GrammarId);
                });
            builder.Entity<RGrammar>(entity =>
            {
                entity.HasKey(x => x.GrammarId);
            });
        }
    }
}

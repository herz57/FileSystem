﻿using FM.FileService.Data.EntityConfigurations;
using FM.FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
 
namespace FM.FileService.Data
{
    public class FileDbContext : DbContext
    {
        public DbSet<File> Files { get; set; }
        public DbSet<FileReadHistory> FileReadHistories { get; set; }
        public FileDbContext(DbContextOptions<FileDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new FileEntityConfiguration());
            builder.ApplyConfiguration(new FileReadHistoryEntityConfiguration());
        }
    }
}

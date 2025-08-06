using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WindowsFormsApp1
{
    internal class AppDbContext : DbContext
    {
        private readonly string _dbPath;

        public AppDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        public DbSet<AlignInfo> AlignInfos { get; set; }
        public DbSet<ProductInfo> ProductInfos { get; set; }
        public DbSet<ResultInfo> ResultInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AlignInfo>().ToTable("AlignInfos");
            modelBuilder.Entity<ProductInfo>().ToTable("ProductInfos");
            modelBuilder.Entity<ResultInfo>().ToTable("ResultInfos");
        }
    }
}

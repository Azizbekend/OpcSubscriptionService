using Microsoft.EntityFrameworkCore;
using OpcSubscriptionService.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcSubscriptionService
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt)
        { }
        public AppDbContext()
        {
            Database.EnsureCreated();
        }
        public DbSet<NodeInfo> Nodes { get; set; }
        public DbSet<NodeIndicates> NodesIndicates { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Hardware> Hardwares { get; set; }
        public DbSet<ControlBlock> ControlBlocks { get; set; }
        public DbSet<StaticObjectInfo> StaticObjectInfos { get; set; }



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=192.168.95.238;Port=5432;Database=DispetcherDb;User Id=trieco_admin;Password=trieco;pooling=true;");
        }
    }
}

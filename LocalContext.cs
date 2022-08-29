using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Feralas
{
    public class LocalContext : DbContext
    {
        public DbSet<WowItem> WowItems { get; set; }
        public DbSet<WowAuction> WowAuctions { get; set; }

        public string DbPath { get; }

        public LocalContext()
        {
            string DbFolder =
                Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                Path.DirectorySeparatorChar +
                "Data";

            try
            {
                if (!Directory.Exists(DbFolder))
                {
                    Directory.CreateDirectory(DbFolder);
                }
            }
            catch
            {
                // we can't make a database so we are stuffed..
                Environment.Exit(1);
            }

            // 
            string DbPath =
                DbFolder +
                Path.DirectorySeparatorChar +
                "inspired.db";

            //Console.WriteLine(DbPath);
        }




        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isLinux)
            {
                optionsBuilder.UseSqlite($"Data Source=/home/patrick/Data/blizzard_data.db");
            }
            else
                optionsBuilder.UseSqlite($"Data Source=blizzard_data.db");
        }

    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; }

        public List<Post> Posts { get; } = new();
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}
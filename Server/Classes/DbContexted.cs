using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Server.Classes
{
    public class DbContexted : DbContext
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<BlackList> blackLists { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql(
                "server=127.0.0.1;port=3306;database=;user=root;password=",
                new MySqlServerVersion(new Version(8, 0)));
        }

    }
}
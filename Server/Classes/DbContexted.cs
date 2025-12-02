using Microsoft.EntityFrameworkCore;

namespace Server.Classes
{
    public class DbContexted : DbContext
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<BlackList> BlackLists { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql(
                "server=localhost;port=3306;database=LicenseServerDB;user=root;password=;",
                new MySqlServerVersion(new Version(8, 0)));
        }
    }
}
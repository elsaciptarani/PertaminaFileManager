using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PertaminaFileManager.Models.Base;

namespace PertaminaFileManager.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<fileUploadEmployee> FileUploadInfos { get; set; }
        public DbSet<FileUploadEmployee> FileUploadEmployees { get; set; }
    }
}
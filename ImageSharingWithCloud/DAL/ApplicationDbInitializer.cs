
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using ImageSharingWithCloud.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ImageSharingWithCloud.DAL
{
    public  class ApplicationDbInitializer
    {
        private ApplicationDbContext db;
        private IImageStorage imageStorage;
        private ILogContext logContext;
        private ILogger<ApplicationDbInitializer> logger;
        public ApplicationDbInitializer(ApplicationDbContext db, 
                                        IImageStorage imageStorage,
                                        ILogContext logContext,
                                        ILogger<ApplicationDbInitializer> logger)
        {
            this.db = db;
            this.imageStorage = imageStorage;   
            this.logContext = logContext;
            this.logger = logger;
        }

        public async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            /*
             * Initialize databases.
             */
            await db.Database.MigrateAsync();

            await imageStorage.InitImageStorage();

            /*
             * Clear any existing data from the databases.
             */
            IList<Image> images = await imageStorage.GetAllImagesInfoAsync();
            foreach (Image image in images)
            {
                await imageStorage.RemoveImageAsync(image);
            }

            db.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            logger.LogDebug("Adding role: User");
            var idResult = await CreateRole(serviceProvider, "User");
            if (!idResult.Succeeded)
            {
                logger.LogDebug("Failed to create User role!");
            }

            // TODO add other roles


            logger.LogDebug("Adding user: jfk");
            idResult = await CreateAccount(serviceProvider, "jfk@example.org", "jfk123", "Admin");
            if (!idResult.Succeeded)
            {
                logger.LogDebug("Failed to create jfk user!");
            }

            logger.LogDebug("Adding user: nixon");
            idResult = await CreateAccount(serviceProvider, "nixon@example.org", "nixon123", "User");
            if (!idResult.Succeeded)
            {
                logger.LogDebug("Failed to create nixon user!");
            }

            // TODO add other users and assign more roles


            await db.SaveChangesAsync();

        }

        public static async Task<IdentityResult> CreateRole(IServiceProvider provider,
                                                            string role)
        {
            RoleManager<IdentityRole> roleManager = provider
                .GetRequiredService
                       <RoleManager<IdentityRole>>();
            var idResult = IdentityResult.Success;
            if (await roleManager.FindByNameAsync(role) == null)
            {
                idResult = await roleManager.CreateAsync(new IdentityRole(role));
            }
            return idResult;
        }

        public static async Task<IdentityResult> CreateAccount(IServiceProvider provider,
                                                               string email, 
                                                               string password,
                                                               string role)
        {
            UserManager<ApplicationUser> userManager = provider
                .GetRequiredService
                       <UserManager<ApplicationUser>>();
            var idResult = IdentityResult.Success;

            if (await userManager.FindByNameAsync(email) == null)
            {
                ApplicationUser user = new ApplicationUser { UserName = email, Email = email };
                idResult = await userManager.CreateAsync(user, password);

                if (idResult.Succeeded)
                {
                    idResult = await userManager.AddToRoleAsync(user, role);
                }
            }

            return idResult;
        }

    }
}
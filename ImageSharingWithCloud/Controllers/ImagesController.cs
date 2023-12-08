using Microsoft.AspNetCore.Mvc;
using ImageSharingWithCloud.DAL;
using ImageSharingWithCloud.Models;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Azure;

namespace ImageSharingWithCloud.Controllers
{
    // TODO require authorization by default -- DONE
    [Authorize]
    public class ImagesController : BaseController
    {
        protected ILogContext logContext;

        protected readonly ILogger<ImagesController> logger;

        // Dependency injection
        public ImagesController(UserManager<ApplicationUser> userManager,
                                ApplicationDbContext userContext,
                                ILogContext logContext,
                                IImageStorage imageStorage,
                                ILogger<ImagesController> logger)
            : base(userManager, imageStorage, userContext)
        {
            this.logContext = logContext;

            this.logger = logger;
        }


        // TODO -- DONE
        [HttpGet]
        [Authorize(Roles = "User")]
        public ActionResult Upload()
        {
            CheckAda();

            ViewBag.Message = "";
            ImageView imageView = new ImageView();
            return View(imageView);
        }

        // TODO prevent CSRF -- DONE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<ActionResult> Upload(ImageView imageView)
        {
            CheckAda();

            logger.LogDebug("Processing the upload of an image....");

            await TryUpdateModelAsync(imageView);

            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Please correct the errors in the form!";
                return View();
            }

            logger.LogDebug("...getting the current logged-in user....");
            ApplicationUser user = await GetLoggedInUser();

            if (imageView.ImageFile == null || imageView.ImageFile.Length <= 0)
            {
                ViewBag.Message = "No image file specified!";
                return View(imageView);
            }

            logger.LogDebug("....saving image metadata in the database....");

            string imageId = null;

            // TODO save image metadata in the database -- DONE
            Image image = new Image
            {
                Caption = imageView.Caption,
                Description = imageView.Description,
                DateTaken = imageView.DateTaken,
                UserId = user.Id,
                Id = imageView.Id,
                Valid = true,
                Approved = true
            };

            imageId = await imageStorage.SaveImageInfoAsync(image);
            // end TODO

            logger.LogDebug("...saving image file on disk....");

            // TODO save image file on disk -- DONE
            if (imageView.ImageFile != null && imageView.ImageFile.Length > 0)
            {
                await imageStorage.SaveImageFileAsync(imageView.ImageFile, user.Id, imageId);
            }

            logger.LogDebug("....forwarding to the details page, image Id = "+imageId);

            return RedirectToAction("Details", new { UserId = user.Id, Id = imageId });
        }

        // TODO -- DONE
        [HttpGet]
        public ActionResult Query()
        {
            CheckAda();

            ViewBag.Message = "";
            return View();
        }

        // TODO -- DONE
        [HttpGet]
        public async Task<ActionResult> Details(string UserId, string Id)
        {
            CheckAda();

            Image image = await imageStorage.GetImageInfoAsync(UserId, Id);
            if (image == null)
            {
                return RedirectToAction("Error", "Home", new { ErrId = "Details: " + Id });
            }

            ImageView imageView = new ImageView();
            imageView.Id = image.Id;
            imageView.Caption = image.Caption;
            imageView.Description = image.Description;
            imageView.DateTaken = image.DateTaken;
            imageView.Uri = imageStorage.ImageUri(image.UserId, image.Id);

            imageView.UserName = image.UserName;
            imageView.UserId = image.UserId;

            // TODO Log this view of the image -- DONE
            await logContext.AddLogEntryAsync(UserId, imageView.UserName, imageView);

            return View(imageView);
        }

        // TODO -- DONE
        [HttpGet]
        public async Task<ActionResult> Edit(string UserId, string Id)
        {
            CheckAda();
            ApplicationUser user = await GetLoggedInUser();
            if (user == null || !user.Id.Equals(UserId))
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotAuth" });
            }

            Image image = await imageStorage.GetImageInfoAsync(UserId, Id);
            if (image == null)
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotFound" });
            }

            ViewBag.Message = "";

            ImageView imageView = new ImageView();
            imageView.Id = image.Id;
            imageView.Caption = image.Caption;
            imageView.Description = image.Description;
            imageView.DateTaken = image.DateTaken;

            imageView.UserId = image.UserId;
            imageView.UserName = image.UserName;

            return View("Edit", imageView);
        }

        // TODO prevent CSRF -- DONE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DoEdit(string UserId, string Id, ImageView imageView)
        {
            CheckAda();

            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Please correct the errors on the page";
                imageView.Id = Id;
                return View("Edit", imageView);
            }

            ApplicationUser user = await GetLoggedInUser();
            if (user == null || !user.Id.Equals(UserId))
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotAuth" });
            }

            logger.LogDebug("Saving changes to image " + Id);
            Image image = await imageStorage.GetImageInfoAsync(imageView.UserId, Id);
            if (image == null)
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotFound" });
            }

            image.Caption = imageView.Caption;
            image.Description = imageView.Description;
            image.DateTaken = imageView.DateTaken;
            await imageStorage.UpdateImageInfoAsync(image);

            return RedirectToAction("Details", new { UserId = UserId, Id = Id });
        }

        // TODO -- DONE
        [HttpGet]
        public async Task<ActionResult> Delete(string UserId, string Id)
        {
            CheckAda();
            ApplicationUser user = await GetLoggedInUser();
            if (user == null || !user.Id.Equals(UserId))
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotAuth" });
            }

            Image image = await imageStorage.GetImageInfoAsync(user.Id, Id);
            if (image == null)
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotFound" });
            }

            ImageView imageView = new ImageView();
            imageView.Id = image.Id;
            imageView.Caption = image.Caption;
            imageView.Description = image.Description;
            imageView.DateTaken = image.DateTaken;

            imageView.UserName = image.UserName;
            return View(imageView);
        }

        // TODO prevent CSRF -- DONE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DoDelete(string UserId, string Id)
        {
            CheckAda();
            ApplicationUser user = await GetLoggedInUser();
            if (user == null || !user.Id.Equals(UserId))
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotAuth" });
            }

            Image image = await imageStorage.GetImageInfoAsync(user.Id, Id);
            if (image == null)
            {
                return RedirectToAction("Error", "Home", new { ErrId = "EditNotFound" });
            }

            await imageStorage.RemoveImageAsync(image);

            return RedirectToAction("Index", "Home");

        }

        // TODO -- DONE
        [HttpGet]
        public async Task<ActionResult> ListAll()
        {
            CheckAda();
            ApplicationUser user = await GetLoggedInUser();

            IList<Image> images = await imageStorage.GetAllImagesInfoAsync();
            ViewBag.UserId = user.Id; // TODO -- Changed UserId to Id here -- DONE
            return View(images);
        }

        // TODO -- DONE
        [HttpGet]
        public async Task<IActionResult> ListByUser()
        {
            CheckAda();

            // Return form for selecting a user from a drop-down list
            ListByUserModel userView = new ListByUserModel();
            var defaultId = (await GetLoggedInUser()).Id;

            userView.Users = new SelectList(ActiveUsers(), "Id", "UserName", defaultId);
            return View(userView);
        }

        // TODO -- DONE
        [HttpGet]
        public async Task<ActionResult> DoListByUser(string Id)
        {
            CheckAda();

            // TODO list all images uploaded by the user in userView -- DONE
            ApplicationUser user = await userManager.FindByIdAsync(Id);
            var images = await imageStorage.GetImageInfoByUserAsync(user);

            // Assuming a view model or a view that can list images
            return View("ListImages", images);
            // End TODO

        }

        // TODO -- DONE
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public ActionResult ImageViews()
        {
            CheckAda();
            return View();
        }


        // TODO -- DONE
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public ActionResult ImageViewsList(string Today)
        {
            CheckAda();
            logger.LogDebug("Looking up log views, \"Today\"=" + Today);
            AsyncPageable<LogEntry> entries = logContext.Logs("true".Equals(Today));
            logger.LogDebug("Query completed, rendering results....");
            return View(entries);
        }

    }

}

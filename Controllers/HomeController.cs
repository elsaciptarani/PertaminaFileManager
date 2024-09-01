using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.FileProviders;
using Syncfusion.Blazor.FileManager;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using PertaminaFileManager.Models.Base;
using PertaminaFileManager.Models;
using PertaminaFileManager.Data;

namespace PertaminaFileManager.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    [Route("api/[controller]")]
    public class FileManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public PhysicalFileProvider operation;
        public string basePath;
        string root = "wwwroot\\Files";

        // Tambahkan batas ukuran file maksimum dalam byte (10 MB)
        private const long MaxFileSize = 10485760; // 10 MB


        public FileManagerController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            basePath = _hostingEnvironment.ContentRootPath;
            operation = new PhysicalFileProvider();
            operation.RootFolder(basePath + root); // It denotes in which files and folders are available.
        }
        // Processing the File Manager operations.
        [Route("FileOperations")]
        public object? FileOperations([FromBody] FileManagerDirectoryContent args, string Role)
        {
            // Ambil role dari HttpContext
            var userRole = User.IsInRole("Admin") ? "Admin" : "Employee";

            // Tentukan path berdasarkan role
            string rootFolder = userRole == "Admin" ? "wwwroot/Files/Admin" : "wwwroot/Files/Employee";

            // Pastikan operasi file dilakukan di folder yang sesuai dengan role
            var fullPath = Path.Combine(basePath, rootFolder, args.Path);

            if (Role == "Admin")
            {
                if (args.Action == "create")
                    return operation.ToCamelCase(operation.Create(args.Path, args.Name));
                else if (args.Action == "delete")
                    return operation.ToCamelCase(operation.Delete(args.Path, args.Names));
            }

            if (Role == "Admin" || Role == "Employee")
            {
                if (args.Action == "create")
                    return operation.ToCamelCase(operation.Create(args.Path, args.Name));
                else if (args.Action == "copy")
                    return operation.ToCamelCase(operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                else if (args.Action == "move")
                    return operation.ToCamelCase(operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                else if (args.Action == "rename")
                    return operation.ToCamelCase(operation.Rename(args.Path, args.Name, args.NewName));
                else if (args.Action == "delete")
                    return operation.ToCamelCase(operation.Delete(args.Path, args.Names));

            }

            if (args.Action == "read")
                return operation.ToCamelCase(operation.GetFiles(args.Path, args.ShowHiddenItems));
            else if (args.Action == "search")
                return operation.ToCamelCase(operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive));
            else if (args.Action == "details")
                return operation.ToCamelCase(operation.Details(args.Path, args.Names));

            return null;
        }

        [Route("Upload")]
        public IActionResult Upload(string path, IList<IFormFile> uploadFiles, string action, FileUploadEmployee fileUploadEmployee)
        {
            if (uploadFiles == null || !uploadFiles.Any())
            {
                return BadRequest("No files were uploaded.");
            }

            FileManagerResponse uploadResponse;
            foreach (var file in uploadFiles)
            {
                // Periksa ukuran file sebelum memproses
                if (file.Length > MaxFileSize)
                {
                    Response.Clear();
                    Response.ContentType = "application/json; charset=utf-8";
                    Response.StatusCode = 400; // Bad Request
                    Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = "File terlalu besar. Maksimum ukuran adalah 10 MB.";
                    return Content("{\"message\":\"File terlalu besar. Maksimum ukuran adalah 10 MB.\"}", "application/json");
                }

                var folders = file.FileName.Split('/');
                if (folders.Length > 1)
                {
                    for (var i = 0; i < folders.Length - 1; i++)
                    {
                        string newDirectoryPath = Path.Combine(basePath + path, folders[i]);
                        if (!Directory.Exists(newDirectoryPath))
                        {
                            operation.ToCamelCase(operation.Create(path, folders[i]));
                        }
                        path = Path.Combine(path, folders[i]) + "/";
                    }
                }

                // Proses pengunggahan file
                var fileName = file.FileName;
                var uploadPath = Path.Combine(basePath + path, fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Simpan informasi file ke database
                var fileEmployee = new FileUploadEmployee
                {
                    FileName = fileName,
                    UploadedBy = User.Identity?.Name ?? "Unknown", // Menggunakan "Unknown" jika User.Identity null
                    UploadDate = DateTime.Now
                };

                _context.FileUploadEmployees.Add(fileEmployee); // Tambahkan ke context
                _context.SaveChanges(); // Simpan perubahan ke database
            }

            uploadResponse = operation.Upload(path, uploadFiles, action, null);
            if (uploadResponse?.Error != null)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = Convert.ToInt32(uploadResponse.Error.Code);
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = uploadResponse.Error.Message;
                return Content("");
            }
            return Ok(); // Menggunakan Ok() untuk merespon sukses
        }

        /// <summary>
        /// downloads the selected file(s) and folder(s)
        /// </summary>
        /// <param name="downloadInput"></param>
        /// <returns></returns>
        //[HttpPost]
        [Route("Download")]
        public IActionResult Download(string downloadInput)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "Employee";
            string rootFolder = userRole == "Admin" ? "wwwroot/Files/Admin" : "wwwroot/Files/Employee";
            FileManagerDirectoryContent args = JsonConvert.DeserializeObject<FileManagerDirectoryContent>(downloadInput);

            var fullPath = Path.Combine(basePath, rootFolder, args.Path);
            return operation.Download(fullPath, args.Names, args.Data);
        }

        /// <summary>
        ///  gets the image(s) from the given path
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        //[HttpGet]
        [Route("GetImage")]
        public IActionResult GetImage(FileManagerDirectoryContent args)
        {
            var userRole = User.IsInRole("Admin") ? "Admin" : "Employee";
            string rootFolder = userRole == "Admin" ? "wwwroot/Files/Admin" : "wwwroot/Files/Employee";

            var fullPath = Path.Combine(basePath, rootFolder, args.Path);
            return operation.GetImage(fullPath, args.Id, false, null, null);
        }

    }
}
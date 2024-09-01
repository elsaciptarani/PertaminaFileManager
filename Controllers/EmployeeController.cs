using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PertaminaFileManager.Data;
using PertaminaFileManager.Models;
using PertaminaFileManager.Models.Base;
using Syncfusion.Blazor.FileManager;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PertaminaFileManager.Controllers
{
    [Route("api/[controller]")]
    public class EmployeeController : Controller
    {
        public PhysicalFileProvider operation;
        public string basePath;
        private readonly ApplicationDbContext _context; // Tambahkan context untuk mengakses database
        string root = "wwwroot\\Employee";

        // Tambahkan batas ukuran file maksimum dalam byte (10 MB)
        private const long MaxFileSize = 10485760; // 10 MB

        // Constructor menerima ApplicationDbContext melalui dependency injection
        public EmployeeController(IWebHostEnvironment hostingEnvironment, ApplicationDbContext context)
        {
            if (hostingEnvironment == null)
                throw new ArgumentNullException(nameof(hostingEnvironment), "Hosting environment is null.");

            if (context == null)
                throw new ArgumentNullException(nameof(context), "Database context is null.");

            basePath = hostingEnvironment.ContentRootPath ?? throw new ArgumentNullException(nameof(hostingEnvironment.ContentRootPath));
            operation = new PhysicalFileProvider();
            operation.RootFolder(Path.Combine(basePath, root)); // Menetapkan folder root tempat file berada.
            _context = context;
        }

        // Memproses operasi File Manager
        [Route("FileOperations")]
        public object? FileOperations([FromBody] FileManagerDirectoryContent args, string Role)
        {
            if (args == null)
                return BadRequest("Invalid input data.");

            if (operation == null)
                throw new InvalidOperationException("File provider operation is not initialized.");

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

            return BadRequest("Invalid action.");
        }

        // Mengunggah file dan menyimpan detail ke database
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

        // Mendownload file/folder yang dipilih
        [Route("Download")]
        public IActionResult Download(string downloadInput)
        {
            if (string.IsNullOrEmpty(downloadInput))
                return BadRequest("Invalid download input.");

            FileManagerDirectoryContent args = JsonConvert.DeserializeObject<FileManagerDirectoryContent>(downloadInput);
            if (args == null)
                return BadRequest("Failed to parse download input.");

            return operation.Download(args.Path, args.Names, args.Data);
        }

        // Mendapatkan gambar dari path yang diberikan
        [Route("GetImage")]
        public IActionResult GetImage(FileManagerDirectoryContent args)
        {
            if (args == null)
                return BadRequest("Invalid image request.");

            return operation.GetImage(args.Path, args.Id, false, null, null);
        }
    }
}

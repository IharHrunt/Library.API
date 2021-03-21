using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    //[ApiController]
    [Route("api/files")]
    public class FilesController : Controller
    {
        private ILibraryRepository _libraryRepository;
        private IConfiguration _configuration;
        string _storage; 

        public FilesController(ILibraryRepository libraryRepository, IConfiguration configuration)
        {
            _libraryRepository = libraryRepository;
            _configuration = configuration;
            _storage = _configuration.GetValue<string>("storages:local");
        }

        [HttpGet("{filename}")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (fileName == null)
            {
                return Content("fileName not present");
            }

            var path = Path.Combine(_storage, fileName);

            try
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, GetContentType(path), Path.GetFileName(path));
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet("random/images")]
        public async Task<IActionResult> DownloadRandom()
        {
            var files = await _libraryRepository.GetAllFilesAsync();

            var random = new Random();
            int index = random.Next(files.Count);
            var path = files[index].Path;

            try
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, GetContentType(path), Path.GetFileName(path));
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Content("file not selected");
            }

            var path = Path.Combine(_storage, file.FileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Entities.File filedb = new Entities.File
            {
                Name = file.FileName,
                Path = path,
                ContentType = GetContentType(path).ToString()
            };

            _libraryRepository.AddFileAsync(filedb);
            if (!(await _libraryRepository.SaveAsync()))
            {
                throw new Exception("Failed to save file data to db");                
            }

            return Ok();
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats  officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }
    }
}

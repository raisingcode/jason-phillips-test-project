using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public class DirectoryItem
{
    public string? Name { get; set; }
    public string? Type { get; set; } // "Directory" or "File"
    public long Size { get; set; } // Size in bytes, applicable for files
    public int FileCount { get; set; } // Number of files inside, applicable for directories
    public string? ErrorMessage { get; set; } // Error message, if any
}

namespace TestProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly string _targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

        // Helper function to count files inside a directory (recursively)
        private int GetFileCount(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return 0;
            return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories).Length;
        }


        [HttpGet("ListFiles")]
        public IActionResult GetFolderContent([FromQuery] string? folderpath)
        {
        string potentialPath = _targetDirectory;
        Console.WriteLine($"Folder path: {folderpath?.ToString()}");

        if (folderpath != null && folderpath.Trim() != "")
        {
            Console.WriteLine("Combining paths");
            var subPaths = folderpath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            potentialPath = Path.Combine(_targetDirectory, Path.Combine(subPaths));
        }
        if (!Directory.Exists(potentialPath))
        {
            return NotFound(new DirectoryItem { ErrorMessage = "Specified folder does not exist." });
        }

            var files = Directory.GetFiles(potentialPath);
            var directories = Directory.GetDirectories(potentialPath);

            var dirItems = directories.Select(d => new DirectoryItem {
                Name = Path.GetFileName(d),
                Type = "Directory",
                FileCount = GetFileCount(d)
            }).ToList();
            var fileItems = files.Select(f => new DirectoryItem { Name = Path.GetFileName(f), Type = "File", Size = new FileInfo(f).Length }).ToList();

            var result = dirItems.Concat(fileItems).ToList();
            return Ok(result);
        }
        [HttpPost("CreateFolder")]
        public IActionResult CreateFolder([FromBody] Dictionary<string,string> body)
        {
            var foldername = body["folderName"].ToString();
            var folderpath = body.ContainsKey("folderPath") ? body["folderPath"].ToString() : null;
            var potentialPath = "";
            if (folderpath != null && folderpath.Trim() != "")
            {
            Console.WriteLine("Combining paths");
            var subPaths = folderpath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            potentialPath = Path.Combine(_targetDirectory, Path.Combine(subPaths));
         
                if (!Directory.Exists(potentialPath))
                {
                    return NotFound(new DirectoryItem { ErrorMessage = "Specified parent folder does not exist." });
                }

            }
            else
            {
                potentialPath = Path.Combine(_targetDirectory, foldername);
            }

            Console.WriteLine(foldername);
            if (string.IsNullOrWhiteSpace(foldername))
                return BadRequest("Folder name cannot be empty.");

            var newFolderPath = Path.Combine(potentialPath, foldername);

            if (Directory.Exists(newFolderPath))
            return Conflict("Folder already exists.");

            Directory.CreateDirectory(newFolderPath);
            return Ok(new { foldername, path = newFolderPath });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string? folderpath)
        {
            string currentPath=""; 
            currentPath = folderpath != null && folderpath.Trim() != "" ? Path.Combine(_targetDirectory, folderpath) : _targetDirectory;
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!Directory.Exists(currentPath))
                Directory.CreateDirectory(currentPath);

            var filePath = Path.Combine(currentPath, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { file.FileName, filePath });
        }
    }
}
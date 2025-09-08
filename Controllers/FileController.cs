using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

// Add this class for search results
public class SearchResult
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; } // Relative path from target directory
    public string? FileExtension { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    public string? Type { get; set; } // "File" or "Directory"
}



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

        private readonly IConfiguration _configuration;
        private readonly string _targetDirectory;
        
        public FileController(IConfiguration configuration)
        {
            _configuration = configuration;
            _targetDirectory = _configuration.GetValue<string>("UploadPath") ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            // Ensure the target directory exists
            if (!Directory.Exists(_targetDirectory))
            {
                Directory.CreateDirectory(_targetDirectory);
            }
        }
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

            if (!string.IsNullOrWhiteSpace(folderpath))
            {
                Console.WriteLine("Combining paths");

                // Sanitize the input path to prevent directory traversal attacks
                folderpath = folderpath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Remove any parent directory references for security
                if (folderpath.Contains(".."))
                {
                    return BadRequest(new DirectoryItem { ErrorMessage = "Invalid path." });
                }

                // Split and combine the path properly
                var subPaths = folderpath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine("subPaths: " + string.Join(", ", subPaths));

                potentialPath = Path.Combine(_targetDirectory, Path.Combine(subPaths));
            }

            if (!Directory.Exists(potentialPath))
            {
                return NotFound(new DirectoryItem { ErrorMessage = "Specified folder does not exist." });
            }

            var files = Directory.GetFiles(potentialPath);
            var directories = Directory.GetDirectories(potentialPath);

            var dirItems = directories.Select(d => new DirectoryItem
            {
                Name = Path.GetFileName(d),
                Type = "Directory",
                FileCount = GetFileCount(d)
            }).ToList();

            var fileItems = files.Select(f => new DirectoryItem
            {
                Name = Path.GetFileName(f),
                Type = "File",
                Size = new FileInfo(f).Length
            }).ToList();

            var result = dirItems.Concat(fileItems).ToList();
            return Ok(result);
        }

        [HttpPost("CreateFolder")]
        public IActionResult CreateFolder([FromBody] Dictionary<string, string> body)
        {
            var foldername = body["folderName"].ToString();
            var folderpath = body.ContainsKey("folderPath") ? body["folderPath"].ToString() : null;
            Console.WriteLine($"Folder path: {folderpath?.ToString()}");
            Console.WriteLine("Target path: " + _targetDirectory);
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
                potentialPath = _targetDirectory;
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
        [HttpGet("searchfiles")]
public IActionResult SearchFiles([FromQuery] string searchTerm, [FromQuery] string? folderpath, [FromQuery] bool? includeSubfolders = true)
{
    if (string.IsNullOrWhiteSpace(searchTerm))
        return BadRequest(new { errorMessage = "Search term cannot be empty." });

    string searchPath = _targetDirectory;
            Console.WriteLine(searchTerm);
    // Handle specific folder path if provided
    if (!string.IsNullOrWhiteSpace(folderpath))
    {
        // Sanitize the input path to prevent directory traversal attacks
        folderpath = folderpath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Remove any parent directory references for security
                if (folderpath.Contains(".."))
                {
                    return BadRequest(new { errorMessage = "Invalid path." });
                }
                else if (folderpath == "/" || folderpath == "\\" || String.IsNullOrWhiteSpace(folderpath) || folderpath == "null")
                {
                    searchPath = _targetDirectory;
                }
                else
                {
                    var subPaths = folderpath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    searchPath = Path.Combine(_targetDirectory, Path.Combine(subPaths));
                }
                Console.WriteLine("Search path: " + searchPath);
        if (!Directory.Exists(searchPath))
                {
                    return NotFound(new { errorMessage = "Specified search folder does not exist." });
                }
    }

    try
    {
        var searchOption = includeSubfolders == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var matchingFiles = new List<SearchResult>();

                //Search for Directories matching the search term
        var directories = Directory.GetDirectories(searchPath, "*", searchOption)
            .Where(dir => Path.GetFileName(dir).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var dir in directories)
        {   
            var dirInfo = new DirectoryInfo(dir);
            
            // Calculate relative path from target directory
            var relativePath = Path.GetRelativePath(_targetDirectory, Path.GetDirectoryName(dir) ?? "");
            
            // Normalize path separators and handle root directory
            relativePath = relativePath.Replace('\\', '/');
            if (relativePath == ".")
                relativePath = "/";
            else if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;

            matchingFiles.Add(new SearchResult
            {
                FileName = dirInfo.Name,
                FilePath = relativePath,
                FileExtension = null,
                Size = 0,
                CreatedAt = dirInfo.CreationTime,
                ModifiedAt = dirInfo.LastWriteTime,
                Type = "Directory"
            });
        }
        
        // Search for files matching the search term
                    var files = Directory.GetFiles(searchPath, "*", searchOption)
            .Where(file => Path.GetFileName(file).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            
            // Calculate relative path from target directory
            var relativePath = Path.GetRelativePath(_targetDirectory, Path.GetDirectoryName(file) ?? "");
            
            // Normalize path separators and handle root directory
            relativePath = relativePath.Replace('\\', '/');
            if (relativePath == ".")
                relativePath = "/";
            else if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;

            matchingFiles.Add(new SearchResult
            {
                FileName = fileInfo.Name,
                FilePath = relativePath,
                FileExtension = fileInfo.Extension,
                Size = fileInfo.Length,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                Type = "File"
            });
        }

        return Ok(new 
        { 
            searchTerm,
            searchPath = Path.GetRelativePath(_targetDirectory, searchPath).Replace('\\', '/'),
            includeSubfolders,
            totalResults = matchingFiles.Count,
            results = matchingFiles.OrderBy(r => r.Type).ThenBy(r => r.FileName)
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { errorMessage = $"An error occurred while searching: {ex.Message}" });
    }
}
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string? folderpath)
        {
            string currentPath = "";
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

        [HttpGet("download")]
        public IActionResult DownloadFile([FromQuery] string filepath, string filename)
        {
            Console.WriteLine("download file called");
            string potentialPath = filepath == "/" ? Path.Combine(_targetDirectory, filename) :
                Path.Combine(_targetDirectory, filepath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), filename);

            if (!System.IO.File.Exists(potentialPath))
            {
                return NotFound(new { errorMessage = "Specified file does not exist." });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(potentialPath, FileMode.Open))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;
            var contentType = "APPLICATION/octet-stream";
            return File(memory, contentType, Path.GetFileName(potentialPath));
        }

        [HttpGet("getfileInfo")]

        public IActionResult GetFileInfo([FromQuery] string filepath, string filename)
        {

            Console.Write($"filepath: {filepath}, filename: {filename}");
            string potentialPath = filepath == "/" ? Path.Combine(_targetDirectory, filename) :
                Path.Combine(_targetDirectory, filepath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), filename);

            Console.WriteLine("target path: " + _targetDirectory);
            Console.WriteLine($"Potential file path: {potentialPath}");
            if (!System.IO.File.Exists(potentialPath))
            {
                return NotFound(new { errorMessage = "Specified file does not exist." });
            }

            var fileInfo = new FileInfo(potentialPath);
            var fileDetails = new
            {
                Name = fileInfo.Name,
                Size = fileInfo.Length,
                Type = fileInfo.Extension,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                FullPath = fileInfo.FullName
            };

            return Ok(fileDetails);
        }

    }
}
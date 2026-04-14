using Microsoft.AspNetCore.Mvc;
using WealthTracker.FileIntegration;
using WealthTracker.Models;
using WealthTracker.Services;

namespace WealthTracker.Controllers;

public class FileController (IFileService fileService) : BaseController

{
    [HttpPost]
    public async Task<IActionResult> Import([FromForm] IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("No file was uploaded.");

        var userId = GetUserId(); 
        
        var result = await fileService.ParseExcelFile(file, userId);

        return Success(result);
    }
}
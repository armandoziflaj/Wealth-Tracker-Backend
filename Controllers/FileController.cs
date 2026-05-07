using Microsoft.AspNetCore.Mvc;
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
    [HttpPost("PostAI")]
    public async Task<IActionResult> PostAi([FromForm] IFormFile file)
    {
        var userId = GetUserId(); 
        
        var result = await fileService.AiService(file, userId);

        return Success(result);
    }
    
    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "FileIntegration", "Transactions.xlsx");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Template file not found." });
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
    
        return File(
            fileBytes, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            "Transactions_Template.xlsx"
        );
    }
}
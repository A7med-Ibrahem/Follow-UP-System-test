using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WoundImagesController : ControllerBase
    {
        private readonly WoundImageService _woundImageService;

        public WoundImagesController(WoundImageService woundImageService)
        {
            _woundImageService = woundImageService;
        }

        // POST api/woundimages/upload/{reportId}
        [HttpPost("upload/{reportId}")]
        [Authorize(Roles = "Patient")]
        [EnableRateLimiting("upload")]
        public async Task<IActionResult> UploadImage(long reportId, IFormFile file)
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _woundImageService.UploadImageAsync(reportId, patientId, file);

            if (result == null)
                return BadRequest(new { message = "Invalid file or report not found. Max size 5MB, images only." });

            return Ok(result);
        }

        // GET api/woundimages/case/{caseId}
        [HttpGet("case/{caseId}")]
        [Authorize(Roles = "Doctor,Patient")]
        public async Task<IActionResult> GetCaseImages(long caseId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _woundImageService.GetCaseImagesAsync(caseId, userId);

            if (result == null)
                return NotFound(new { message = "Case not found or not accessible" });

            return Ok(result);
        }

        // DELETE api/woundimages/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> DeleteImage(long id)
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _woundImageService.DeleteImageAsync(id, patientId);

            if (!success)
                return NotFound(new { message = "Image not found or not accessible" });

            return Ok(new { message = "Image deleted" });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Services;
using System.Security.Claims;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CasesController : ControllerBase
    {
        private readonly CaseService _caseService;

        public CasesController(CaseService caseService)
        {
            _caseService = caseService;
        }

        // POST api/cases
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreateCase(CreateCaseRequestDto request)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var doctorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Doctor";

            try
            {
                var result = await _caseService.CreateCaseAsync(request, doctorId);
                if (result == null)
                    return BadRequest(new { message = "Failed to create case" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/cases?page=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetDoctorCases([FromQuery] PaginationRequestDto pagination)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _caseService.GetDoctorCasesAsync(doctorId, pagination);
            return Ok(result);
        }

        // GET api/cases/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetCaseById(long id)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _caseService.GetCaseByIdAsync(id, doctorId);
            if (result == null)
                return NotFound(new { message = "Case not found" });

            return Ok(result);
        }

        // PUT api/cases/{id}/close
        [HttpPut("{id}/close")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CloseCase(long id)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var doctorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Doctor";

            var success = await _caseService.CloseCaseAsync(id, doctorId);
            if (!success)
                return NotFound(new { message = "Case not found" });

            

            return Ok(new { message = "Case closed successfully" });
        }

        // GET api/cases/search?keyword=ahmed
        [HttpGet("search")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> SearchPatients([FromQuery] string keyword)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _caseService.SearchPatientsAsync(keyword, doctorId);
            return Ok(result);
        }

        // DELETE api/cases/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DeleteCase(long id)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _caseService.DeleteCaseAsync(id, doctorId);
            if (!success)
                return NotFound(new { message = "Case not found" });

            return Ok(new { message = "Case deleted successfully" });
        }
    }
}
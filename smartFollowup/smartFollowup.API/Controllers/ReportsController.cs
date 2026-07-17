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
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
        }

        // POST api/reports
        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> CreateReport(CreateReportRequestDto request)
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _reportService.CreateReportAsync(request, patientId);
            if (result == null)
                return BadRequest(new { message = "Case not found or not assigned to you" });

            return Ok(result);
        }

        // GET api/reports/case/{caseId}?page=1&pageSize=10
        [HttpGet("case/{caseId}")]
        [Authorize(Roles = "Doctor,Patient")]
        public async Task<IActionResult> GetCaseReports(long caseId, [FromQuery] PaginationRequestDto pagination)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _reportService.GetCaseReportsAsync(caseId, pagination, userId);

            if (result == null)
                return NotFound(new { message = "Case not found or not accessible" });

            return Ok(result);
        }

        // PUT api/reports/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Patient")]
        public async Task<IActionResult> UpdateReport(long id, UpdateReportRequestDto request)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _reportService.UpdateReportAsync(id, request, userId);

            if (result == null)
                return BadRequest(new { message = "Report not found, not accessible, or no longer editable (patients can only edit same-day reports)." });

            return Ok(result);
        }
    }
}
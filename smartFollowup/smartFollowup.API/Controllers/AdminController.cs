using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Services;
using System.Security.Claims;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }


        // GET api/admin/audit-logs
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? userName = null,
            [FromQuery] string? entityName = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _adminService.GetAuditLogsAsync(page, pageSize, userName, entityName, action, fromDate, toDate);
            return Ok(result);
        }

        // GET api/admin/audit-logs/entity-names
        [HttpGet("audit-logs/entity-names")]
        public async Task<IActionResult> GetAuditEntityNames()
        {
            var result = await _adminService.GetAuditEntityNamesAsync();
            return Ok(result);
        }

        // GET api/admin/patients
        [HttpGet("patients")]
        public async Task<IActionResult> GetPatients()
        {
            var result = await _adminService.GetPatientsAsync();
            return Ok(result);
        }

        // DELETE api/admin/doctors/{id}
        [HttpDelete("doctors/{id}")]
        public async Task<IActionResult> DeleteDoctor(long id)
        {
            var adminId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _adminService.DeleteDoctorAsync(id, adminId);
            if (!success)
                return NotFound(new { message = "Doctor not found" });

            return Ok(new { message = "Doctor account deleted" });
        }

        // GET api/admin/doctors
        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors([FromQuery] bool? isActive = null)
        {
            var result = await _adminService.GetDoctorsAsync(isActive);
            return Ok(result);
        }

        // GET api/admin/doctor-requests
        [HttpGet("doctor-requests")]
        public async Task<IActionResult> GetDoctorRequests([FromQuery] string? status = null)
        {
            var result = await _adminService.GetDoctorRequestsAsync(status);
            return Ok(result);
        }

        // PUT api/admin/doctor-requests/{id}/approve
        [HttpPut("doctor-requests/{id}/approve")]
        public async Task<IActionResult> ApproveDoctorRequest(long id)
        {
            var adminId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _adminService.ApproveDoctorRequestAsync(id, adminId);
            if (!success)
                return NotFound(new { message = "Request not found or already processed" });

            

            return Ok(new { message = "Doctor approved successfully" });
        }

        // PUT api/admin/doctor-requests/{id}/reject
        [HttpPut("doctor-requests/{id}/reject")]
        public async Task<IActionResult> RejectDoctorRequest(long id, RejectDoctorRequestDto request)
        {
            var adminId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _adminService.RejectDoctorRequestAsync(id, adminId, request.RejectionReason);
            if (!success)
                return NotFound(new { message = "Request not found or already processed" });

            

            return Ok(new { message = "Doctor request rejected" });
        }

        // PUT api/admin/doctors/{id}/toggle-status
        [HttpPut("doctors/{id}/toggle-status")]
        public async Task<IActionResult> ToggleDoctorStatus(long id)
        {
            var success = await _adminService.ToggleDoctorStatusAsync(id);
            if (!success)
                return NotFound(new { message = "Doctor not found" });

            return Ok(new { message = "Doctor status updated" });
        }

        // GET api/admin/analytics
        [HttpGet("Analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var result = await _adminService.GetAnalyticsAsync();
            return Ok(result);
        }
    }
}
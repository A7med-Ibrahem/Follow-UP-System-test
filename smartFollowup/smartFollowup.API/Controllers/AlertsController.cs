using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.Services;
using System.Security.Claims;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlertsController : ControllerBase
    {
        private readonly AlertService _alertService;

        public AlertsController(AlertService alertService)
        {
            _alertService = alertService;
        }

        // GET api/alerts
        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetDoctorAlerts()
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _alertService.GetDoctorAlertsAsync(doctorId);
            return Ok(result);
        }

        // PUT api/alerts/{id}/handle
        [HttpPut("{id}/handle")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> HandleAlert(long id)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _alertService.HandleAlertAsync(id, doctorId);
            if (!success)
                return NotFound(new { message = "Alert not found" });

            return Ok(new { message = "Alert handled successfully" });
        }

        // GET api/alerts/count
        [HttpGet("count")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetOpenAlertsCount()
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var count = await _alertService.GetOpenAlertsCountAsync(doctorId);
            return Ok(new { openAlerts = count });
        }
    }
}
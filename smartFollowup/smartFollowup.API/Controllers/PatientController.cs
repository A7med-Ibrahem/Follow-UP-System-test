using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Services;
using System.Security.Claims;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Patient")]
    public class PatientController : ControllerBase
    {
        private readonly PatientService _patientService;

        public PatientController(PatientService patientService)
        {
            _patientService = patientService;
        }

        // GET api/patient/profile
        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _patientService.GetPatientProfileAsync(patientId);
            if (result == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(result);
        }

        // PUT api/patient/profile
        [HttpPut("Profile")]
        public async Task<IActionResult> UpdateProfile(UpdatePatientProfileRequestDto request)
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _patientService.UpdatePatientProfileAsync(patientId, request);
            if (result == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(result);
        }

        // GET api/patient/risk-status
        [HttpGet("risk-status")]
        public async Task<IActionResult> GetRiskStatus()
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await _patientService.GetPatientProfileAsync(patientId);
            if (profile == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(new
            {
                riskLevel = profile.RiskLevel,
                caseId = profile.CaseId,
                operationDate = profile.OperationDate,
                latestRiskScore = profile.LatestRiskScore
            });
        }
    }
}
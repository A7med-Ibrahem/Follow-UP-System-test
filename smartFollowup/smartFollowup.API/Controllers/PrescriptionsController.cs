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
    public class PrescriptionsController : ControllerBase
    {
        private readonly PrescriptionService _prescriptionService;

        public PrescriptionsController(PrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        // POST api/prescriptions
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePrescription(CreatePrescriptionRequestDto request)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _prescriptionService.CreatePrescriptionAsync(request, doctorId);

            if (result == null)
                return BadRequest(new { message = "Case not found or not assigned to you" });

            

            return Ok(result);
        }

        // GET api/prescriptions/case/{caseId}
        [HttpGet("case/{caseId}")]
        [Authorize(Roles = "Doctor,Patient")]
        public async Task<IActionResult> GetCasePrescriptions(long caseId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _prescriptionService.GetCasePrescriptionsAsync(caseId, userId);

            if (result == null)
                return NotFound(new { message = "Case not found or not accessible" });

            return Ok(result);
        }

        // POST api/prescriptions/medications/{medicationId}/confirm
        [HttpPost("medications/{medicationId}/confirm")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ConfirmMedicationTaken(long medicationId, [FromQuery] long? adherenceId = null)
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _prescriptionService.ConfirmMedicationTakenAsync(medicationId, patientId, adherenceId);
            if (!success)
                return NotFound(new { message = "Medication not found" });

            return Ok(new { message = "Medication confirmed as taken" });
        }

        // GET api/prescriptions/case/{caseId}/today-schedule
        [HttpGet("case/{caseId}/today-schedule")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetTodaySchedule(long caseId)
        {
            var patientId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _prescriptionService.GetTodayScheduleAsync(caseId, patientId);

            if (result == null)
                return NotFound(new { message = "Case not found or not accessible" });

            return Ok(result);
        }

        // PUT api/prescriptions/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdatePrescription(long id, CreatePrescriptionRequestDto request)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _prescriptionService.UpdatePrescriptionAsync(id, request, doctorId);

            if (result == null)
                return NotFound(new { message = "Prescription not found" });

            

            return Ok(result);
        }
    }
}
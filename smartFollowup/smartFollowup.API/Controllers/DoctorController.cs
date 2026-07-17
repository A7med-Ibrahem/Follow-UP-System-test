using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Services;
using System.Security.Claims;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class DoctorController : ControllerBase
    {
        private readonly DoctorService _doctorService;

        public DoctorController(DoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        // GET api/doctor/profile
        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _doctorService.GetDoctorProfileAsync(doctorId);
            if (result == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(result);
        }

        // PUT api/doctor/profile
        [HttpPut("Profile")]
        public async Task<IActionResult> UpdateProfile(UpdateDoctorProfileRequestDto request)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _doctorService.UpdateDoctorProfileAsync(doctorId, request);
            if (result == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(result);
        }
    }
}
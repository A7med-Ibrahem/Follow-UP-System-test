using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/login
        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(result);
        }
        
        // POST api/auth/forgot-password
        [HttpPost("forgot-password")]
        [EnableRateLimiting("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request)
        {
            var success = await _authService.ForgotPasswordAsync(request.Email);
            if (!success)
                return NotFound(new { message = "Email not found" });

            return Ok(new { message = "Reset token sent to your email" });
        }

        // POST api/auth/verify-otp
        [HttpPost("verify-otp")]
        [EnableRateLimiting("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequestDto request)
        {
            var error = await _authService.VerifyOtpAsync(request);
            if (error != null)
                return BadRequest(new { message = error });

            return Ok(new { message = "Code verified." });
        }

        // POST api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
        {
            var error = await _authService.ResetPasswordAsync(request);
            if (error != null)
                return BadRequest(new { message = error });

            return Ok(new { message = "Password reset successfully" });
        }

        // POST api/auth/request-doctor
        [HttpPost("request-doctor")]
        public async Task<IActionResult> RequestDoctor(DoctorRequestDto request)
        {
            var success = await _authService.SubmitDoctorRequestAsync(request);
            if (!success)
                return BadRequest(new { message = "Email already exists" });

            return Ok(new { message = "Request submitted successfully, pending admin approval" });
        }

        // POST api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (result == null)
                return Unauthorized(new { message = "Invalid or expired refresh token" });

            return Ok(result);
        }

        // POST api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }

        // POST api/auth/change-password
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var newToken = await _authService.ChangePasswordAsync(userId, request);
            if (newToken == null)
                return BadRequest(new { message = "Current password is incorrect" });

            return Ok(new { message = "Password changed successfully", token = newToken });
        }

    }
}
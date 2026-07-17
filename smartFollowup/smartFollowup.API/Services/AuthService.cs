using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Enums;
using SmartFollowUp.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartFollowUp.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AuthService(AppDbContext context, IConfiguration configuration, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _configuration = configuration;
            _backgroundJobClient = backgroundJobClient;
        }

        // Login
        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null) return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            if (!string.IsNullOrWhiteSpace(request.Role) &&
                !string.Equals(request.Role, user.Role.ToString(), StringComparison.OrdinalIgnoreCase))
                return null;

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = GenerateToken(user),
                RefreshToken = refreshToken,
                RefreshTokenExpiry = user.RefreshTokenExpiry.Value,
                MustChangePassword = user.MustChangePassword
            };
        }

        
        // Submit Doctor Request
        public async Task<bool> SubmitDoctorRequestAsync(DoctorRequestDto request)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email) ||
                              await _context.DoctorRequests.AnyAsync(r => r.Email == request.Email);

            if (emailExists) return false;

            var doctorRequest = new DoctorRequest
            {
                Name = request.Name,
                Email = request.Email,
                Specialty = request.Specialty,
                LicenseNumber = request.LicenseNumber,
                Status = DoctorRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.DoctorRequests.Add(doctorRequest);
            await _context.SaveChangesAsync();

            return true;
        }

        // Forgot Password
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            // Admin accounts cannot use self-service password reset for security reasons
            if (user == null || user.Role == UserRole.Admin) return false;

            // Cooldown: prevent sending a new code within 60 seconds of the last one
            if (user.OtpExpiry.HasValue && user.OtpExpiry.Value > DateTime.UtcNow.AddMinutes(9))
                return true; // A code was already sent recently — pretend success, don't resend

            var otpCode = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otpCode;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(15);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _backgroundJobClient.Enqueue<EmailService>(x => x.SendPasswordResetEmailAsync(
                user.Email,
                user.Name,
                otpCode
            ));

            return true;
        }

        // Reset Password
        // Verify OTP (check-only, does not change the password or clear the code)
        public async Task<string?> VerifyOtpAsync(VerifyOtpRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null) return "Email not found.";

            if (user.OtpCode != request.Token)
                return "Invalid code. Please check the code and try again.";

            if (!user.OtpExpiry.HasValue || user.OtpExpiry.Value <= DateTime.UtcNow)
                return "This code has expired. Please request a new one.";

            return null; // null = valid
        }

        public async Task<string?> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null) return "Email not found.";

            if (user.OtpCode != request.Token)
                return "Invalid code. Please check the code and try again.";

            if (!user.OtpExpiry.HasValue || user.OtpExpiry.Value <= DateTime.UtcNow)
                return "This code has expired. Please request a new one.";

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return null; // null = success
        }

        // Logout
        public async Task<bool> LogoutAsync(long userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _context.SaveChangesAsync();

            return true;
        }

        // Change Password
        public async Task<string?> ChangePasswordAsync(long userId, ChangePasswordRequestDto request)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return null;

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return null;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return GenerateToken(user);
        }

        // Generate Refresh Token
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        // Refresh Token
        public async Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.RefreshToken == refreshToken &&
                    u.RefreshTokenExpiry > DateTime.UtcNow);

            if (user == null) return null;

            var newAccessToken = GenerateToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiry = user.RefreshTokenExpiry.Value
            };
        }

        // Generate JWT Token
        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("mustChangePassword", user.MustChangePassword.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
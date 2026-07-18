using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartFollowUp.API.Data;
using SmartFollowUp.API.Hubs;
using SmartFollowUp.API.Interfaces;
using SmartFollowUp.API.Services;
using SmartFollowUp.API.Validators;
using System.Text;
using System.Threading.RateLimiting;

namespace smartFollowup.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database
            builder.Services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            // Services
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<CaseService>();
            builder.Services.AddScoped<ReportService>();
            builder.Services.AddScoped<PrescriptionService>();
            builder.Services.AddScoped<NoteService>();
            builder.Services.AddScoped<AlertService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddSignalR();
            builder.Services.AddScoped<WoundImageService>();
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddScoped<PatientService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<EscalationService>();
            builder.Services.AddScoped<MedicationReminderService>();
            builder.Services.AddScoped<AuditService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<AuditInterceptor>();
            builder.Services.AddScoped<DoctorService>();
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            // Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                // Login — 5 محاولات كل دقيقة
                options.AddFixedWindowLimiter("login", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // API عام — 100 request كل دقيقة
                options.AddFixedWindowLimiter("api", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // Upload — 10 uploads كل دقيقة
                options.AddFixedWindowLimiter("upload", opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // Forgot Password — 3 طلبات كل 5 دقايق، لكل IP لوحده (منع سبام الإيميلات)
                options.AddPolicy("forgot-password", httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(5),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                // Verify OTP — أوسع لأنها بس تحقق، مش بتبعت إيميل، والمستخدم ممكن يغلط في الكود أكتر من مرة
                options.AddPolicy("verify-otp", httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(5),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.RejectionStatusCode = 429;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"message\":\"Too many attempts. Please wait a few minutes before trying again.\"}",
                        cancellationToken: token);
                };
            });

            // Hangfire
            builder.Services.AddHangfire(x => x.UseSqlServerStorage(
                builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHangfireServer();

            // تقليل محاولات إعادة المحاولة التلقائية من 10 (الافتراضي) لـ 3 بس، بفاصل زمني أوسع
            // عشان منضربش SMTP بمحاولات كتيرة في وقت قصير لو فيه مشكلة مؤقتة
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 3,
                DelaysInSeconds = new[] { 60, 300, 900 } // دقيقة، 5 دقايق، 15 دقيقة
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    // SignalR sends its negotiate request with credentials included, and per the
                    // CORS spec the wildcard "*" origin cannot be combined with credentials — the
                    // browser will block it. SetIsOriginAllowed(_ => true) still allows any origin,
                    // but echoes back the actual requesting origin instead of "*", which satisfies
                    // the spec and lets SignalR connect.
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };

                    // SignalR can't send a normal Authorization header on WebSocket/SSE
                    // connections, so the client (accessTokenFactory) sends the JWT as an
                    // "access_token" query string parameter instead. Without this, the hub
                    // connection gets a 401 right after negotiate succeeds.
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddControllers();
            builder.Services.AddAuthorization();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            
                app.UseSwagger();
                app.UseSwaggerUI();
            

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("AllowAll");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");

            // Hangfire
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[]
    {
        new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()
    }
            });
            RecurringJob.AddOrUpdate<EscalationService>(
                "emergency-escalation",
                x => x.CheckAndEscalateAsync(),
                "*/30 * * * *");

            RecurringJob.AddOrUpdate<MedicationReminderService>(
                "medication-reminders",
                x => x.SendDueRemindersAsync(),
                "*/30 * * * *");

            app.Run();
        }
    }
}
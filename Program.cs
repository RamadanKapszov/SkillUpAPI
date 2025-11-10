using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SkillUpAPI.Persistence;
using SkillUpAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext (keep your connection string in appsettings.json)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProgressService, ProgressService>();

builder.Services.AddControllers();

// JWT config (reads section "Jwt" from appsettings.json)
var jwt = builder.Configuration.GetSection("Jwt");
var secret = jwt.GetValue<string>("Secret") ?? throw new InvalidOperationException("JWT secret missing");
var key = Encoding.UTF8.GetBytes(secret);


// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy
            .WithOrigins("http://localhost:4200") // 👈 Angular frontend
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.GetValue<string>("Issuer"),
            ValidAudience = jwt.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Add Authorization (policies if needed)
builder.Services.AddAuthorization();

// Swagger with JWT (Bearer) support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});



var app = builder.Build();


// ✅ Enable Swagger (for testing)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillUpAPI v1");
    c.RoutePrefix = "swagger"; // or "" if you want it as default route
});

app.UseHttpsRedirection();

// ✅ Must come BEFORE CORS & Auth
app.UseRouting();

// ✅ CORS goes AFTER routing, BEFORE auth
app.UseCors("AllowAngular");

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniAts.Api.Data;
using MiniAts.Api.Data.Configurations;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Controllers;

// Public, unauthenticated: anyone can ask to get their organization onboarded.
[ApiController]
[Route("api/signup-requests")]
[AllowAnonymous]
public class SignupRequestsController(MiniAtsDbContext db) : ControllerBase
{
    public const string RateLimitPolicy = "signup";

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy)]
    public async Task<IActionResult> Create(CreateSignupRequest request)
    {
        // Pretend success so bots that fill the hidden field don't learn they were filtered.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Accepted();
        }

        var company = request.CompanyName?.Trim();
        var contact = request.ContactName?.Trim();
        var email = request.Email?.Trim();
        var message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();

        if (string.IsNullOrEmpty(company) || company.Length > SignupRequestConfiguration.CompanyNameMaxLength)
        {
            return BadRequest("Company name is required (max 200 characters).");
        }

        if (string.IsNullOrEmpty(contact) || contact.Length > SignupRequestConfiguration.ContactNameMaxLength)
        {
            return BadRequest("Your name is required (max 200 characters).");
        }

        if (string.IsNullOrEmpty(email) || email.Length > SignupRequestConfiguration.EmailMaxLength
            || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
        {
            return BadRequest("A valid email address is required.");
        }

        if (message?.Length > SignupRequestConfiguration.MessageMaxLength)
        {
            return BadRequest("Message is too long (max 2000 characters).");
        }

        db.SignupRequests.Add(new SignupRequest
        {
            CompanyName = company,
            ContactName = contact,
            Email = email,
            Message = message,
        });
        await db.SaveChangesAsync();

        return Accepted();
    }
}

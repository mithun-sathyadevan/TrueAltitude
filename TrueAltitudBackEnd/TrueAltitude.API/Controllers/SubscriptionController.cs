using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Application.Services;

namespace TrueAltitude.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration _configuration;

    public SubscriptionController(ISubscriptionService subscriptionService, IConfiguration configuration)
    {
        _subscriptionService = subscriptionService;
        _configuration = configuration;
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetPlansAsync();
        return Ok(plans);
    }

    [HttpPost("create-order")]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateSubscriptionOrderDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var result = await _subscriptionService.CreateOrderAsync(userId.Value, dto);
        return Ok(result);
    }

    [HttpPost("verify-payment")]
    [Authorize]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifySubscriptionPaymentDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var result = await _subscriptionService.VerifyPaymentAsync(userId.Value, dto);
        return Ok(result);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> RazorpayWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var secret = _configuration["Razorpay:WebhookSecret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(secret) || !IsValidWebhookSignature(body, signature, secret))
        {
            return Unauthorized(new { message = "Invalid webhook signature." });
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var eventName = root.TryGetProperty("event", out var eventElement) ? eventElement.GetString() ?? string.Empty : string.Empty;

        string? providerOrderId = null;
        string? providerPaymentId = null;
        string? providerSignature = null;
        string? failureReason = null;

        if (root.TryGetProperty("payload", out var payload)
            && payload.TryGetProperty("payment", out var payment)
            && payment.TryGetProperty("entity", out var entity))
        {
            providerOrderId = entity.TryGetProperty("order_id", out var orderIdElement) ? orderIdElement.GetString() : null;
            providerPaymentId = entity.TryGetProperty("id", out var paymentIdElement) ? paymentIdElement.GetString() : null;
            providerSignature = signature;

            if (entity.TryGetProperty("error_description", out var errorDescriptionElement))
            {
                failureReason = errorDescriptionElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(failureReason)
                && entity.TryGetProperty("error_reason", out var errorReasonElement))
            {
                failureReason = errorReasonElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(failureReason)
                && entity.TryGetProperty("error_source", out var errorSourceElement))
            {
                failureReason = errorSourceElement.GetString();
            }
        }

        await _subscriptionService.ProcessRazorpayWebhookAsync(eventName, providerOrderId, providerPaymentId, providerSignature, failureReason);
        return Ok(new { received = true });
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static bool IsValidWebhookSignature(string body, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var generated = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Equals(generated, signature.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }
}

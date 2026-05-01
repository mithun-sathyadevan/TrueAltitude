using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Application.Services;

namespace TrueAltitude.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
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

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}

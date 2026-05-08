using SHIELDON.Application.Features.Users.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All profile endpoints require authentication
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdStr!);
    }

    /// <summary>
    /// GET /api/profile
    /// Returns the currently authenticated user's profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserProfileResponse>.Ok(profile, "Profile retrieved successfully."));
    }

    /// <summary>
    /// PATCH /api/profile
    /// Updates the text-based profile fields (e.g., First Name, Last Name).
    /// </summary>
    [HttpPatch]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await _profileService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<UserProfileResponse>.Ok(profile, "Profile updated successfully."));
    }

    /// <summary>
    /// POST /api/profile/picture
    /// Uploads and resizes the user's profile picture using multipart/form-data.
    /// </summary>
    [HttpPost("picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file was uploaded.", null));

        var userId = GetUserId();
        
        using var stream = file.OpenReadStream();
        var profile = await _profileService.UploadProfilePictureAsync(
            userId, stream, file.ContentType, file.FileName, cancellationToken);

        return Ok(ApiResponse<UserProfileResponse>.Ok(profile, "Profile picture updated successfully."));
    }

    /// <summary>
    /// PATCH /api/profile/password
    /// Securely changes the user's password.
    /// </summary>
    [HttpPatch("password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _profileService.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully."));
    }

    /// <summary>
    /// PATCH /api/profile/onboarding-complete
    /// Marks the onboarding tour as completed for the current user.
    /// Called silently by the frontend Shepherd.js service on tour finish or skip.
    /// </summary>
    [HttpPatch("onboarding-complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteOnboarding(CancellationToken cancellationToken)
    {
        await _profileService.CompleteOnboardingAsync(GetUserId(), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// PATCH /api/profile/onboarding-reset
    /// Resets the onboarding tour flag so it will show again on next login.
    /// Triggered by the "Replay Tour" button on the Profile page.
    /// </summary>
    [HttpPatch("onboarding-reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetOnboarding(CancellationToken cancellationToken)
    {
        await _profileService.ResetOnboardingAsync(GetUserId(), cancellationToken);
        return NoContent();
    }
}

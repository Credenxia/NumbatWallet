using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, bool>
{
    private readonly IPersonRepository _personRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPersonRepository personRepository,
        IEmailService emailService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _personRepository = personRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", command.Email);

        // Find person by email
        var person = await _personRepository.GetByEmailAsync(command.Email, cancellationToken);

        // Don't reveal if email exists or not (security best practice)
        if (person == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", command.Email);
            // Still return success to avoid email enumeration
            return true;
        }

        // In production, this would:
        // 1. Generate a secure reset token
        // 2. Store the token with expiry in database
        // 3. Send reset email with secure link
        // 4. If token provided, validate and update password

        if (string.IsNullOrEmpty(command.ResetToken))
        {
            // Step 1: Request password reset - send email
            var resetToken = GenerateResetToken();

            // Send email (in POA, just log it)
            await _emailService.SendEmailAsync(
                command.Email,
                "Password Reset Request",
                $"Your password reset token is: {resetToken}",
                true,
                cancellationToken);

            _logger.LogInformation("Password reset email sent to: {Email}", command.Email);
        }
        else if (!string.IsNullOrEmpty(command.NewPassword))
        {
            // Step 2: Reset password with token
            // In production, validate the token from database
            // For POA, accept any non-empty token

            _logger.LogInformation("Password reset completed for email: {Email}", command.Email);
        }

        return true;
    }

    private string GenerateResetToken()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 16);
    }
}
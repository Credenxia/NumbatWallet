using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class ShareCredentialCommandHandler : ICommandHandler<ShareCredentialCommand, ShareCredentialResult>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly ICredentialSharingService _sharingService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ShareCredentialCommandHandler> _logger;

    public ShareCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        ICredentialSharingService sharingService,
        IEmailService emailService,
        ILogger<ShareCredentialCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _sharingService = sharingService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ShareCredentialResult> HandleAsync(ShareCredentialCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sharing credential {CredentialId} with {Recipient}",
            command.CredentialId, command.RecipientEmail);

        // Get credential
        var credential = await _credentialRepository.GetByIdAsync(command.CredentialId, cancellationToken)
            ?? throw new NotFoundException($"Credential with ID {command.CredentialId} not found");

        // Check if credential can be shared
        if (credential.Status != CredentialStatus.Active)
        {
            throw new BusinessRuleException("Cannot share inactive credential");
        }

        if (credential.IsExpired())
        {
            throw new BusinessRuleException("Cannot share expired credential");
        }

        // Generate share code
        var shareCode = GenerateShareCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(command.ExpiresInMinutes);

        // Create share link
        var shareUrl = await _sharingService.CreateShareLinkAsync(
            credential.Id,
            shareCode,
            expiresAt,
            command.RequirePin,
            command.Pin,
            cancellationToken);

        // Send notification email to recipient
        var emailBody = $@"
            <p>A credential has been shared with you.</p>
            <p>Credential Type: {credential.CredentialType}</p>
            <p>Access Link: <a href='{shareUrl}'>{shareUrl}</a></p>
            <p>This link will expire at: {expiresAt:yyyy-MM-dd HH:mm} UTC</p>
            {(command.RequirePin ? "<p>Note: You will need a PIN to access this credential.</p>" : "")}
        ";

        await _emailService.SendEmailAsync(
            command.RecipientEmail,
            "Credential Shared With You",
            emailBody,
            true,
            cancellationToken);

        _logger.LogInformation("Successfully created share link for credential {CredentialId}", command.CredentialId);

        return new ShareCredentialResult(shareUrl, shareCode, expiresAt);
    }

    private string GenerateShareCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, 16);
    }
}

// Interface for sharing service
public interface ICredentialSharingService
{
    Task<string> CreateShareLinkAsync(
        Guid credentialId,
        string shareCode,
        DateTime expiresAt,
        bool requirePin,
        string? pin,
        CancellationToken cancellationToken);
}
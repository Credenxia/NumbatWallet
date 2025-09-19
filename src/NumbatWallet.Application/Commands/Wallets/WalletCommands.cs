using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Wallets;

public record CreateWalletCommand(
    Guid PersonId,
    string Name,
    string UserId) : ICommand<WalletDto>;

public record UpdateWalletSettingsCommand(
    Guid WalletId,
    string? Name,
    bool? EnableBiometric,
    bool? RequirePinForAccess,
    int? AutoLockTimeoutMinutes) : ICommand<WalletDto>;

public record BackupWalletCommand(Guid WalletId) : ICommand<BackupResult>;

public record RestoreWalletCommand(
    string BackupData,
    string RecoveryPhrase,
    string? Pin) : ICommand<WalletDto>;

public record DeleteWalletCommand(Guid WalletId) : ICommand<bool>;

public record BackupResult(
    string BackupData,
    string BackupId,
    DateTime CreatedAt,
    bool IsEncrypted);
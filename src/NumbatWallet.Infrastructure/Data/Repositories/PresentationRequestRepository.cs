using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class PresentationRequestRepository : RepositoryBase<PresentationRequest, Guid>, IPresentationRequestRepository
{
    public PresentationRequestRepository(NumbatWalletDbContext context) : base(context)
    {
    }
}

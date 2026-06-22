using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

public interface IPresentationRequestRepository : IRepository<PresentationRequest, Guid>
{
}

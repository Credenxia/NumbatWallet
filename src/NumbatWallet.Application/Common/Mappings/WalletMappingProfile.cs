using AutoMapper;
using NumbatWallet.Application.Wallets.DTOs;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Application.Common.Mappings;

public class WalletMappingProfile : Profile
{
    public WalletMappingProfile()
    {
        CreateMap<Wallet, WalletDto>()
            .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.WalletName))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Did, opt => opt.MapFrom(src => src.WalletDid))
            .ForMember(dest => dest.CredentialCount, opt => opt.MapFrom(src => src.GetCredentials().Count))
            .ForMember(dest => dest.Description, opt => opt.Ignore());
    }
}
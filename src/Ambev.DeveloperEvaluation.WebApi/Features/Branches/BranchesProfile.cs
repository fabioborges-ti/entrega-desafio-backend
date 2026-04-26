using Ambev.DeveloperEvaluation.Application.Branches;
using Ambev.DeveloperEvaluation.Application.Branches.ListBranches;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches;

public class BranchesProfile : Profile
{
    public BranchesProfile()
    {
        CreateMap<BranchDto, BranchResponse>();
        CreateMap<ListBranchesResult, ListBranchesResponse>();
    }
}

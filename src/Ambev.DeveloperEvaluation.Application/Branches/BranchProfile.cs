using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Branches;

public class BranchProfile : Profile
{
    public BranchProfile()
    {
        CreateMap<Branch, BranchDto>();
    }
}

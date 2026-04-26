using Ambev.DeveloperEvaluation.Application.Branches;
using Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;
using Ambev.DeveloperEvaluation.Application.Branches.GetBranch;
using Ambev.DeveloperEvaluation.Application.Branches.ListBranches;
using Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Branches;

public class BranchHandlersTests
{
    private static IMapper Mapper()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<BranchDto>(Arg.Any<Branch>()).Returns(ci =>
        {
            var b = ci.Arg<Branch>();
            return new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Cnpj = b.Cnpj,
                CreatedAt = b.CreatedAt,
                CreatedByUserId = b.CreatedByUserId,
                LastModifiedAt = b.LastModifiedAt
            };
        });
        return mapper;
    }

    [Fact(DisplayName = "CreateBranch: comando inválido lança ValidationException")]
    public async Task CreateBranch_InvalidCommand_Throws()
    {
        var handler = new CreateBranchHandler(Substitute.For<IBranchRepository>(), Substitute.For<IUserRepository>(), Mapper());
        var act = async () => await handler.Handle(new CreateBranchCommand { Name = "", Cnpj = "", CreatedByUserId = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "CreateBranch: usuário inexistente lança ValidationException")]
    public async Task CreateBranch_UserNotFound_Throws()
    {
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new CreateBranchHandler(Substitute.For<IBranchRepository>(), users, Mapper());

        var cmd = new CreateBranchCommand { Name = "Filial", Cnpj = "12.345.678/0001-99", CreatedByUserId = 7 };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.CreatedByUserId));
    }

    [Fact(DisplayName = "CreateBranch: CNPJ duplicado lança ValidationException")]
    public async Task CreateBranch_DuplicateCnpj_Throws()
    {
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
        var branches = Substitute.For<IBranchRepository>();
        branches.ExistsCnpjAsync("12345678000199", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateBranchHandler(branches, users, Mapper());

        var cmd = new CreateBranchCommand { Name = "Filial", Cnpj = "12.345.678/0001-99", CreatedByUserId = 1 };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.Cnpj));
    }

    [Fact(DisplayName = "CreateBranch: dados válidos persiste e retorna DTO")]
    public async Task CreateBranch_Valid_PersistsAndReturnsDto()
    {
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
        var branches = Substitute.For<IBranchRepository>();
        branches.ExistsCnpjAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);
        branches.CreateAsync(Arg.Any<Branch>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Branch>());
        var handler = new CreateBranchHandler(branches, users, Mapper());

        var cmd = new CreateBranchCommand { Name = " Filial ", Cnpj = "12.345.678/0001-99", CreatedByUserId = 1 };
        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Name.Should().Be("Filial");
        dto.Cnpj.Should().Be("12345678000199");
        dto.CreatedByUserId.Should().Be(1);
        await branches.Received(1).CreateAsync(Arg.Any<Branch>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetBranch: id vazio lança ValidationException")]
    public async Task GetBranch_EmptyId_Throws()
    {
        var handler = new GetBranchHandler(Substitute.For<IBranchRepository>(), Mapper());
        var act = async () => await handler.Handle(new GetBranchCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "GetBranch: não encontrado lança KeyNotFound")]
    public async Task GetBranch_NotFound_Throws()
    {
        var branches = Substitute.For<IBranchRepository>();
        branches.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var handler = new GetBranchHandler(branches, Mapper());

        var act = async () => await handler.Handle(new GetBranchCommand { Id = Random.Shared.Next(1, int.MaxValue) }, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "GetBranch: encontrado retorna DTO")]
    public async Task GetBranch_Found_ReturnsDto()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var branches = Substitute.For<IBranchRepository>();
        branches.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Branch { Id = id, Name = "F", Cnpj = "12345678000199" });
        var handler = new GetBranchHandler(branches, Mapper());

        var dto = await handler.Handle(new GetBranchCommand { Id = id }, CancellationToken.None);

        dto.Id.Should().Be(id);
        dto.Name.Should().Be("F");
    }

    [Fact(DisplayName = "DeleteBranch: id vazio lança ValidationException")]
    public async Task DeleteBranch_EmptyId_Throws()
    {
        var handler = new DeleteBranchHandler(Substitute.For<IBranchRepository>());
        var act = async () => await handler.Handle(new DeleteBranchCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "DeleteBranch: não encontrado lança KeyNotFound")]
    public async Task DeleteBranch_NotFound_Throws()
    {
        var branches = Substitute.For<IBranchRepository>();
        branches.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var handler = new DeleteBranchHandler(branches);

        var act = async () => await handler.Handle(new DeleteBranchCommand { Id = Random.Shared.Next(1, int.MaxValue) }, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "DeleteBranch: com vendas vinculadas lança ValidationException")]
    public async Task DeleteBranch_HasSales_Throws()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var branches = Substitute.For<IBranchRepository>();
        branches.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Branch { Id = id });
        branches.HasSalesAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteBranchHandler(branches);

        var act = async () => await handler.Handle(new DeleteBranchCommand { Id = id }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("vendas vinculadas", StringComparison.Ordinal));
        await branches.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "DeleteBranch: válido remove e retorna Deleted=true")]
    public async Task DeleteBranch_Valid_Deletes()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var branches = Substitute.For<IBranchRepository>();
        branches.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Branch { Id = id });
        branches.HasSalesAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        branches.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteBranchHandler(branches);

        var result = await handler.Handle(new DeleteBranchCommand { Id = id }, CancellationToken.None);

        result.Deleted.Should().BeTrue();
    }

    [Fact(DisplayName = "UpdateBranch: comando inválido lança ValidationException")]
    public async Task UpdateBranch_InvalidCommand_Throws()
    {
        var handler = new UpdateBranchHandler(Substitute.For<IBranchRepository>(), Mapper());
        var act = async () => await handler.Handle(new UpdateBranchCommand { Id = 0, Name = "", Cnpj = "" }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "UpdateBranch: não encontrado lança KeyNotFound")]
    public async Task UpdateBranch_NotFound_Throws()
    {
        var branches = Substitute.For<IBranchRepository>();
        branches.GetTrackedByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var handler = new UpdateBranchHandler(branches, Mapper());

        var act = async () => await handler.Handle(new UpdateBranchCommand { Id = Random.Shared.Next(1, int.MaxValue), Name = "F", Cnpj = "12.345.678/0001-99" }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateBranch: CNPJ duplicado lança ValidationException")]
    public async Task UpdateBranch_DuplicateCnpj_Throws()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var branches = Substitute.For<IBranchRepository>();
        branches.GetTrackedByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Branch { Id = id });
        branches.ExistsCnpjAsync("12345678000199", id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateBranchHandler(branches, Mapper());

        var act = async () => await handler.Handle(new UpdateBranchCommand { Id = id, Name = "F", Cnpj = "12.345.678/0001-99" }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Cnpj");
    }

    [Fact(DisplayName = "UpdateBranch: válido persiste e retorna DTO")]
    public async Task UpdateBranch_Valid_Updates()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var entity = new Branch { Id = id };
        var branches = Substitute.For<IBranchRepository>();
        branches.GetTrackedByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        branches.ExistsCnpjAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        branches.UpdateAsync(entity, Arg.Any<CancellationToken>()).Returns(entity);
        var handler = new UpdateBranchHandler(branches, Mapper());

        var dto = await handler.Handle(new UpdateBranchCommand { Id = id, Name = " Updated ", Cnpj = "12.345.678/0001-99" }, CancellationToken.None);

        entity.Name.Should().Be("Updated");
        entity.Cnpj.Should().Be("12345678000199");
        dto.Name.Should().Be("Updated");
    }

    [Fact(DisplayName = "ListBranches: comando inválido lança ValidationException")]
    public async Task ListBranches_InvalidCommand_Throws()
    {
        var handler = new ListBranchesHandler(Substitute.For<IBranchRepository>(), Mapper());
        var act = async () => await handler.Handle(new ListBranchesCommand { Page = 0, Size = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "ListBranches: paginado retorna mapeamento e contagens")]
    public async Task ListBranches_Valid_ReturnsResult()
    {
        var branches = Substitute.For<IBranchRepository>();
        var items = new List<Branch>
        {
            new() { Id = Random.Shared.Next(1, int.MaxValue), Name = "A", Cnpj = "11111111111111" },
            new() { Id = Random.Shared.Next(1, int.MaxValue), Name = "B", Cnpj = "22222222222222" }
        };
        branches.ListPagedAsync(1, 10, Arg.Any<CancellationToken>()).Returns((items, 11));
        var handler = new ListBranchesHandler(branches, Mapper());

        var result = await handler.Handle(new ListBranchesCommand { Page = 1, Size = 10 }, CancellationToken.None);

        result.TotalItems.Should().Be(11);
        result.TotalPages.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.Data.Should().HaveCount(2);
        result.Data[0].Name.Should().Be("A");
    }
}

public class BranchValidatorsTests
{
    [Fact(DisplayName = "CreateBranchCommandValidator aceita CNPJ formatado e rejeita inválidos")]
    public void CreateBranchCommandValidator_ValidationRules()
    {
        var v = new CreateBranchCommandValidator();
        v.Validate(new CreateBranchCommand { Name = "X", Cnpj = "12.345.678/0001-99", CreatedByUserId = 1 }).IsValid.Should().BeTrue();
        v.Validate(new CreateBranchCommand { Name = "", Cnpj = "12345678000199", CreatedByUserId = 1 }).IsValid.Should().BeFalse();
        v.Validate(new CreateBranchCommand { Name = "X", Cnpj = "123", CreatedByUserId = 1 }).IsValid.Should().BeFalse();
        v.Validate(new CreateBranchCommand { Name = "X", Cnpj = "12345678000199", CreatedByUserId = 0 }).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "UpdateBranchCommandValidator: regras")]
    public void UpdateBranchCommandValidator_ValidationRules()
    {
        var v = new UpdateBranchCommandValidator();
        v.Validate(new UpdateBranchCommand { Id = Random.Shared.Next(1, int.MaxValue), Name = "X", Cnpj = "12345678000199" }).IsValid.Should().BeTrue();
        v.Validate(new UpdateBranchCommand { Id = 0, Name = "X", Cnpj = "12345678000199" }).IsValid.Should().BeFalse();
        v.Validate(new UpdateBranchCommand { Id = Random.Shared.Next(1, int.MaxValue), Name = "", Cnpj = "12345678000199" }).IsValid.Should().BeFalse();
        v.Validate(new UpdateBranchCommand { Id = Random.Shared.Next(1, int.MaxValue), Name = "X", Cnpj = "" }).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "DeleteBranchCommandValidator: regras")]
    public void DeleteBranchCommandValidator_ValidationRules()
    {
        var v = new DeleteBranchCommandValidator();
        v.Validate(new DeleteBranchCommand { Id = Random.Shared.Next(1, int.MaxValue) }).IsValid.Should().BeTrue();
        v.Validate(new DeleteBranchCommand { Id = 0 }).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "GetBranchCommandValidator: regras")]
    public void GetBranchCommandValidator_ValidationRules()
    {
        var v = new GetBranchCommandValidator();
        v.Validate(new GetBranchCommand { Id = Random.Shared.Next(1, int.MaxValue) }).IsValid.Should().BeTrue();
        v.Validate(new GetBranchCommand { Id = 0 }).IsValid.Should().BeFalse();
    }

    [Theory(DisplayName = "ListBranchesCommandValidator: limites")]
    [InlineData(1, 1, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void ListBranchesCommandValidator_ValidationRules(int page, int size, bool expected)
    {
        var v = new ListBranchesCommandValidator();
        v.Validate(new ListBranchesCommand { Page = page, Size = size }).IsValid.Should().Be(expected);
    }
}





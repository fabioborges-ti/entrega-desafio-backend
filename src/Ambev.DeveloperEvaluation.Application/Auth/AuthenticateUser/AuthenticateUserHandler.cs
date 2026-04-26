using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser
{
    public class AuthenticateUserHandler : IRequestHandler<AuthenticateUserCommand, AuthenticateUserResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<AuthenticateUserHandler> _logger;

        public AuthenticateUserHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<AuthenticateUserHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _logger = logger;
        }

        public async Task<AuthenticateUserResult> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                sw.Stop();
                _logger.LogWarning(
                    "Autenticação falhou (credenciais inválidas) após {ElapsedMs} ms",
                    sw.ElapsedMilliseconds);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var activeUserSpec = new ActiveUserSpecification();
            if (!activeUserSpec.IsSatisfiedBy(user))
            {
                sw.Stop();
                _logger.LogWarning(
                    "Autenticação falhou (usuário inativo) UserId={UserId} após {ElapsedMs} ms",
                    user.Id,
                    sw.ElapsedMilliseconds);
                throw new UnauthorizedAccessException("User is not active");
            }

            var token = _jwtTokenGenerator.GenerateToken(user);

            var displayName = $"{user.Name.FirstName} {user.Name.LastName}".Trim();
            if (string.IsNullOrEmpty(displayName))
                displayName = user.Username;

            sw.Stop();
            _logger.LogInformation(
                "Autenticação concluída UserId={UserId} Role={Role} em {ElapsedMs} ms",
                user.Id,
                user.Role,
                sw.ElapsedMilliseconds);

            return new AuthenticateUserResult
            {
                Token = token,
                Id = user.Id,
                Email = user.Email,
                Name = displayName,
                Role = user.Role.ToString()
            };
        }
    }
}

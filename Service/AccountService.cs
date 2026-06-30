using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Account;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repo;
        private readonly IMapper _mapper;

        public AccountService(IAccountRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<AccountViewDto>> GetAccountsWithRoleRangeAsync(int minRole, int maxRole)
        {
            var accounts = await _repo.GetAccountsWithRoleRangeAsync(minRole, maxRole);
            return _mapper.Map<List<AccountViewDto>>(accounts);
        }
    }
}

using Dedup.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dedup.Repositories
{
    public interface IAuthTokenRepository : IDisposable
    {
        void Reload(AuthTokens entity);
        void Add(ref AuthTokens item);
        AuthTokens Find(string key);
        void Remove(string id);
        void Update(AuthTokens item);
        void UpdateReturnUrl(string authId, string returnUrl);
        bool IsValidResource(string key);
    }
}

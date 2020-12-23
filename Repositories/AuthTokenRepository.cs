using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dedup.Models;
using Dedup.Data;
using Microsoft.EntityFrameworkCore;

namespace Dedup.Repositories
{
    public class AuthTokenRepository : IAuthTokenRepository
    {
        private DeDupContext _context;
        public AuthTokenRepository(DeDupContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Method: Add
        /// Description: It is used to add new token to authtoken table
        /// </summary>
        /// <param name="item"></param>
        public void Add(ref AuthTokens item)
        {
            //Get AuthTokens by auth_id
            var entity = Find(item.auth_id);
            if (entity == null)
            {
                _context.AuthTokens.Add(item);
                _context.SaveChanges();

                //attach resource reference
                _context.Entry(entity).Reference(e => e.Resource).Load();
            }
            else
            {
                entity.access_token = item.access_token;
                entity.refresh_token = item.refresh_token;
                entity.token_type = item.token_type;
                entity.user_id = item.user_id;
                entity.session_nonce = item.session_nonce;
                entity.expires_in = item.expires_in;
                _context.Entry(entity).State = EntityState.Modified;
                _context.SaveChanges();
            }

            //assign resource
            item.redirect_url = entity.redirect_url;
            item.Resource = entity.Resource;
        }

        /// <summary>
        /// Method: Find
        /// Description: It is used to get AuthTokens by auth_id
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public AuthTokens Find(string key)
        {
            if (_context.AuthTokens != null && _context.AuthTokens.Where(p => p.auth_id == key).Count() > 0)
            {
                var entity = _context.AuthTokens.FirstOrDefault(p => p.auth_id == key);
                //get latest database value
                Reload(entity);
                return entity;
            }

            return null;
        }

        /// <summary>
        /// Method: Reload
        /// Description: It is used to reload AuthTokens entity
        /// </summary>
        /// <param name="entity"></param>
        public void Reload(AuthTokens entity)
        {
            if (entity != null)
            {
                _context.Entry(entity).Reload();
                _context.Entry(entity).Reference(e => e.Resource).Load();
            }
        }

        /// <summary>
        /// Method: Remove
        /// Description: It is used to delete AuthToken by ccid from AuthTokens table
        /// </summary>
        /// <param name="id"></param>
        public void Remove(string id)
        {
            //Get AuthTokens by auth_id
            var entity = Find(id);
            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Deleted;
                _context.SaveChanges();
            }
        }

        public void Update(AuthTokens item)
        {
            //Get AuthTokens by auth_id
            var entity = Find(item.auth_id);
            if (entity != null)
            {
                entity.access_token = item.access_token;
                entity.refresh_token = item.refresh_token;
                entity.token_type = item.token_type;
                entity.user_id = item.user_id;
                entity.session_nonce = item.session_nonce;
                entity.expires_in = item.expires_in;
                _context.Entry(entity).State = EntityState.Modified;
                _context.SaveChanges();
            }
        }

        public bool IsValidResource(string key)
        {
            if (_context.Resources != null && _context.Resources.Where(p => p.uuid == key).Count() > 0)
            {
                return true;
            }

            return false;
        }

        public void UpdateReturnUrl(string authId, string returnUrl)
        {
            //Get AuthTokens by auth_id
            var entity = Find(authId);
            if (entity == null)
            {
                entity = new AuthTokens();
                entity.auth_id = authId;
                entity.access_token = "";
                entity.refresh_token = "";
                entity.expires_in = DateTime.MinValue;
                entity.redirect_url = returnUrl;
                _context.AuthTokens.Add(entity);
                _context.SaveChanges();
            }
            else
            {
                entity.redirect_url = returnUrl;
                _context.Entry(entity).State = EntityState.Modified;
                _context.SaveChanges();
            }
        }

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AuthTokenRepository()
        {
            Dispose(false);
        }
    }
}

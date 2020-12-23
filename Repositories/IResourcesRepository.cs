using System;
using System.Collections.Generic;
using Dedup.Models;
using Dedup.ViewModels;

namespace Dedup.Repositories
{
    public interface IResourcesRepository: IDisposable
    {
        void Add(Resources item, OauthGrant? authToken);
        IEnumerable<Resources> GetAll();
        Resources Find(string key);
        void Remove(string id);
        void UpdatePlan(string id, string plan);
        void Update(Resources item);
        void UpdateAppAndUserInfo(Resources item);
        void UpdateLicenseAgreement(string id, bool isLicenseAccepted);
    }
}

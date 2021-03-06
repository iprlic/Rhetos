﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace Rhetos.Dom.DefaultConcepts
{
    public class AuthorizationDataCache : IAuthorizationData
    {
        private readonly ILogger _logger;
        private readonly Lazy<AuthorizationDataLoader> _authorizationDataReader;
        private readonly ObjectCache _cache = MemoryCache.Default;

        public AuthorizationDataCache(
            ILogProvider logProvider,
            Lazy<AuthorizationDataLoader> authorizationDataReader)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _authorizationDataReader = authorizationDataReader;
        }

        // This property must not be static, and AuthorizationDataCache must be registered as InstancePerLifetimeScope,
        // in order to avoid multiple reading of data on parallel requests from the same user and to avoid locking between users.
        private object _userLevelCacheUpdateLock = new object();

        public static void ClearCache()
        {
            var cache = MemoryCache.Default;
            var killList = cache.Where(item => item.Key.StartsWith("AuthorizationDataCache.")).Select(item => item.Key).ToList();
            foreach (string key in killList)
                cache.Remove(key);
        }

        private static double? _defaultExpirationSeconds = null;

        private double GetDefaultExpirationSeconds()
        {
            if (_defaultExpirationSeconds == null)
            {
                string value = ConfigUtility.GetAppSetting("AuthorizationCacheExpirationSeconds");
                if (!string.IsNullOrEmpty(value))
                    _defaultExpirationSeconds = double.Parse(value);
                else
                    _defaultExpirationSeconds = 30;
            }
            return _defaultExpirationSeconds.Value;
        }

        private T CacheGetOrAdd<T>(string key, Func<T> valueCreator, double absoluteExpirationInSeconds)
        {
            T value = (T)_cache.Get(key);
            if (value == null)
                lock (_userLevelCacheUpdateLock) // Avoiding multiple invocations of valueCreator on parallel requests.
                {
                    value = (T)_cache.Get(key);
                    if (value == null)
                    {
                        value = valueCreator();
                        if (value != null)
                            _cache.Set(key, value, DateTimeOffset.Now.AddSeconds(absoluteExpirationInSeconds));
                    }
                }
            return value;
        }

        public PrincipalInfo GetPrincipal(string username)
        {
            return CacheGetOrAdd("AuthorizationDataCache.Principal." + username,
                () => _authorizationDataReader.Value.GetPrincipal(username),
                GetDefaultExpirationSeconds());
        }

        public IEnumerable<Guid> GetPrincipalRoles(IPrincipal principal)
        {
            return CacheGetOrAdd("AuthorizationDataCache.PrincipalRoles." + principal.Name + "." + principal.ID.ToString(),
                () => _authorizationDataReader.Value.GetPrincipalRoles(principal),
                GetDefaultExpirationSeconds());
        }

        public IEnumerable<Guid> GetRoleRoles(Guid roleId)
        {
            return CacheGetOrAdd("AuthorizationDataCache.RoleRoles." + roleId.ToString(),
                () => _authorizationDataReader.Value.GetRoleRoles(roleId),
                GetDefaultExpirationSeconds());
        }

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        public IEnumerable<PrincipalPermissionInfo> GetPrincipalPermissions(IPrincipal principal, IEnumerable<Guid> claimIds = null)
        {
            return CacheGetOrAdd("AuthorizationDataCache.PrincipalPermissions." + principal.Name + "." + principal.ID.ToString(),
                () => _authorizationDataReader.Value.GetPrincipalPermissions(principal, null),
                GetDefaultExpirationSeconds());
        }

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        public IEnumerable<RolePermissionInfo> GetRolePermissions(IEnumerable<Guid> roleIds, IEnumerable<Guid> claimIds = null)
        {
            IEnumerable<RolePermissionInfo> result = null;

            Func<Guid, string> cacheKey = roleId => "AuthorizationDataCache.RolePermissions." + roleId.ToString();

            var missingCache = new List<Guid>();

            foreach (Guid roleId in roleIds)
            {
                var rolePermissions = (IEnumerable<RolePermissionInfo>)_cache.Get(cacheKey(roleId));
                if (rolePermissions != null)
                    result = result == null ? rolePermissions : result.Concat(rolePermissions);
                else
                    missingCache.Add(roleId);
            }

            var freshPermissions = _authorizationDataReader.Value.GetRolePermissions(missingCache, null);
            result = result == null ? freshPermissions : result.Concat(freshPermissions);

            foreach (var rolePermissions in freshPermissions.GroupBy(p => p.RoleID))
                _cache.Set(cacheKey(rolePermissions.Key), rolePermissions.ToList(), DateTimeOffset.Now.AddSeconds(GetDefaultExpirationSeconds()));

            return result ?? Enumerable.Empty<RolePermissionInfo>();
        }

        /// <summary>
        /// The function may return more roles than required.
        /// Note that the result will not include roles that do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        public IDictionary<Guid, string> GetRoles(IEnumerable<Guid> roleIds = null)
        {
            return CacheGetOrAdd("AuthorizationDataCache.Roles",
                () => _authorizationDataReader.Value.GetRoles(null),
                GetDefaultExpirationSeconds());
        }

        /// <summary>
        /// The function may return more claims than required.
        /// Note that the result will not include claims that are inactive or do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        public IDictionary<Claim, ClaimInfo> GetClaims(IEnumerable<Claim> requiredClaims = null)
        {
            return CacheGetOrAdd("AuthorizationDataCache.Claims",
                () => _authorizationDataReader.Value.GetClaims(null),
                GetDefaultExpirationSeconds() > 0 ? 60*60*24*365 : 0);
        }
    }
}

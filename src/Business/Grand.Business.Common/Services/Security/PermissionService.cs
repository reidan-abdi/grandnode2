using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;

namespace Grand.Business.Common.Services.Security;

public class PermissionService : IPermissionService
{
    private readonly IRepository<Permission> _permissions;
    private readonly IRepository<PermissionAction> _permissionActions;
    private readonly IWorkContext _workContext;
    private readonly IGroupService _groupService;
    private readonly ICache _cache;

    public PermissionService(IRepository<Permission> permissions, IRepository<PermissionAction> permissionActions, IWorkContext workContext, IGroupService groupService, ICache cache)
    {
        _permissions = permissions;
        _permissionActions = permissionActions;
        _workContext = workContext;
        _groupService = groupService;
        _cache = cache;
    }
    
    protected virtual async Task<bool> Authorize(string permissionSystemName, CustomerGroup customerGroup)
    {
        if (string.IsNullOrEmpty(permissionSystemName))
            return false;

        var key = string.Format(CacheKey.PERMISSIONS_ALLOWED_KEY, customerGroup.Id, permissionSystemName);
        return await _cache.GetAsync(key, async () =>
        {
            var permissionRecord =
                await Task.FromResult(
                    _permissions.Table.FirstOrDefault(x => x.SystemName == permissionSystemName));
            return permissionRecord?.CustomerGroups.Contains(customerGroup.Id) ?? false;
        });
    }
    
    public virtual async Task DeletePermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        await _permissions.DeleteAsync(permission);
        await _cache.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public virtual Task<Permission> GetPermissionById(string permissionId)
    {
        return _permissions.GetByIdAsync(permissionId);
    }
    
    public virtual async Task<Permission> GetPermissionBySystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return await Task.FromResult<Permission>(null);

        var query = from pr in _permissions.Table
            where pr.SystemName == systemName
            select pr;

        return await Task.FromResult(query.FirstOrDefault());
    }

    public virtual async Task<IList<Permission>> GetAllPermissions()
    {
        var query = from pr in _permissions.Table
            orderby pr.Name
            select pr;
        
        return await Task.FromResult(query.ToList());
    }

    public virtual async Task InsertPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        await _permissions.InsertAsync(permission);

        await _cache.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public virtual async Task UpdatePermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        await _permissions.UpdateAsync(permission);

        await _cache.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public virtual async Task<bool> Authorize(Permission permission)
    {
        return await Authorize(permission, _workContext.CurrentCustomer);
    }

    public virtual async Task<bool> Authorize(Permission permission, Customer customer)
    {
        if (permission == null)
            return false;

        if (customer == null)
            return false;

        return await Authorize(permission.SystemName, customer);
    }

    public virtual async Task<bool> Authorize(string permissionSystemName)
    {
        return await Authorize(permissionSystemName, _workContext.CurrentCustomer);
    }

    public virtual async Task<bool> Authorize(string permissionSystemName, Customer customer)
    {
        if (string.IsNullOrEmpty(permissionSystemName))
            return false;

        var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
        foreach (var group in customerGroups)
            if (await Authorize(permissionSystemName, group))
                //yes, we have such permission
                return true;

        //no permission found
        return false;
    }

    public virtual async Task<IList<PermissionAction>> GetPermissionActions(string systemName, string customerGroupId)
    {
        return await Task.FromResult(_permissionActions.Table
            .Where(x => x.SystemName == systemName && x.CustomerGroupId == customerGroupId).ToList());
    }

    public virtual async Task InsertPermissionAction(PermissionAction permissionAction)
    {
        ArgumentNullException.ThrowIfNull(permissionAction);

        //insert
        await _permissionActions.InsertAsync(permissionAction);
        //clear cache
        await _cache.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public virtual async Task DeletePermissionAction(PermissionAction permissionAction)
    {
        ArgumentNullException.ThrowIfNull(permissionAction);

        //delete
        await _permissionActions.DeleteAsync(permissionAction);
        //clear cache
        await _cache.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public virtual async Task<bool> AuthorizeAction(string permissionSystemName, string permissionActionName)
    {
        if (string.IsNullOrEmpty(permissionSystemName) || string.IsNullOrEmpty(permissionActionName))
            return false;

        if (!await Authorize(permissionSystemName))
            return false;

        var customerGroups = await _groupService.GetAllByIds(_workContext.CurrentCustomer.Groups.ToArray());
        foreach (var group in customerGroups)
        {
            if (!await Authorize(permissionSystemName, group))
                continue;

            var key = string.Format(CacheKey.PERMISSIONS_ALLOWED_ACTION_KEY, group.Id, permissionSystemName,
                permissionActionName);
            var permissionAction = await _cache.GetAsync(key, async () =>
            {
                return await Task.FromResult(_permissionActions.Table
                    .FirstOrDefault(x =>
                        x.SystemName == permissionSystemName && x.CustomerGroupId == group.Id &&
                        x.Action == permissionActionName));
            });
            if (permissionAction != null)
                return false;
        }

        return true;
    }
}
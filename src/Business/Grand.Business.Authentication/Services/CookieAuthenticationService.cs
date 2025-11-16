using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Represents service using cookie middleware for the authentication
/// </summary>
public class CookieAuthenticationService : IGrandAuthenticationService
{
    public CookieAuthenticationService(
        CustomerSettings customerSettings,
        ICustomerService customerService,
        IGroupService groupService,
        IHttpContextAccessor httpContextAccessor,
        SecurityConfig securityConfig)
    {
        _customerSettings = customerSettings;
        _customerService = customerService;
        _groupService = groupService;
        _httpContextAccessor = httpContextAccessor;
        _securityConfig = securityConfig;
    }
    
    private string CustomerCookieName => $"{_securityConfig.CookiePrefix}Customer";
    private readonly CustomerSettings _customerSettings;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SecurityConfig _securityConfig;
    private Customer _cachedCustomer;
    
    public virtual async Task SignIn(Customer customer, bool isPersistent)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var claims = new List<Claim>();
        
        if (!string.IsNullOrEmpty(customer.Username))
            claims.Add(new Claim(ClaimTypes.Name, customer.Username, ClaimValueTypes.String, _securityConfig.CookieClaimsIssuer));

        if (!string.IsNullOrEmpty(customer.Email))
            claims.Add(new Claim(ClaimTypes.Email, customer.Email, ClaimValueTypes.Email, _securityConfig.CookieClaimsIssuer));

        var passwordToken = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordToken);
        if (string.IsNullOrEmpty(passwordToken))
        {
            var passwordGuid = Guid.NewGuid().ToString();
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.PasswordToken, passwordGuid);
            claims.Add(new Claim(ClaimTypes.UserData, passwordGuid, ClaimValueTypes.String, _securityConfig.CookieClaimsIssuer));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.UserData, passwordToken, ClaimValueTypes.String, _securityConfig.CookieClaimsIssuer));
        }

        //create principal for the present scheme of authentication
        var identity = new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        //set value that indicates whether the session is persisted and the time at which the authentication was issued
        var cookieProperties = new AuthenticationProperties {
            IsPersistent = isPersistent,
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddHours(_securityConfig.CookieAuthExpires)
        };

        if (_httpContextAccessor.HttpContext != null)
        {
            // Drop the guest cookie
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(CustomerCookieName);
            await _httpContextAccessor.HttpContext.SignInAsync(GrandCookieAuthenticationDefaults.AuthenticationScheme, principal, cookieProperties);
        }

        //cache authenticated customer
        _cachedCustomer = customer;
    }

    public virtual async Task SignOut()
    {
        _cachedCustomer = null;

        //and then sign out customer from the present scheme of authentication
        if (_httpContextAccessor.HttpContext != null)
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(GrandCookieAuthenticationDefaults.AuthenticationScheme);
            await _httpContextAccessor.HttpContext.SignOutAsync(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme);
        }
    }

    public virtual async Task<Customer> GetAuthenticatedCustomer()
    {
        //check if there is a cached customer
        if (_cachedCustomer != null)
            return _cachedCustomer;

        //get the authenticated user identity
        if (_httpContextAccessor.HttpContext == null) return _cachedCustomer;
        var authenticateResult =  await _httpContextAccessor.HttpContext.AuthenticateAsync(GrandCookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return null;

        Customer customer = null;
        if (_customerSettings.UsernamesEnabled)
        {
            //get customer by username if exists
            var userName = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Name  && claim.Issuer.Equals(_securityConfig.CookieClaimsIssuer, StringComparison.InvariantCultureIgnoreCase));
            if (userName != null)
                customer = await _customerService.GetCustomerByName(userName.Value);
        }
        else
        {
            //get customer by email
            var email = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Email && claim.Issuer.Equals(_securityConfig.CookieClaimsIssuer, StringComparison.InvariantCultureIgnoreCase));
            if (email != null)
                customer = await _customerService.GetCustomerByEmail(email.Value);
        }

        if (customer != null)
        {
            var passwordToken = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordToken);
            if (!string.IsNullOrEmpty(passwordToken))
            {
                var token = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.UserData && claim.Issuer.Equals(_securityConfig.CookieClaimsIssuer, StringComparison.InvariantCultureIgnoreCase));
                if (token == null || token.Value != passwordToken) 
                    customer = null;
            }
        }

        //Check if the found customer is available
        if (customer is not { Active: true } || customer.Deleted || !await _groupService.IsRegistered(customer))
            return null;

        //Cache the authenticated customer
        _cachedCustomer = customer;

        return _cachedCustomer;
    }

    public virtual Task<string> GetCustomerGuid()
    {
        return _httpContextAccessor.HttpContext?.Request == null
            ? Task.FromResult<string>(null)
            : Task.FromResult(_httpContextAccessor.HttpContext.Request.Cookies[CustomerCookieName]);
    }

    public virtual Task SetCustomerGuid(Guid customerGuid)
    {
        if (_httpContextAccessor.HttpContext?.Response == null)
            return Task.CompletedTask;

        //Delete existing cookie value
        _httpContextAccessor.HttpContext.Response.Cookies.Delete(CustomerCookieName);

        //Get the date date of current cookie expiration
        var cookieExpiresDate = DateTime.UtcNow.AddHours(_securityConfig.CookieAuthExpires);

        //If provided guid is empty (only remove cookies)
        if (customerGuid == Guid.Empty)
            return Task.CompletedTask;

        //set new cookie value
        var options = new CookieOptions {
            HttpOnly = true,
            Expires = cookieExpiresDate
        };
        
        _httpContextAccessor.HttpContext.Response.Cookies.Append(CustomerCookieName, customerGuid.ToString(), options);

        return Task.CompletedTask;
    }
}
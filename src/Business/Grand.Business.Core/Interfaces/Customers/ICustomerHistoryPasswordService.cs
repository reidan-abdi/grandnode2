using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Customers;

public interface ICustomerHistoryPasswordService
{
    Task<IList<CustomerHistoryPassword>> GetPasswords(string customerId, int passwordsToReturn);

    Task InsertCustomerPassword(Customer customer);
}
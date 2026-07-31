namespace MenuGoBE.Exceptions;

public class CustomerConflictException : Exception
{
    public string ExistingCustomerName { get; }

    public CustomerConflictException(string message, string existingCustomerName) : base(message)
    {
        ExistingCustomerName = existingCustomerName;
    }
}

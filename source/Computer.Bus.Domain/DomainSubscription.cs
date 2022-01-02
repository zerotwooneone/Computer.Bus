using Computer.Bus.Domain.Contracts;

namespace Computer.Bus.Domain;

internal class DomainSubscription : ISubscription
{
    private readonly Computer.Bus.Contracts.Models.ISubscription _dtoSubscription;

    public DomainSubscription(Computer.Bus.Contracts.Models.ISubscription dtoSubscription)
    {
        _dtoSubscription = dtoSubscription;
    }
    public void Dispose()
    {
        _dtoSubscription.Dispose();
    }
}
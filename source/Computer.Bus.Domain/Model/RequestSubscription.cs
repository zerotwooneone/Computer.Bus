using Computer.Bus.Domain.Contracts.Models;

namespace Computer.Bus.Domain.Model;

internal class RequestSubscription : ISubscription
{
    private readonly Computer.Bus.Contracts.Models.ISubscription _dtoSubscription;

    public RequestSubscription(Computer.Bus.Contracts.Models.ISubscription dtoSubscription)
    {
        _dtoSubscription = dtoSubscription;
    }
    public void Dispose()
    {
        _dtoSubscription.Dispose();
    }
}
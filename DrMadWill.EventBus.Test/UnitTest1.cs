using Microsoft.Extensions.DependencyInjection;

namespace MDMS.EventBus.Test;


public class UnitTest1
{
    private ServiceCollection services;

    public UnitTest1(ServiceCollection services)
    {
        this.services = services;
    }

    [Fact]
    public void Test1()
    {

    }
}

namespace BovineLabs.Core.Tests
{
    public interface ITestInterface0
    {
    }

    public interface ITestInterface1
    {
    }

    public interface ITestInterface2
    {
    }

    public class TestImplementation0
    {
    }

    public class TestImplementation1 : ITestInterface2
    {
    }

    public class TestImplementation2 : ITestInterface2, ITestInterface1
    {
    }
}

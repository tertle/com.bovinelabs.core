// <copyright file="TestData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests
{
    /// <summary> Interface that tests no implementation. </summary>
    public interface ITestInterface0
    {
    }

    /// <summary> Interface that tests one implementation. </summary>
    public interface ITestInterface1
    {
    }

    /// <summary> Interface that tests multiple implementations. </summary>
    public interface ITestInterface2
    {
    }

    /// <summary> Implementation with no interfaces. </summary>
    public class TestImplementation0
    {
    }

    /// <summary> Implementation with 1 interface. </summary>
    public class TestImplementation1 : ITestInterface2
    {
    }

    /// <summary> Implementation with 2 interfaces. </summary>
    public class TestImplementation2 : ITestInterface2, ITestInterface1
    {
    }
}

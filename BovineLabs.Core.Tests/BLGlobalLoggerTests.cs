// <copyright file="BLGlobalLoggerTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests
{
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using Unity.Collections;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class BLGlobalLoggerTests
    {
        [TestCase(LogLevel.Fatal)]
        [TestCase(LogLevel.Error)]
        public void Log128_WithErrorLevel_EmitsError(LogLevel level)
        {
            var previousLevel = BLLogger.CurrentLogLevel.Data;

            try
            {
                BLLogger.CurrentLogLevel.Data = (int)LogLevel.Warning;
                var message = new FixedString128Bytes($"dispatch {level}");
                LogAssert.Expect(LogType.Error, new Regex($"E \\|.*{message}"));

                BLGlobalLogger.Log128(message, level);
            }
            finally
            {
                BLLogger.CurrentLogLevel.Data = previousLevel;
            }
        }
    }
}

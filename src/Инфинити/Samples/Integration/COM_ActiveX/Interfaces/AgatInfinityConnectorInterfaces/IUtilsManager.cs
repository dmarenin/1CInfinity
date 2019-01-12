//-----------------------------------------------------------------------
// <copyright file="IUtilsManager.cs" company="IntelTelecom">
//     Copyright IntelTelecom. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AgatInfinityConnector
{
    /// <summary>
    /// Прочие функции
    /// </summary>
    [ComVisible(true)]
    [Guid("9F96C567-D123-45BF-BF4E-C5280581B89F")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface IUtilsManager : IDisposable
    {
        // Configuration parameters
        [DispId(10000)]
        object GetConfigurationParameter(string Name, long IDObject);

        [DispId(10001)]
        bool TryGetConfigurationParameter(string Name, long IDObject, out object Value);

        [DispId(10002)]
        void SetConfigurationParameter(string Name, long IDObject, object Value);

        [ComVisible(false)]
        List<object> GetConfigurationParameters(List<string> Names, long IDObject);

        [ComVisible(false)]
        void SetConfigurationParameters(List<string> Names, long IDObject, List<object> Values);
    }

}

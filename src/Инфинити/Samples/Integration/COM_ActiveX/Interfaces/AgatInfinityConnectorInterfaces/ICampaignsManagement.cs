//-----------------------------------------------------------------------
// <copyright file="ICampaignsManagement.cs" company="IntelTelecom">
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
    /// Основной интерфейс для работы с кампаниями
    /// </summary>
    [ComVisible(true)]
    [Guid("2037C657-C41F-4BCB-9DCB-E4EC4656C265")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface ICampaignsManagement : IDisposable
    {
        /// <summary>
        /// Запустить кампанию
        /// </summary>
        [DispId(10000)]
        void StartCampaign(long IDCampaign);

        /// <summary>
        /// Остановить кампанию
        /// </summary>
        [DispId(10001)]
        void StopCampaign(long IDCampaign, bool bForce);

        /// <summary>
        /// Получить статус кампании
        /// </summary>
        [DispId(10002)]
        uint GetCampaignStatus(long IDCampaign);
    }

}

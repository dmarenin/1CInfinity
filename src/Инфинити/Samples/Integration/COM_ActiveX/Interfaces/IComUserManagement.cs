//-----------------------------------------------------------------------
// <copyright file="IUserManagement.cs" company="IntelTelecom">
//     Copyright IntelTelecom. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Cx.ActiveX
{
    /// <summary>
    /// Основной интерфейс для мониторинга пользователей.
    /// </summary>
    [ComVisible(true)]
    [Guid("77BC4DBD-FA4F-40A6-8FD5-E0E1738BDE2F")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface IComUsersManagement
    {
        /// <summary>
        /// Коллекция пользователей. В коллекции присутствуют все пользователи независимо от их 
        /// текущих статусов.
        /// </summary>
        [DispId(10000)]
        object Users { get; }

        /// <summary>
        /// Текущий залогиненный пользователь.
        /// </summary>
        [DispId(10001)]
        IComUser CurrentUser { get; }

        /// <summary>
        /// Обработчики событий для скриптовых языков
        /// </summary>
        [DispId(10002)]
        object StateChangedEvent { set; }
    }

    // IUsersManagementEvents Events
    [ComVisible(true)]
    [Guid("A1C9ED36-0DA9-4868-AEC6-6A575A91C741")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IUsersManagementEvents
    {
        /// <summary>
        /// Событие возникает при изменении статуса пользователя
        /// </summary>
        [DispId(1001)]
        //void StateChanged(IComUser User, long OldState, long State);
        void StateChanged(IComUser User, object OldState, object State);
        //[DispId(1002)]
        //void StateChanged_Test(IComUser User, long OldState, long State);

    }

}

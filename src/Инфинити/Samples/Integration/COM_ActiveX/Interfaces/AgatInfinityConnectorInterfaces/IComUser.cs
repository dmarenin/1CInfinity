//-----------------------------------------------------------------------
// <copyright file="IUser.cs" company="IntelTelecom">
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
    /// Основной интерфейс пользователя, предназначенный для получения информации и управления
    /// </summary>
    [ComVisible(true)]
    [Guid("957F291F-ABC6-4700-BF19-00C256B0DE76")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface IComUser
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        [DispId(10000)]
        long ID { get; }

        /// <summary>
        /// Имя пользователя, используемое для входа в систему
        /// </summary>
        [DispId(10001)]
        string Login { get; }

        /// <summary>
        /// Текущий статус пользователя
        /// </summary>
        [DispId(10002)]
        long State { get; set; }
        //UserState State { get; set; }

        /// <summary>
        /// Список внутренних телефонных номеров пользователя
        /// </summary>
        [DispId(10003)]
        object Extensions { get; }

        /// <summary>
        /// Вошел ли пользователь в систему
        /// </summary>
        [DispId(10004)]
        bool IsLoggedIn { get; }


        /// <summary>
        /// Войти в систему. Возвращает результат (успешно/неуспешно)
        /// </summary>
        [DispId(10005)]
        bool Logon(string Password, object IDRole);
        [DispId(10006)]
        long LogonEx(string Password, object IDRole);

        /// <summary>
        /// Выйти из системы
        /// </summary>
        [DispId(10007)]
        void Logoff();

    }

}

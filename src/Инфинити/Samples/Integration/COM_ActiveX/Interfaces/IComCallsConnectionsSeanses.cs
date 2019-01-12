//-----------------------------------------------------------------------
// <copyright file="ICallsConnectionsSeances.cs" company="IntelTelecom">
//     Copyright IntelTelecom. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Cx.ActiveX
{
    /// <summary>
    /// Основной интерфейс для доступа к информации о вызовах, соединениях и сеансах
    /// </summary>
    [ComVisible(true)]
    [Guid("9F10F096-03F5-4CD4-B08B-50CB02F910C7")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface IComCallsConnectionsSeances
    {

        /// <summary>
        /// Получить набор соединений по идентификатору сеанса
        /// </summary>
        /// <param name="seanceID_">Идентификатор сеанса</param>
        /// <returns>Набор соединений</returns>
        [DispId(10000)]
        object GetConnectionsBySeanceID(string seanceID_);

        /// <summary>
        /// Получить набор соединений по идентификатору Call-а
        /// </summary>
        /// <param name="seanceID_">Идентификатор Call-а</param>
        /// <returns>Набор соединений</returns>
        [DispId(10001)]
        object GetConnectionsByCallID(string CallID);

        /// <summary>
        /// Получить набор соединений по диапазону времени, номеру телефона, идентификатору линии
        /// </summary>
        /// <param name="timeStartFrom_">Начало диапазона времени</param>
        /// <param name="timeStartTo_">Окончание диапазона времени</param>
        /// <param name="number_">Фрагмент номера телефона</param>
        /// <param name="lineID_">Идентификатор линии</param>
        /// <returns>Набор соединений</returns>
        [DispId(10002)]
        object GetConnections(DateTime timeStartFrom_, DateTime timeStartTo_, string number_, string lineID_);

        /// <summary>
        /// Получить соединение по идентификатору
        /// </summary>
        /// <param name="connectionID_">Идентификатор соединения</param>
        /// <returns>Соединение</returns>
        [DispId(10003)]
        object GetConnectionByID(long connectionID_);

        /// <summary>
        /// Получить набор звонков по диапазону времени и пользователю
        /// </summary>
        /// <param name="timeStartFrom_">Начало диапазона времени</param>
        /// <param name="timeStartTo_">Окончание диапазона времени</param>
        /// <param name="IDUser_">Пользователь</param>
        /// <returns></returns>
        [DispId(10004)]
        object GetCalls(DateTime timeStartFrom_, DateTime timeStartTo_, long IDUser_);

        /// <summary>
        /// Получить текущий набор соединений
        /// </summary>
        [DispId(10005)]
        object  GetCurrentConnections();

        
        /// Обработчики событий для скриптовых языков

        [DispId(20001)]
        object ConnectionCreatedEvent { set; }
        [DispId(20002)]
        object ConnectionDeletedEvent { set; }
        [DispId(20003)]
        object ConnectionChangedEvent { set; }
    }


    // IComCallsConnectionsSeances Events
    [ComVisible(true)]
    [Guid("658E7DE6-FCD4-4E79-995E-61462AE82186")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IComCallsConnectionsSeancesEvents
    {
        /// <summary>
        /// Создано новое соединение.
        /// </summary>
        [DispId(1001)]
        void ConnectionCreated(AgatInfinityConnector.IConnection Connection);

        /// <summary>
        /// Соединение удалено.
        /// </summary>
        [DispId(1002)]
        void ConnectionDeleted(AgatInfinityConnector.IConnection Connection);

        /// <summary>
        /// Соединение изменено.
        /// </summary>
        [DispId(1003)]
        void ConnectionChanged(AgatInfinityConnector.IConnection Connection);
    }

}

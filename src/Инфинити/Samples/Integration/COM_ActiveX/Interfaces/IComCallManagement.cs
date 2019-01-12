//-----------------------------------------------------------------------
// <copyright file="IComCallManagement.cs" company="IntelTelecom">
//     Copyright IntelTelecom. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Cx.ActiveX
{
    /// <summary>
    /// Основной интерфейс для мониторинга вызовов (звонков).
    /// </summary>
    [ComVisible(true)]
    [Guid("A14F2FF4-9904-489B-B3B6-5635145A6007")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface IComCallManagement
    {
        /// <summary>
        /// Внутренний номер - владелец вызовов. Может быть пустым, если получен интерфейс 
        /// для управления всеми вызовами.
        /// </summary>
        [DispId(10000)]
        string Extension { get; }

        /// <summary>
        /// Все вызовы, включая находящиеся в состоянии "Завершен", "Занято", "Ошибка" 
        /// (хранятся около 30 секунд после завершения)
        /// </summary>
        [DispId(10001)]
        object Calls { get; }

        /// <summary>
        /// Совершить новый вызов
        /// </summary>
        /// <param name="number_">Номер телефона для набора</param>
        /// <param name="callerName_">Имя звонящего абонента (опционально)</param>
        /// <param name="extension_">Extension, от имени которого совершается вызов. 
        /// Нужно заполнять в случае, если интерфейс получен без указания Extension</param>
        /// <returns>Интерфейс нового вызова</returns>
        [DispId(10002)]
        IComCall MakeCall(string number_, string callerName_, string extension_ = "");

        /// <summary>
        /// Установить статус внутреннего номера
        /// </summary>
        /// <param name="state_">Статус внутреннего номера</param>
        /// <param name="extension_">Extension, для которого нужно установить статус. 
        /// Нужно заполнять в случае, если интерфейс получен без указания Extension</param>
        [DispId(10003)]
        bool SetExtensionState(AgatInfinityConnector.ExtensionState state_, string extension_ = "");


        /// Обработчики событий для скриптовых языков

        [DispId(10004)]
        object CallCreatedEvent { set; }
        [DispId(10005)]
        object CallDeletedEvent { set; }
        [DispId(10006)]
        object ExtensionStateChangedEvent { set; }
        [DispId(10007)]
        object DisposedEvent { set; }

        [DispId(10008)]
        object StateChangedEvent { set; }
        [DispId(10009)]
        object NumberChangedEvent { set; }
        [DispId(10010)]
        object NameChangedEvent { set; }
        [DispId(10011)]
        object DialedNumberChangedEvent { set; }
        [DispId(10012)]
        object CommandsStateChangedEvent { set; }
        [DispId(10013)]
        object DigitsSentEvent { set; }
        [DispId(10014)]
        object AbonentCallInfoChangedEvent { set; }
    }

    // IComCallManagement Events
    [ComVisible(true)]
    [Guid("2CE69487-AB7F-4EA6-BA0A-1D1625B53FB0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface ICallManagementEvents
    {
        /// <summary>
        /// Создан новый вызов. На момент вызова события вызов уже содержится в коллекции Calls.
        /// </summary>
        [DispId(1001)]
        void CallCreated(IComCall call_);

        /// <summary>
        /// Вызов удален. На момент вызова события вызов еще содержится в коллекции Calls.
        /// </summary>
        [DispId(1002)]
        void CallDeleted(IComCall call_);

        /// <summary>
        /// Изменился статус внутреннего номера.
        /// </summary>
        [DispId(1003)]
        void ExtensionStateChanged(string Extension, AgatInfinityConnector.ExtensionState State);

        /// <summary>
        /// Интерфейс уничтожен. Дальнейшая работа с ним невозможна.
        /// </summary>
        [DispId(1004)]
        void Disposed();

        /// <summary>
        /// Состояние вызова изменилось
        /// </summary>
        [DispId(1005)]
        void StateChanged(IComCall call_, AgatInfinityConnector.CallState oldState, AgatInfinityConnector.CallState state);

        /// <summary>
        /// Номер абонента изменился
        /// </summary>
        [DispId(1006)]
        void NumberChanged(IComCall call_);

        /// <summary>
        /// Имя абонента изменилось
        /// </summary>
        [DispId(1007)]
        void NameChanged(IComCall call_);

        /// <summary>
        /// Набранный номер изменился
        /// </summary>
        [DispId(1008)]
        void DialedNumberChanged(IComCall call_);

        /// <summary>
        /// Состояние (доступность) команд изменилась
        /// </summary>
        [DispId(1009)]
        void CommandsStateChanged(IComCall call_);

        /// <summary>
        /// Выполнен тоновый донабор
        /// </summary>
        [DispId(1010)]
        void DigitsSent(IComCall call_, string digits);

        /// <summary>
        /// Состояние (доступность) команд изменилась
        /// </summary>
        [DispId(1011)]
        void AbonentCallInfoChanged(IComCall call_);

    }

}

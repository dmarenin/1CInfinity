//-----------------------------------------------------------------------
// <copyright file="ICall.cs" company="IntelTelecom">
//     Copyright IntelTelecom. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Cx.ActiveX
{
    /// <summary>
    /// Основной интерфейс вызова (звонка), предназначенный для получения информации и управления
    /// </summary>
    [ComVisible(true)]
    [Guid("3A36C9B6-9D14-430B-B179-A9EA07A789B6")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface IComCall
    {
        /// <summary>
        /// Уникальный идентификатор вызова. В зависимости от подключенного устройства,
        /// уникальность может обеспечиваться абсолютно, либо в пределах времени работы устройства.
        /// </summary>
        [DispId(10000)]
        string ID { get; }

        /// <summary>
        /// Уникальный идентификатор сеанса. Сеанс - это цепочка соединений, в которой участвует 
        /// данный вызов.
        /// </summary>
        [DispId(10001)]
        string SeanceID { get; }

        /// <summary>
        /// Внутренний номер - владелец вызова
        /// </summary>
        [DispId(10002)]
        string Extension { get; }

        /// <summary>
        /// Номер абонента
        /// </summary>
        [DispId(10003)]
        string Number { get; }

        /// <summary>
        /// Имя абонента
        /// </summary>
        [DispId(10004)]
        string Name { get; }

        /// <summary>
        /// Набранный номер (для входящих вызовов - номер городской телефонной линии)
        /// </summary>
        [DispId(10005)]
        string DialedNumber { get; }

        /// <summary>
        /// Состояние вызова
        /// </summary>
        [DispId(10006)]
        AgatInfinityConnector.CallState State { get; }

        /// <summary>
        /// Направление вызова
        /// </summary>
        [DispId(10007)]
        AgatInfinityConnector.CallDirection Direction { get; }

        /// <summary>
        /// Время начала вызова
        /// </summary>
        [DispId(10008)]
        DateTime StartTime { get; }

        /// <summary>
        /// Время последнего изменения состояния
        /// </summary>
        [DispId(10009)]
        DateTime LastStateTime { get; }

        /// <summary>
        /// Продолжительность вызова (как правило, с момента последнего изменения состояния)
        /// </summary>
        [DispId(10010)]
        TimeSpan Duration { get; }

        /// <summary>
        /// Информация о звонке
        /// </summary>
        [DispId(10011)]
        string AbonentCallInfoStr { get; }

        /// <summary>
        /// Информация о звонке в виде COM-коллекции ( пары Key/Value )
        /// В .Net лучше использовать ICall.AbonentCallInfo
        /// </summary>
        [DispId(10012)]
        object AbonentCallInfoCollection { get; }

        /// <summary>
        /// Можно ли совершить новый вызов
        /// </summary>
        [DispId(10013)]
        bool CanMake { get; }

        /// <summary>
        /// Можно ли завершить вызов
        /// </summary>
        [DispId(10014)]
        bool CanDrop { get; }

        /// <summary>
        /// Можно ли ответить на вызов
        /// </summary>
        [DispId(10015)]
        bool CanAnswer { get; }

        /// <summary>
        /// Можно ли поставить вызов на удержание
        /// </summary>
        [DispId(10016)]
        bool CanHold { get; }

        /// <summary>
        /// Можно ли вернуть вызов с удержания
        /// </summary>
        [DispId(10017)]
        bool CanUnHold { get; }

        /// <summary>
        /// Можно ли выполнить быстрый (слепой, безусловный) перевод вызова
        /// </summary>
        [DispId(10018)]
        bool CanQuickTransfer { get; }

        /// <summary>
        /// Можно ли создать быструю (слепую, безусловную) конференцию
        /// </summary>
        [DispId(10019)]
        bool CanQuickConference { get; }

        /// <summary>
        /// Можно ли выполнить обычный (с консультацией, условный) перевод вызова
        /// </summary>
        [DispId(10020)]
        bool CanStartTransfer { get; }

        /// <summary>
        /// Можно ли завершить обычный перевод
        /// </summary>
        [DispId(10021)]
        bool CanFinishTransfer { get; }

        /// <summary>
        /// Можно ли создать обычную (с консультацией, условную) конференцию
        /// </summary>
        [DispId(10022)]
        bool CanStartConference { get; }

        /// <summary>
        /// Можно ли завершить создание конференции
        /// </summary>
        [DispId(10023)]
        bool CanFinishConference { get; }

        /// <summary>
        /// Можно ли выполнить тоновый донабор цифр
        /// </summary>
        [DispId(10024)]
        bool CanSendDigits { get; }

        /// <summary>
        /// Завершить вызов (положить трубку)
        /// </summary>
        [DispId(10025)]
        bool Drop();

        /// <summary>
        /// Принять вызов (поднять трубку, ответить)
        /// </summary>
        [DispId(10026)]
        bool Answer();

        /// <summary>
        /// Поставить вызов на удержание
        /// </summary>
        [DispId(10027)]
        bool Hold();

        /// <summary>
        /// Вернуть вызов с удержания
        /// </summary>
        [DispId(10028)]
        bool UnHold();

        /// <summary>
        /// Выполнить быстрый (слепой, безусловный) перевод вызова
        /// </summary>
        /// <param name="number_">Номер телефона для набора</param>
        /// <param name="callerName_">Имя звонящего абонента (опционально)</param>
        [DispId(10029)]
        bool QuickTransfer(string number_, string callerName_ = "");

        /// <summary>
        /// Создать быструю (слепую, безусловную) конференцию
        /// </summary>
        /// <param name="number_">Номер телефона для набора</param>
        /// <param name="callerName_">Имя звонящего абонента (опционально)</param>
        [DispId(10030)]
        bool QuickConference(string number_, string callerName_ = "");

        /// <summary>
        /// Выполнить обычный (с консультацией, условный) перевод вызова
        /// </summary>
        /// <param name="number_">Номер телефона для набора</param>
        /// <param name="callerName_">Имя звонящего абонента (опционально)</param>
        [DispId(10031)]
        bool StartTransfer(string number_, string callerName_ = "");

        /// <summary>
        /// Завершить обычный перевод вызова
        /// </summary>
        [DispId(10032)]
        bool FinishTransfer();

        /// <summary>
        /// Создать обычную (с консультацией, условную) конференцию
        /// </summary>
        /// <param name="number_">Номер телефона для набора</param>
        /// <param name="callerName_">Имя звонящего абонента (опционально)</param>
        [DispId(10033)]
        bool StartConference(string number_, string callerName_ = "");

        /// <summary>
        /// Завершить создание конференции
        /// </summary>
        [DispId(10034)]
        bool FinishConference();

        /// <summary>
        /// Выполнить тоновый донабор цифр
        /// </summary>
        [DispId(10035)]
        bool SendDigits(string digits_);

        /// <summary>
        /// Уникальный идентификатор родительского вызова. Используется для отслеживания цепочки если один вызов порождает другой (например, при переводе вызовов)
        /// </summary>
        [DispId(10036)]
        string ParentCallID { get; }

    }

}

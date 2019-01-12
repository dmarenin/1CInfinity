//-----------------------------------------------------------------------
// <copyright file="ComInterface.cs" company="IntelTelecom">
//     Copyright IntelTelecom. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Cx.ActiveX
{
    [ComVisible(true)]
    [Guid("28897AAB-AC3D-43BA-843A-DF16B048565B")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface ICxIntegrationCore
    {
        /// <summary>
        /// Идентификатор экземпляра ядра (используется при отображении ActiveX-форм)
        /// </summary>
        [DispId(10000)]
        string CoreID { get; }

        /// <summary>
        /// Установить режим обработки исключительных ситуаций
        /// </summary>
        /// <param name="Value">True - исключения включены, False - отключены</param>
        [DispId(10001)]
        void SetUseExceptions(bool Value);

        /// <summary>
        /// Описание последней ошибки
        /// </summary>
        [DispId(10002)]
        string LastError { get; }

        /// <summary>
        /// Детальное описание последней ошибки
        /// </summary>
        [DispId(10003)]
        string LastErrorDetailed { get; }

        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        /// <param name="ConnectionString">Строка подключения</param>
        [DispId(10004)]
        void Connect(string ConnectionString);

        /// <summary>
        /// Подключиться к серверу, отобразив окно авторизации
        /// </summary>
        /// <returns></returns>
        [DispId(10005)]
        bool Logon();

        
        /// <summary>
        /// Подключиться к серверу и произвести авторизацию с использованием указанных параметров
        /// </summary>
        /// <param name="Login">Имя ползователя</param>
        /// <param name="Password">Пароль</param>
        /// <param name="Role">Роль</param>
        /// <param name="ServerAddress">Адрес сервера</param>
        /// <param name="ServerPort">Порт сервера</param>
        /// <returns>1 - успешно, остальное - код ошибки (см. LogonResult)</returns>
        [DispId(10006)]
        int LogonEx(string Login, string Password, string Role, string ServerAddress, int ServerPort);

        /// <summary>
        /// Выйти из системы
        /// </summary>
        [DispId(10007)]
        void Logoff();

        /// <summary>
        /// Отключиться от сервера
        /// </summary>
        [DispId(10008)]
        void Disconnect();

        [DispId(10009)]
        void Close();

        /// <summary>
        /// Возвращает состояние текущего подключения к серверу
        /// </summary>
        [DispId(10010)]
        bool IsConnected { get; }

        /// <summary>
        /// Преобразовать код результата авторизации в строку
        /// </summary>
        /// <param name="Result">Результат авторизации</param>
        /// <returns>Описание результата авторизации</returns>
        [DispId(10011)]
        string LogonResultToString(int Result);

        /// <summary>
        /// Получить интерфейс управления вызовами
        /// </summary>
        /// <param name="Extension">Внутренний номер абонента, соединениями которого необходимо 
        /// управлять (FirstParty). Если пусто - получаем интерфейс для управления всеми звонками 
        /// (ThirdParty)</param>
        /// <returns>Интерфейс управления вызовами</returns>
        [DispId(10012)]
        IComCallManagement GetCallManagement(string Extension);

        /// <summary>
        /// Получить интерфейс доступа к информации о вызовах, соединениях и сеансах
        /// </summary>
        /// <returns>Интерфейс доступа к информации о вызовах, соединениях и сеансах</returns>
        [DispId(10013)]
        IComCallsConnectionsSeances GetCallsConnectionsSeances();

        /// <summary>
        /// Получить интерфейс управления пользователями
        /// </summary>
        /// <returns>Интерфейс управления пользователями</returns>
        [DispId(10014)]
        IComUsersManagement GetUsersManagement();

        /// <summary>
        /// Получить интерфейс управления кампаниями
        /// </summary>
        /// <returns>Интерфейс управления кампаниями</returns>
        [DispId(10015)]
        AgatInfinityConnector.ICampaignsManagement GetCampaignsManagement();

        /// <summary>
        /// Получить интерфейс утилит
        /// </summary>
        /// <returns>Интерфейс утилит</returns>
        [DispId(10016)]
        AgatInfinityConnector.IUtilsManager GetUtilsManager();


    }


    [ComVisible(true)]
    [Guid("9066DD6F-50C7-458C-86D8-48049DE9DFA8")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface HostCtrl
    {
        [DispId(10000)]
        string GetCoreID();
        [DispId(10001)]
        void SetCoreID(string value);

        [DispId(10002)]
        object Content { get; }

        [DispId(10003)]
        void ShowContent(string Name);
        [DispId(10004)]
        void ShowContentByGuid(string Name);
        [DispId(10005)]
        void Clear();


        [DispId(20000)]
        bool Visible { get; set; }          // Typical control property
        [DispId(20001)]
        bool Enabled { get; set; }          // Typical control property
        [DispId(20002)]
        void Refresh();                     // Typical control method
    }




/*
    enum LogonResult
    {
        // Логон не выполнялся
        Uninitialized = 0,
        // Логон завершен успешно
        OK = 1,
        // Ошибка на сервере, не понятно какая...
        Error = 2,
        // Неверный логин или пароль. Для logoff - пользователь не найден.
        Invalid = 3,
        // Уже залогинен.
        AlreadyLogined = 4,
        // Доступ запрещен с такими правами.
        AccessDenied = 5,
        // Возвращен список ролей. Нужно выбрать роль.
        RolesList = 6,
        // Нет лицензии (для переданного RoleType)
        LicenseDenied = 7,
        // Несоответсивие версий клиента и сервера
        WrongVersion = 10,
        // Ограниченный режим лицензии (запуск с 2-мя инструментами)
        LicenceRestricted = 13
    };
*/


}

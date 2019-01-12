//-----------------------------------------------------------------------
// <copyright file="IConnection.cs" company="IntelTelecom">
//     Copyright IntelTelecom. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AgatInfinityConnector
{
    /// <summary>
    /// Соединение - как правило, разговор двух абонентов
    /// </summary>
    [ComVisible(true)]
    [Guid("78BA85D8-415B-4431-BF0F-E6377E58AE00")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    public interface IConnection
    {

        /// <summary>
        /// Уникальный идентификатор соединения
        /// </summary>
        [DispId(10000)]
        long ID { get; }

        /// <summary>
        /// Время начала соединения
        /// </summary>
        [DispId(10001)]
        DateTime TimeStart { get; }

        /// <summary>
        /// Продолжительность разговора
        /// </summary>
        [DispId(10002)]
        TimeSpan DurationTalk { get; }

        /// <summary>
        /// Состояние соединения
        /// </summary>
        [DispId(10003)]
        ConnectionState State { get; }

        /// <summary>
        /// Номер А
        /// </summary>
        [DispId(10004)]
        string ANumber { get; }

        /// <summary>
        /// Номер Б
        /// </summary>
        [DispId(10005)]
        string BNumber { get; }

        /// <summary>
        /// Абонент А
        /// </summary>
        [DispId(10006)]
        string ADisplayText { get; }

        /// <summary>
        /// Абонент Б
        /// </summary>
        [DispId(10007)]
        string BDisplayText { get; }

        /// <summary>
        /// Признак наличия записанного разговора
        /// </summary>
        [DispId(10008)]
        bool IsRecorded { get; }

        /// <summary>
        /// Уникальный идентификатор соединения
        /// </summary>
        [DispId(10014)]
        object ID_AsVariant { get; }

        /// <summary>
        /// Сохранить записанный разговор в предоставленный поток
        /// </summary>
        /// <param name="stream_">Поток для записи разговора</param>
        [DispId(10009)]
        void SaveRecordedFileToStream(Stream stream_);

        /// <summary>
        /// Сохранить записанный разговор в указанный файл
        /// </summary>
        /// <param name="fileName_">Имя файла</param>
        [DispId(10010)]
        void SaveRecordedFile(string fileName_);

        /// <summary>
        /// Воспроизвести записанный разговор через ShellExecute, сохранив его во временной папке
        /// </summary>
        [DispId(10011)]
        void PlayRecordedFile();

        /// <summary>
        /// Начать запись разговора
        /// </summary>
        [DispId(10012)]
        void StartRecord();

        /// <summary>
        /// Остановить запись разговора
        /// </summary>
        [DispId(10013)]
        void StopRecord();

        //[DispId(10015)]
    }

    /// <summary>
    /// Состояние соединения
    /// </summary>
    [ComVisible(true)]
    [Guid("9F0F9D83-35A1-4204-A1F7-B1D188B255B7")]
    public enum ConnectionState
    {
        /// <summary>
        /// Неизвестно
        /// </summary>
        Unknown = 0,        // Неизвестно
        /// <summary>
        /// Ожидание ответа
        /// </summary>
        Waiting = 1,       
        /// <summary>
        /// Разговор двух абонентов
        /// </summary>
        Talking = 21,          
        /// <summary>
        /// Конференция
        /// </summary>
        Conference = 31, 
        /// <summary>
        /// Удержание
        /// </summary>
        Hold = 41,    
        /// <summary>
        /// Соединение завершено, но канал еще не свободен (например, трубка на FXS не положена)
        /// </summary>
        Disconnected = 99,     
        /// <summary>
        /// Соединение завершено
        /// </summary>
        Finished = 100 
    }
}

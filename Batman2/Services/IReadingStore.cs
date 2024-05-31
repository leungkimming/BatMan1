using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Batman2.Models;
using Plugin.BLE.Abstractions.Contracts;

namespace Batman2.Services
{
    public interface IReadingStore<T>
    {
        string LastDeviceName { get; set; }
        DateTime LastHistoryDate { get; set; }

        Task<int> AddReadingAsync(T item);
        Task<IEnumerable<T>> GetReadingsAsync(string batt = "");
        Task<IEnumerable<Consumption>> GetConsumptionsAsync(string batt, bool today = false);
        Task<string[]> GetBatteriesAsync();
        Task<bool> ClearDBAsync(string battery);
        Task<bool> LoadDB();
        Task<bool> Compress(DateTime before);
    }
}

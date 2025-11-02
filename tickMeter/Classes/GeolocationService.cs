using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace tickMeter.Classes
{
    /// <summary>
    /// Robust геолокационный сервис с множественными fallback провайдерами и rate limiting
    /// </summary>
    public class GeolocationService
    {
        private static readonly object _lockObject = new object();
        private static DateTime _lastRequestTime = DateTime.MinValue;
        private static readonly TimeSpan _minRequestInterval = TimeSpan.FromSeconds(3); // Базовый интервал между запросами
        private static readonly Dictionary<string, LocationInfo> _locationCache = new Dictionary<string, LocationInfo>();
        private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30); // Кэш на 30 минут
        private static readonly Dictionary<string, ProviderStatus> _providerStatus = new Dictionary<string, ProviderStatus>(); // Статус каждого провайдера
        private static DateTime _geolocationDisabledUntil = DateTime.MinValue; // Время до которого геолокация отключена
        private static readonly TimeSpan _disableDuration = TimeSpan.FromMinutes(15); // Отключаем на 15 минут после исчерпания всех провайдеров
        private static int _totalFailedAttempts = 0; // Общий счетчик неудачных попыток по всем провайдерам
        /// <summary>
        /// Универсальная структура для геолокационных данных
        /// </summary>
        public class LocationInfo
        {
            public string Ip { get; set; }
            public string Country { get; set; }
            public string CountryCode { get; set; }
            public string Region { get; set; }
            public string City { get; set; }
            public string Timezone { get; set; }
            public string Isp { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string Source { get; set; } // Какой сервис предоставил данные
            public DateTime CachedAt { get; set; } = DateTime.Now; // Время кэширования
            
            public string FormattedLocation => 
                !string.IsNullOrEmpty(Country) && Country != "Error" && Country != "N/A"
                    ? !string.IsNullOrEmpty(City) ? $"{Country}, {City}" : Country
                    : "N/A";
                    
            public bool IsExpired => DateTime.Now - CachedAt > TimeSpan.FromMinutes(30);
        }

        /// <summary>
        /// Статус провайдера геолокации
        /// </summary>
        public class ProviderStatus
        {
            public string Name { get; set; }
            public int FailureCount { get; set; } = 0;
            public DateTime LastFailureTime { get; set; } = DateTime.MinValue;
            public DateTime DisabledUntil { get; set; } = DateTime.MinValue;
            public TimeSpan BackoffDelay { get; set; } = TimeSpan.FromSeconds(30); // Начальная задержка
            public bool IsAvailable => DateTime.Now >= DisabledUntil;
            public string LastError { get; set; } = "";
            
            /// <summary>
            /// Увеличивает счетчик ошибок и вычисляет время отключения провайдера
            /// </summary>
            public void RecordFailure(string error)
            {
                FailureCount++;
                LastFailureTime = DateTime.Now;
                LastError = error;
                
                // Экспоненциальный backoff: 30с, 2м, 5м, 10м, 20м максимум
                var backoffSeconds = Math.Min(30 * Math.Pow(2, FailureCount - 1), 1200); // Максимум 20 минут
                BackoffDelay = TimeSpan.FromSeconds(backoffSeconds);
                DisabledUntil = DateTime.Now.Add(BackoffDelay);
                
                DebugLogger.log($"[Geolocation] Provider {Name} disabled for {BackoffDelay.TotalMinutes:F1} minutes (failure #{FailureCount}: {error})");
            }
            
            /// <summary>
            /// Сбрасывает счетчик ошибок при успешном запросе
            /// </summary>
            public void RecordSuccess()
            {
                if (FailureCount > 0)
                {
                    DebugLogger.log($"[Geolocation] Provider {Name} recovered after {FailureCount} failures");
                }
                FailureCount = 0;
                LastError = "";
                DisabledUntil = DateTime.MinValue;
                BackoffDelay = TimeSpan.FromSeconds(30);
            }
        }

        /// <summary>
        /// Список геолокационных провайдеров в порядке приоритета
        /// </summary>
        private static readonly List<GeolocationProvider> Providers = new List<GeolocationProvider>
        {
            // 1. IPInfo.io - основной (бесплатно 50,000 запросов/месяц)
            new GeolocationProvider
            {
                Name = "IPInfo.io",
                UrlTemplate = "http://ipinfo.io/{0}/json",
                Parser = ParseIpInfoResponse,
                Timeout = 5000
            },
            
            // 2. IP-API.com - fallback (бесплатно 1000 запросов/час)
            new GeolocationProvider
            {
                Name = "IP-API.com", 
                UrlTemplate = "http://ip-api.com/json/{0}?fields=status,message,country,countryCode,region,regionName,city,timezone,isp,lat,lon,query",
                Parser = ParseIpApiResponse,
                Timeout = 7000
            },
            
            // 3. IPAPI.co - backup (бесплатно 30,000 запросов/месяц)
            new GeolocationProvider
            {
                Name = "IPAPI.co",
                UrlTemplate = "https://ipapi.co/{0}/json/",
                Parser = ParseIpApiCoResponse,
                Timeout = 8000
            },
            
            // 4. IPGeolocation.io - резерв (бесплатно 30,000 запросов/месяц)
            new GeolocationProvider
            {
                Name = "IPGeolocation.io",
                UrlTemplate = "https://api.ipgeolocation.io/ipgeo?apiKey=&ip={0}",
                Parser = ParseIpGeolocationResponse,
                Timeout = 10000
            },
            
            // 5. FreeGeoIP.app - последний шанс (бесплатно 15,000 запросов/час)
            new GeolocationProvider
            {
                Name = "FreeGeoIP.app",
                UrlTemplate = "https://freegeoip.app/json/{0}",
                Parser = ParseFreeGeoIpResponse,
                Timeout = 12000
            }
        };

        /// <summary>
        /// Структура провайдера геолокации
        /// </summary>
        private class GeolocationProvider
        {
            public string Name { get; set; }
            public string UrlTemplate { get; set; }
            public Func<string, LocationInfo> Parser { get; set; }
            public int Timeout { get; set; }
        }

        /// <summary>
        /// Получает геолокационную информацию для IP адреса с кэшированием и умным fallback
        /// </summary>
        public static async Task<LocationInfo> GetLocationAsync(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                return new LocationInfo { Country = "N/A", Source = "Invalid IP" };
            }

            // Проверяем, не отключена ли геолокация временно
            lock (_lockObject)
            {
                if (DateTime.Now < _geolocationDisabledUntil)
                {
                    var remainingTime = _geolocationDisabledUntil - DateTime.Now;
                    DebugLogger.log($"[Geolocation] Service temporarily disabled for {remainingTime.TotalMinutes:F1} more minutes due to all providers failing");
                    return new LocationInfo { Country = "Service Disabled", Source = "All Providers Failed", CachedAt = DateTime.Now };
                }
            }

            // Проверяем кэш
            lock (_lockObject)
            {
                if (_locationCache.ContainsKey(ipAddress))
                {
                    var cached = _locationCache[ipAddress];
                    if (!cached.IsExpired)
                    {
                        DebugLogger.log($"[Geolocation] Using cached location for IP: {ipAddress} -> {cached.FormattedLocation}");
                        return cached;
                    }
                    else
                    {
                        _locationCache.Remove(ipAddress);
                        DebugLogger.log($"[Geolocation] Cache expired for IP: {ipAddress}");
                    }
                }
            }

            // Инициализируем статус провайдеров если нужно
            lock (_lockObject)
            {
                foreach (var provider in Providers)
                {
                    if (!_providerStatus.ContainsKey(provider.Name))
                    {
                        _providerStatus[provider.Name] = new ProviderStatus { Name = provider.Name };
                    }
                }
            }

            // Rate limiting - базовая задержка между запросами
            lock (_lockObject)
            {
                var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
                if (timeSinceLastRequest < _minRequestInterval)
                {
                    var waitTime = _minRequestInterval - timeSinceLastRequest;
                    DebugLogger.log($"[Geolocation] Rate limiting: waiting {waitTime.TotalMilliseconds}ms");
                    Thread.Sleep(waitTime);
                }
                _lastRequestTime = DateTime.Now;
            }

            DebugLogger.log($"[Geolocation] Starting location detection for IP: {ipAddress}");

            var availableProviders = new List<GeolocationProvider>();
            var disabledProviders = new List<string>();

            // Проверяем какие провайдеры доступны
            lock (_lockObject)
            {
                foreach (var provider in Providers)
                {
                    var status = _providerStatus[provider.Name];
                    if (status.IsAvailable)
                    {
                        availableProviders.Add(provider);
                    }
                    else
                    {
                        var timeLeft = status.DisabledUntil - DateTime.Now;
                        disabledProviders.Add($"{provider.Name} (available in {timeLeft.TotalMinutes:F1}m)");
                    }
                }
            }

            if (disabledProviders.Count > 0)
            {
                DebugLogger.log($"[Geolocation] Disabled providers: {string.Join(", ", disabledProviders)}");
            }

            if (availableProviders.Count == 0)
            {
                DebugLogger.log("[Geolocation] No providers available - all are disabled due to errors");
                
                // Отключаем геолокацию на 15 минут если все провайдеры недоступны
                lock (_lockObject)
                {
                    _geolocationDisabledUntil = DateTime.Now.Add(_disableDuration);
                    DebugLogger.log($"[Geolocation] Disabling entire service for {_disableDuration.TotalMinutes} minutes");
                }

                var result = new LocationInfo 
                { 
                    Country = "All Providers Failed", 
                    Source = "Service Temporarily Disabled",
                    CachedAt = DateTime.Now
                };
                
                // Кэшируем результат на короткое время
                lock (_lockObject)
                {
                    _locationCache[ipAddress] = result;
                }
                
                return result;
            }

            DebugLogger.log($"[Geolocation] Available providers: {string.Join(", ", availableProviders.Select(p => p.Name))}");

            // Пробуем каждый доступный провайдер
            for (int i = 0; i < availableProviders.Count; i++)
            {
                var provider = availableProviders[i];
                try
                {
                    DebugLogger.log($"[Geolocation] Trying provider {i+1}/{availableProviders.Count}: {provider.Name}");
                    
                    var result = await GetLocationFromProvider(ipAddress, provider);
                    
                    if (result != null && !string.IsNullOrEmpty(result.Country) && 
                        result.Country != "Error" && result.Country != "N/A")
                    {
                        DebugLogger.log($"[Geolocation] SUCCESS with {provider.Name}: {result.FormattedLocation}");
                        
                        // Отмечаем успех для провайдера
                        lock (_lockObject)
                        {
                            _providerStatus[provider.Name].RecordSuccess();
                            _locationCache[ipAddress] = result;
                            _totalFailedAttempts = 0; // Сбрасываем общий счетчик при успехе
                        }
                        
                        return result;
                    }
                    else
                    {
                        DebugLogger.log($"[Geolocation] Provider {provider.Name} returned invalid/empty data");
                        throw new Exception("Invalid or empty data received");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[Geolocation] Provider {provider.Name} failed: {ex.Message}");
                    
                    // Отмечаем неудачу для провайдера
                    lock (_lockObject)
                    {
                        _providerStatus[provider.Name].RecordFailure(ex.Message);
                        _totalFailedAttempts++;
                    }
                    
                    // Если это не последний провайдер, продолжаем к следующему
                    if (i < availableProviders.Count - 1)
                    {
                        DebugLogger.log($"[Geolocation] Moving to next provider ({availableProviders.Count - i - 1} remaining)");
                    }
                }
                
                // Небольшая задержка между попытками разных провайдеров
                if (i < availableProviders.Count - 1)
                {
                    await Task.Delay(1000);
                }
            }

            DebugLogger.log($"[Geolocation] All available providers failed for IP: {ipAddress}");
            
            // Все доступные провайдеры не сработали
            var fallbackResult = new LocationInfo 
            { 
                Country = "Error", 
                Source = "All Available Providers Failed",
                CachedAt = DateTime.Now
            };
            
            // Кэшируем неудачный результат на короткое время
            lock (_lockObject)
            {
                _locationCache[ipAddress] = fallbackResult;
            }
            
            return fallbackResult;
        }

        /// <summary>
        /// Получает геолокацию от конкретного провайдера с надежной обработкой ошибок
        /// </summary>
        private static async Task<LocationInfo> GetLocationFromProvider(string ipAddress, GeolocationProvider provider)
        {
            try
            {
                var uri = string.Format(provider.UrlTemplate, ipAddress);
                DebugLogger.log($"[Geolocation] Requesting: {uri}");
                
                // Создаем HttpWebRequest для лучшего контроля
                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "GET";
                request.UserAgent = $"tickMeter/{System.Windows.Forms.Application.ProductVersion}";
                request.Timeout = provider.Timeout; // Используем настроенный timeout провайдера
                request.ReadWriteTimeout = provider.Timeout;
                request.Proxy = null; // Отключаем proxy
                request.KeepAlive = false; // Не держим соединение
                
                string response;
                using (var httpResponse = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (var stream = httpResponse.GetResponseStream())
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        response = await reader.ReadToEndAsync();
                    }
                }
                
                var result = provider.Parser(response);
                
                if (result != null)
                {
                    result.Source = provider.Name;
                    result.Ip = ipAddress;
                    result.CachedAt = DateTime.Now;
                }
                
                return result;
            }
            catch (WebException webEx)
            {
                if (webEx.Response is HttpWebResponse httpResponse)
                {
                    int statusCode = (int)httpResponse.StatusCode;
                    var errorMessage = $"HTTP {statusCode} ({httpResponse.StatusDescription})";
                    DebugLogger.log($"[Geolocation] Provider {provider.Name} HTTP error: {errorMessage}");
                    throw new Exception(errorMessage);
                }
                else if (webEx.Status == WebExceptionStatus.Timeout)
                {
                    var errorMessage = $"Request timeout ({provider.Timeout}ms)";
                    DebugLogger.log($"[Geolocation] Provider {provider.Name} timeout: {errorMessage}");
                    throw new Exception(errorMessage);
                }
                else
                {
                    var errorMessage = $"Network error: {webEx.Message}";
                    DebugLogger.log($"[Geolocation] Provider {provider.Name} network error: {errorMessage}");
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Geolocation] Provider {provider.Name} error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Очищает кэш геолокации и сбрасывает все ошибки провайдеров
        /// </summary>
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _locationCache.Clear();
                _providerStatus.Clear();
                _totalFailedAttempts = 0;
                _geolocationDisabledUntil = DateTime.MinValue; // Включаем геолокацию обратно
                DebugLogger.log("[Geolocation] Cache cleared, all provider statuses reset, and service re-enabled");
            }
        }

        /// <summary>
        /// Возвращает информацию о кэше и статистике провайдеров
        /// </summary>
        public static string GetCacheInfo()
        {
            lock (_lockObject)
            {
                int total = _locationCache.Count;
                int expired = 0;
                foreach (var item in _locationCache.Values)
                {
                    if (item.IsExpired) expired++;
                }
                
                bool isDisabled = DateTime.Now < _geolocationDisabledUntil;
                string disabledInfo = isDisabled ? $", Service disabled until: {_geolocationDisabledUntil:HH:mm:ss}" : "";
                
                var activeProviders = _providerStatus.Values.Count(p => p.IsAvailable);
                var totalProviders = Providers.Count;
                
                return $"Cache: {total} entries ({expired} expired), Providers: {activeProviders}/{totalProviders} active, Total failures: {_totalFailedAttempts}{disabledInfo}";
            }
        }

        /// <summary>
        /// Возвращает детальную информацию о статусе каждого провайдера
        /// </summary>
        public static string GetProviderStatus()
        {
            lock (_lockObject)
            {
                var status = new List<string>();
                foreach (var provider in Providers)
                {
                    if (_providerStatus.ContainsKey(provider.Name))
                    {
                        var ps = _providerStatus[provider.Name];
                        var statusText = ps.IsAvailable ? "Available" : 
                            $"Disabled until {ps.DisabledUntil:HH:mm:ss} (failures: {ps.FailureCount})";
                        var lastError = !string.IsNullOrEmpty(ps.LastError) ? $" - Last error: {ps.LastError}" : "";
                        status.Add($"{provider.Name}: {statusText}{lastError}");
                    }
                    else
                    {
                        status.Add($"{provider.Name}: Never used");
                    }
                }
                
                // Добавляем общую информацию о системе
                bool isServiceDisabled = DateTime.Now < _geolocationDisabledUntil;
                if (isServiceDisabled)
                {
                    var remainingTime = _geolocationDisabledUntil - DateTime.Now;
                    status.Add($"\n🚫 ENTIRE SERVICE DISABLED for {remainingTime.TotalMinutes:F1} more minutes");
                }
                
                status.Add($"\nTotal failed attempts: {_totalFailedAttempts}");
                
                return string.Join("\n", status);
            }
        }

        /// <summary>
        /// Принудительно включает всех провайдеров (для тестирования)
        /// </summary>
        public static void ForceEnableAllProviders()
        {
            lock (_lockObject)
            {
                foreach (var status in _providerStatus.Values)
                {
                    status.RecordSuccess(); // Сбрасываем все ошибки
                }
                _totalFailedAttempts = 0;
                _geolocationDisabledUntil = DateTime.MinValue;
                DebugLogger.log("[Geolocation] ALL PROVIDERS FORCE-ENABLED - all errors cleared");
            }
        }

        /// <summary>
        /// Очищает только устаревшие записи из кэша
        /// </summary>
        public static void CleanExpiredCache()
        {
            lock (_lockObject)
            {
                var keysToRemove = new List<string>();
                foreach (var kvp in _locationCache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    _locationCache.Remove(key);
                }
                
                if (keysToRemove.Count > 0)
                {
                    DebugLogger.log($"[Geolocation] Cleaned {keysToRemove.Count} expired cache entries");
                }
            }
        }

        /// <summary>
        /// Сбрасывает статус всех провайдеров и включает геолокацию (полезно после длительного перерыва)
        /// </summary>
        public static void ResetProviderErrors()
        {
            lock (_lockObject)
            {
                foreach (var status in _providerStatus.Values)
                {
                    status.RecordSuccess(); // Сбрасываем все ошибки
                }
                _totalFailedAttempts = 0;
                _geolocationDisabledUntil = DateTime.MinValue;
                DebugLogger.log("[Geolocation] All provider statuses reset and service re-enabled");
            }
        }

        #region Provider Parsers

        /// <summary>
        /// Парсер для IPInfo.io (оригинальный формат)
        /// </summary>
        private static LocationInfo ParseIpInfoResponse(string json)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(json);
                
                var result = new LocationInfo
                {
                    Country = data?.country?.ToString(),
                    CountryCode = data?.country?.ToString(), 
                    Region = data?.region?.ToString(),
                    City = data?.city?.ToString(),
                    Isp = data?.org?.ToString()
                };

                // Конвертируем код страны в полное название
                if (!string.IsNullOrEmpty(result.Country) && result.Country.Length == 2)
                {
                    try
                    {
                        var regionInfo = new RegionInfo(result.Country);
                        result.Country = regionInfo.EnglishName;
                    }
                    catch
                    {
                        // Если не удалось конвертировать, оставляем как есть
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Geolocation] IPInfo parsing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Парсер для IP-API.com
        /// </summary>
        private static LocationInfo ParseIpApiResponse(string json)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(json);
                
                if (data?.status?.ToString() != "success")
                {
                    DebugLogger.log($"[Geolocation] IP-API error: {data?.message}");
                    return null;
                }

                return new LocationInfo
                {
                    Country = data?.country?.ToString(),
                    CountryCode = data?.countryCode?.ToString(),
                    Region = data?.regionName?.ToString(),
                    City = data?.city?.ToString(),
                    Timezone = data?.timezone?.ToString(),
                    Isp = data?.isp?.ToString(),
                    Latitude = data?.lat != null ? (double?)Convert.ToDouble(data.lat) : null,
                    Longitude = data?.lon != null ? (double?)Convert.ToDouble(data.lon) : null
                };
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Geolocation] IP-API parsing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Парсер для IPAPI.co
        /// </summary>
        private static LocationInfo ParseIpApiCoResponse(string json)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(json);
                
                if (data?.error == true)
                {
                    DebugLogger.log($"[Geolocation] IPAPI.co error: {data?.reason}");
                    return null;
                }

                return new LocationInfo
                {
                    Country = data?.country_name?.ToString(),
                    CountryCode = data?.country_code?.ToString(),
                    Region = data?.region?.ToString(),
                    City = data?.city?.ToString(),
                    Timezone = data?.timezone?.ToString(),
                    Isp = data?.org?.ToString(),
                    Latitude = data?.latitude != null ? (double?)Convert.ToDouble(data.latitude) : null,
                    Longitude = data?.longitude != null ? (double?)Convert.ToDouble(data.longitude) : null
                };
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Geolocation] IPAPI.co parsing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Парсер для IPGeolocation.io
        /// </summary>
        private static LocationInfo ParseIpGeolocationResponse(string json)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(json);
                
                return new LocationInfo
                {
                    Country = data?.country_name?.ToString(),
                    CountryCode = data?.country_code2?.ToString(),
                    Region = data?.state_prov?.ToString(),
                    City = data?.city?.ToString(),
                    Timezone = data?.time_zone?.name?.ToString(),
                    Isp = data?.isp?.ToString(),
                    Latitude = data?.latitude != null ? (double?)Convert.ToDouble(data.latitude) : null,
                    Longitude = data?.longitude != null ? (double?)Convert.ToDouble(data.longitude) : null
                };
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Geolocation] IPGeolocation parsing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Парсер для FreeGeoIP.app
        /// </summary>
        private static LocationInfo ParseFreeGeoIpResponse(string json)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(json);
                
                return new LocationInfo
                {
                    Country = data?.country_name?.ToString(),
                    CountryCode = data?.country_code?.ToString(),
                    Region = data?.region_name?.ToString(),
                    City = data?.city?.ToString(),
                    Timezone = data?.time_zone?.ToString(),
                    Latitude = data?.latitude != null ? (double?)Convert.ToDouble(data.latitude) : null,
                    Longitude = data?.longitude != null ? (double?)Convert.ToDouble(data.longitude) : null
                };
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Geolocation] FreeGeoIP parsing error: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
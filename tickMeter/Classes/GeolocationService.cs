using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace tickMeter.Classes
{
    /// <summary>
    /// Robust геолокационный сервис с множественными fallback провайдерами
    /// </summary>
    public class GeolocationService
    {
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
            
            public string FormattedLocation => 
                !string.IsNullOrEmpty(Country) && Country != "Error" && Country != "N/A"
                    ? !string.IsNullOrEmpty(City) ? $"{Country}, {City}" : Country
                    : "N/A";
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
        /// Получает геолокацию IP с автоматическим fallback
        /// </summary>
        public static async Task<LocationInfo> GetLocationAsync(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                return new LocationInfo { Country = "N/A", Source = "Invalid IP" };
            }

            DebugLogger.log($"[Geolocation] Starting location detection for IP: {ipAddress}");

            foreach (var provider in Providers)
            {
                try
                {
                    DebugLogger.log($"[Geolocation] Trying provider: {provider.Name}");
                    
                    var result = await GetLocationFromProvider(ipAddress, provider);
                    if (result != null && !string.IsNullOrEmpty(result.Country) && 
                        result.Country != "Error" && result.Country != "N/A")
                    {
                        DebugLogger.log($"[Geolocation] SUCCESS with {provider.Name}: {result.FormattedLocation}");
                        return result;
                    }
                    
                    DebugLogger.log($"[Geolocation] Provider {provider.Name} returned invalid data");
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[Geolocation] Provider {provider.Name} failed: {ex.Message}");
                }
                
                // Небольшая задержка между попытками
                await Task.Delay(500);
            }

            DebugLogger.log($"[Geolocation] All providers failed for IP: {ipAddress}");
            return new LocationInfo { Country = "Error", Source = "All providers failed" };
        }

        /// <summary>
        /// Получает геолокацию от конкретного провайдера
        /// </summary>
        private static async Task<LocationInfo> GetLocationFromProvider(string ipAddress, GeolocationProvider provider)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("User-Agent", $"tickMeter/{System.Windows.Forms.Application.ProductVersion}");
                        
                        // Устанавливаем таймаут
                        var uri = string.Format(provider.UrlTemplate, ipAddress);
                        
                        DebugLogger.log($"[Geolocation] Requesting: {uri}");
                        
                        var response = webClient.DownloadString(uri);
                        var result = provider.Parser(response);
                        
                        if (result != null)
                        {
                            result.Source = provider.Name;
                            result.Ip = ipAddress;
                        }
                        
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[Geolocation] Provider {provider.Name} error: {ex.Message}");
                    throw;
                }
            });
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
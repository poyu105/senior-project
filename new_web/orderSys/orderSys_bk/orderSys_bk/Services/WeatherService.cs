using System.Net.Http;
using System.Text.Json;

namespace orderSys_bk.Services
{
    public class WeatherService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        public static string getSeason(int month)
        {
            //對應季節 win:冬季(12,1,2) spr:春季(3,4,5) sum:夏季(6,7,8) aut:秋季(9,10,11)
            string season = (month == 12 || month <= 2) ? "wi" :
                            (month >= 3 && month <= 5) ? "sp" :
                            (month >= 6 && month <= 8) ? "su" : "au";
            return season;
        }

        /// <summary>
        /// 從Open-Meteo取得天氣預報
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="latitude">緯度</param>
        /// <param name="longitude">經度</param>
        /// <returns>天氣狀況代碼 S:晴天 C:陰天 R:雨天 N:未知</returns>
        public static async Task<String> GetWeatherForecastAsync(String date, double latitude, double longitude)
        {
            Console.WriteLine($"【WeatherService】 -> GetWeatherForecastAsync() -> 日期:{date}, 緯度:{latitude}, 經度:{longitude}");
            if (String.IsNullOrEmpty(date))
            {
                throw new ArgumentException("預測日期不可為空");
            }

            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                throw new ArgumentException("日期格式錯誤，請使用YYYY-MM-DD格式");
            }

            try
            {
                string dateString = parsedDate.ToString("yyyy-MM-dd");
                string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=weather_code&timezone=Asia%2FSingapore&start_date={dateString}&end_date={dateString}";

                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"【WeatherService】 -> GetWeatherForecastAsync() -> 天氣預報資料response: {response}");

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"【WeatherService】 -> GetWeatherForecastAsync() -> 天氣預報資料json: {json}");
                using var doc = JsonDocument.Parse(json);

                var code = doc.RootElement
                     .GetProperty("daily")
                     .GetProperty("weather_code")[0]
                     .GetInt32();

                //將天氣代碼轉成中文描述
                //自訂代碼: S:晴天, C:陰天, R:雨天, N:未知天氣
                string weather = code switch
                {
                    0 => "S", //"晴天"
                    1 => "C", //"主要多雲"
                    2 => "R", //"陣雨"
                    3 => "R", //"雷陣雨"
                    45 => "C", //"霧"
                    48 => "C", //"濕霧"
                    51 => "R", //"細雨"
                    53 => "R", //"中雨"
                    55 => "R", //"大雨"
                    61 => "N", //"小雪"
                    63 => "N", //"中雪"
                    65 => "N", //"大雪"
                    80 => "R", //"雷陣雨"
                    81 => "R", //"暴雨"
                    95 => "R", //"雷暴"
                    96 => "R", //"雷暴伴隨小冰雹"
                    99 => "R", //"雷暴伴隨大冰雹"
                    _ => "N", //"未知天氣"
                };

                return weather;
            }
            catch (Exception ex)
            {
                throw new Exception("取得天氣預報失敗: " + ex.Message);
            }
        }
    }
}

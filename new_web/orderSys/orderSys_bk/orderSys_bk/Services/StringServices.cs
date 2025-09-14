namespace orderSys_bk.Services
{
    public class StringServices
    {
        /// <summary>
        /// 移除字串首尾空白
        /// </summary>
        /// <param name="input">輸入字串</param>
        /// <returns>去除首尾空白後的字串，若輸入為 null 則回傳空字串</returns>
        public static string TrimSpaces(string? input)
        {
            return input?.Trim() ?? string.Empty;
        }
    }
}
